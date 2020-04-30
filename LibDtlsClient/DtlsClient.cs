﻿using System;


using com.mobius.software.windows.iotbroker.coap.net;
using com.mobius.software.windows.iotbroker.network;


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

    public DtlsClient(DataReceivedCallback data_cb, ConnectedCallback connect_cb)
    {
      pFnReceive = data_cb;
      pFnConnect = connect_cb;
    }

    public bool Connect(string ipaddr, UInt16 usPort)
    {
      udp_client = new UDPClient(new System.Net.DnsEndPoint(ipaddr, usPort), true, null, String.Empty, 1, 10);
      udp_client.Init(this);
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
