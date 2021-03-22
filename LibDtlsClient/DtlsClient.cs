using System;


using com.mobius.software.windows.iotbroker.coap.net;
using com.mobius.software.windows.iotbroker.network;
using Org.BouncyCastle.Crypto.Tls;


namespace LibDtlsClient
{
  public class DtlsClient : ConnectionListener<string>
  {

    UDPClient udp_client;

    public delegate void DataReceivedCallback(string msg);
    public delegate void ConnectedCallback(bool fStatus);
    public DataReceivedCallback pFnReceive;
    public ConnectedCallback pFnConnect;
    public bool fHasConnected = false;
    System.Net.IPEndPoint localEndPoint;

    public DtlsClient(DataReceivedCallback data_cb, ConnectedCallback connect_cb)
    {
      pFnReceive = data_cb;
      pFnConnect = connect_cb;

      this.localEndPoint = null;
    }

    public DtlsClient(DataReceivedCallback data_cb, ConnectedCallback connect_cb, System.Net.IPEndPoint localEndPoint)
    {
      pFnReceive = data_cb;
      pFnConnect = connect_cb;
      this.localEndPoint = localEndPoint;
    }

    public bool Connect(string ipaddr, UInt16 usPort)
    {
      return Connect(ipaddr, usPort, null, null, false);
    }

    public bool Connect(string ipaddr, UInt16 usPort, string certFile, string certPassword, bool fLoadWholeChain)
    {
      udp_client = new UDPClient(new System.Net.DnsEndPoint(ipaddr, usPort), true, 1, 10);
      udp_client.Init(this, localEndPoint, certFile, certPassword, fLoadWholeChain);

#if false
      /* Wait for connection or timeout */
      int timeouts = 0;
      while ((!fHasConnected) && (timeouts < 50))
      {
        System.Threading.Thread.Sleep(100);
        timeouts++;
      }
      if (timeouts == 50)
      {
        return false;
      }
#endif
      return true;
    }

    public void Send(string sDat)
    {
      udp_client.Send(sDat);
    }

    public void PacketReceived(string message)
    {
      pFnReceive(message);
    }

    public void ConnectionLost()
    {
      //pFnConnect(false);
    }

    public void Connected()
    {
      pFnConnect(true);
    }

    public void ConnectFailed()
    {
      pFnConnect(false);
    }
  }
}
