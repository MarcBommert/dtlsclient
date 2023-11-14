
using com.mobius.software.windows.iotbroker.dal;
using com.mobius.software.windows.iotbroker.network;
using com.mobius.software.windows.iotbroker.network.dtls;
using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

/**
* Mobius Software LTD
* Copyright 2015-2018, Mobius Software LTD
*
* This is free software; you can redistribute it and/or modify it
* under the terms of the GNU Lesser General Public License as
* published by the Free Software Foundation; either version 2.1 of
* the License, or (at your option) any later version.
*
* This software is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
* Lesser General Public License for more details.
*
* You should have received a copy of the GNU Lesser General Public
* License along with this software; if not, write to the Free
* Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
* 02110-1301 USA, or see the FSF site: http://www.fsf.org.
*/

namespace com.mobius.software.windows.iotbroker.coap.net
{

  class MyAsyncDtlsClientAuthentication : TlsAuthentication
  {
    private TlsContext context;
    X509CertificateEntry[] clientCertChain = null;
    AsymmetricKeyEntry clientPrivateKey = null;

    public MyAsyncDtlsClientAuthentication(TlsContext context, X509CertificateEntry[] clientCertChain, AsymmetricKeyEntry clientPrivateKey)
    {
      this.context = context;
      this.clientCertChain = clientCertChain;
      this.clientPrivateKey = clientPrivateKey;
    }

    public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
    {
      if (clientCertChain != null)
      {
        Org.BouncyCastle.Asn1.X509.X509CertificateStructure[] certs = new Org.BouncyCastle.Asn1.X509.X509CertificateStructure[clientCertChain.Length];
        for (int i = 0; i < clientCertChain.Length; i++)
        {
          X509CertificateEntry entry = clientCertChain[i];
          certs[i] = entry.Certificate.CertificateStructure;
        }

        /* for all signature and hsah algorithm tuples the server supports ... */
        foreach (SignatureAndHashAlgorithm sh in certificateRequest.SupportedSignatureAlgorithms)
        {
          if (sh.Signature == SignatureAlgorithm.ecdsa)     /* here, we assume the certificate is signed with ecdsa */
          {
            TlsSignerCredentials creds = new DefaultTlsSignerCredentials(context, new Certificate(certs), clientPrivateKey.Key, sh);
            return creds;
          }
        }
      }
      return null;
    }

    public void NotifyServerCertificate(Certificate serverCertificate)
    {
    }
  }


  public class MyAsyncDtlsClient: AsyncDtlsClient
  {

    X509CertificateEntry[] clientCertChain = null;
    AsymmetricKeyEntry clientPrivateKey = null;

    public MyAsyncDtlsClient(string certFile, string certPassword, bool fLoadWholeChain) : base(null, String.Empty, null)
    {
      if (certFile != String.Empty)
      {
        System.IO.FileStream fs = new System.IO.FileStream(certFile, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
        Pkcs12Store pkcs12Store = new Pkcs12Store(fs, certPassword.ToCharArray());

        string alias = pkcs12Store.Aliases.Cast<string>().FirstOrDefault(al => pkcs12Store.IsKeyEntry(al) && pkcs12Store.GetKey(al).Key.IsPrivate);

        fs.Close();

        if (fLoadWholeChain)
        {
          clientCertChain = pkcs12Store.GetCertificateChain(alias);
        }
        else
        {
          clientCertChain = new X509CertificateEntry[1];
          clientCertChain[0] = pkcs12Store.GetCertificate(alias);
        }

        clientPrivateKey = pkcs12Store.GetKey(alias);
      }
    }

    override public TlsAuthentication GetAuthentication()
    {
      return new MyAsyncDtlsClientAuthentication(this.mContext, clientCertChain, clientPrivateKey);
    }
  }

  public class UDPClient : NetworkChannel<string>, DtlsStateHandler
  {
    private DnsEndPoint address;
    private Int32 workerThreads;

    private Bootstrap bootstrap;
    private MultithreadEventLoopGroup loopGroup;
    private IChannel channel;

    private AsyncDtlsClientProtocol _clientProtocol;

    private Boolean isSecured;
    private String certificate;
    private String certificatePassword;

    private ConnectionListener<string> _listener;

    private Boolean _handshakeSuccesfull = false;
    private System.Timers.Timer _handhshakeTimer = null;

    private Int32 connectPeriod;

    // handlers for client connections
    public UDPClient(DnsEndPoint address, Boolean isSecured, Int32 workerThreads, Int32 connectPeriod)
    {
      this.address = address;
      this.workerThreads = workerThreads;
      this.isSecured = isSecured;
      this.certificate = null;
      this.certificatePassword = String.Empty;
      this.connectPeriod = connectPeriod;
    }

    public void Shutdown()
    {
      if (channel != null)
      {
        channel.CloseAsync();
        channel = null;
      }

      if (loopGroup != null)
        loopGroup.ShutdownGracefullyAsync();
    }

    public void Close()
    {
      _clientProtocol.Close();

      if (channel != null)
      {
        channel.CloseAsync();
        channel = null;
      }
      if (loopGroup != null)
      {
        loopGroup.ShutdownGracefullyAsync();
        loopGroup = null;
      }
    }

    public Boolean Init(ConnectionListener<string> listener)
    {
      return Init(listener, null, null, null, false);
    }

    public Boolean Init(ConnectionListener<string> listener, IPEndPoint localEndPoint, string certFile, string certPassword, bool fLoadWholeChain)
    {
      if (channel == null)
      {
        this._listener = listener;
        bootstrap = new Bootstrap();
        loopGroup = new MultithreadEventLoopGroup(workerThreads);
        bootstrap.Group(loopGroup);

        bootstrap.Channel<SocketDatagramChannel>();

        UDPClient currClient = this;
        bootstrap.Handler(new ActionChannelInitializer<SocketDatagramChannel>(channel =>
        {
          IChannelPipeline pipeline = channel.Pipeline;
          if (isSecured)
          {
            Pkcs12Store keystore = null;
            if (certificate != null && certificate.Length > 0)
              keystore = CertificatesHelper.loadBC(certificate, certificatePassword);

            AsyncDtlsClient client = new MyAsyncDtlsClient(certFile, certPassword, fLoadWholeChain);
            _clientProtocol = new AsyncDtlsClientProtocol(client, new SecureRandom(), channel, currClient, true, ProtocolVersion.DTLSv12);
            pipeline.AddLast(new DtlsClientHandler(_clientProtocol, this));
          }


          pipeline.AddLast("handler", new RawMessageHandler(listener));
#if false
          pipeline.AddLast(new CoapEncoder(channel));
          pipeline.AddLast(new ExceptionHandler());
#endif
        }));

        bootstrap.RemoteAddress(address);

#if true

        try
        {
          Task<IChannel> task;
          if (localEndPoint != null)
          {
             task = bootstrap.BindAsync(localEndPoint);
          }
          else
          {
            task = bootstrap.BindAsync(IPEndPoint.MinPort);
          }

          task.GetAwaiter().OnCompleted(() =>
          {
            try
            {
              channel = task.Result;
              Task connectTask = channel.ConnectAsync(address);
              
              
              connectTask.GetAwaiter().OnCompleted(() =>
              {
                if (_clientProtocol == null)
                {
                  if (channel != null)
                    listener.Connected();
                  else
                    listener.ConnectFailed();
                }
                else
                {
                  startHandhshakeTimer();
                  _clientProtocol.InitHandshake(null);
                }
              });
            }
            catch (Exception)
            {
              listener.ConnectFailed();
              return;
            }
          });
        }
        catch (Exception)
        {
          return false;
        }
      }
#endif
      return true;
    }

    public Boolean IsConnected()
    {
      return channel != null;
    }

    public void Send(string rawmsg)
    {
      IByteBuffer buf = Unpooled.Buffer();
      foreach (byte b in rawmsg)
      {
        buf.WriteByte(b);
      }
      if (_clientProtocol != null)
      {
        _clientProtocol.sendPacket(buf);
      }
      else
        channel.WriteAndFlushAsync(buf);
    }

    public void handshakeStarted(IChannel channel)
    {
      //nothing to do here
    }

    public void handshakeCompleted(IChannel channel)
    {
      _handshakeSuccesfull = true;
      _listener.Connected();
    }

    public void errorOccured(IChannel channel)
    {
      _listener.ConnectFailed();
    }

    public void startHandhshakeTimer()
    {
      _handhshakeTimer = new System.Timers.Timer();
      _handhshakeTimer.AutoReset = false;
      _handhshakeTimer.Elapsed += new ElapsedEventHandler(handshakeVerification);
      _handhshakeTimer.Interval = connectPeriod;
      _handhshakeTimer.Enabled = true;
    }

    public void handshakeVerification(Object sender, ElapsedEventArgs args)
    {
      if (channel != null && channel.Open && !_handshakeSuccesfull)
        _listener.ConnectFailed();
    }
  }
}
