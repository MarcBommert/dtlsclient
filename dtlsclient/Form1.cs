using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using com.mobius.software.windows.iotbroker.coap.net;
using com.mobius.software.windows.iotbroker.network;

namespace dtlsclient
{
  public partial class Form1 : Form, ConnectionListener<string>
  {

    public delegate void SetChecked();
    public SetChecked mySetCheckedDelegate;

    public delegate void SetUnchecked();
    public SetUnchecked mySetUncheckedDelegate;

    public delegate void AddListBoxEntry(string message);
    public AddListBoxEntry myAddListBoxEntryDelegate;
    
    public Form1()
    {
      mySetCheckedDelegate = new SetChecked(SetCheckedMethod);
      mySetUncheckedDelegate = new SetUnchecked(SetUncheckedMethod);
      myAddListBoxEntryDelegate = new AddListBoxEntry(AddListBoxEntryMethod);
      InitializeComponent();
    }

    UDPClient udp_client;

    private void button1_Click(object sender, EventArgs e)
    {
      System.Net.IPAddress ip;
      UInt16 usPort;

      if (!System.Net.IPAddress.TryParse(textBox1.Text, out ip))
      {  
        MessageBox.Show("Failed to parse the specified server IP address.", "Invalid user input", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }

      if (!UInt16.TryParse(textBox2.Text, out usPort))
      {
        MessageBox.Show("Failed to parse the specified server port number.", "Invalid user input", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }

      udp_client = new UDPClient(new System.Net.DnsEndPoint(ip.ToString(), usPort), true, 1, 10);
      udp_client.Init(this);
    }

    public void PacketReceived(string message)
    {
      this.Invoke(myAddListBoxEntryDelegate, message);
    }


    public void SetCheckedMethod()
    {
      checkBox1.Checked = true;
    }

    public void SetUncheckedMethod()
    {
      checkBox1.Checked = false;
    }

    public string MessageToHumanReadableString(string msg)
    {
      string ret = "";
      int i = 0;
      foreach (char c in msg)
      {
        ret += ((int)c).ToString("X2") + " ";
        i++;
        if ((i > 0) && (i % 16 == 0))
          ret += System.Environment.NewLine;
      }
      return ret;
    }

    public void AddListBoxEntryMethod(string message)
    {
      listBox1.Items.Add(MessageToHumanReadableString(message));
    }

    public void ConnectionLost()
    {
      this.Invoke(mySetUncheckedDelegate);
    }

    public void Connected()
    {
      this.Invoke(mySetCheckedDelegate);
    }

    public void ConnectFailed()
    {
      this.Invoke(mySetUncheckedDelegate);
    }


    /// <summary>
    /// Test for all valid hex char [0-9, a-f, A-F]
    /// </summary>
    /// <param name="c">Character to test</param>
    /// <returns>true if valid hex char, false otherwise</returns>
    public bool IsValidHexChar(char c)
    {
      if (Char.IsDigit(c))
        return true;
      c = Char.ToLower(c);
      if ((c == 'a') || (c == 'b') || (c == 'c') || (c == 'd') || (c == 'e') || (c == 'f'))
        return true;
      return false;
    }

    /// <summary>
    /// Test for all valid hex chars in given string [0-9, a-f, A-F]
    /// </summary>
    /// <param name="s">String to test</param>
    /// <returns>true if all valid hex chars in string, false otherwise</returns>
    public bool IsValidHexString(string s)
    {
      foreach (char c in s)
        if (!IsValidHexChar(c))
          return false;
      return true;
    }
    public string byteArrayToString(byte[] source)
    {
      string ret = null;
      if (source != null)
        foreach (byte b in source)
          ret += (char)b;
      return ret;
    }

    public string pack_usint(byte val)
    {
      byte[] barray = new byte[1];
      barray[0] = val;
      return byteArrayToString(barray);
    }

    private void button2_Click(object sender, EventArgs e)
    {

      string sSendUserData = textBox3.Text;         // parse output data from textbox

      int numBytes = sSendUserData.Length / 2;
      byte []sendIoData = new byte[numBytes];
      textBox3.BackColor = Color.White;
      label4.Text = "OK";
      if (sSendUserData.Length % 2 != 0)
      {
        textBox3.BackColor = Color.Red;
        label4.Text = "Odd number of characters given.";
      }
      else if (!IsValidHexString(sSendUserData))
      {
        textBox3.BackColor = Color.Red;
        label4.Text = "Invalid characters given";
      }
      else
      {
        for (int i = 0; i < sSendUserData.Length; i += 2)
        {
          string sByte = sSendUserData.Substring(i, 2);
          if (!byte.TryParse(sByte, System.Globalization.NumberStyles.HexNumber, null, out sendIoData[i / 2]))
          {
            textBox3.BackColor = Color.Red;
            label4.Text = "Parsing failed";
          }
        }

        string sDat = String.Empty;
        for (int i = 0; i < numBytes; i++)
          sDat += pack_usint(sendIoData[i]);  // data

        udp_client.Send(sDat);
      }
    }
  }
}
