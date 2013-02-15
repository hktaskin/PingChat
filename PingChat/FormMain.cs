using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

/* 
 * Author: Halil Kemal TASKIN
 * Web: http://hkt.me
 * 
 */

namespace PingChat
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        #region Fields

        int bufferLen = 32;

        byte[] _buffer = new byte[4096];

        EndPoint remoteRawEndPoint = (EndPoint)new IPEndPoint(IPAddress.Any, 0);

        Socket _socket = null;

        string destIP = "", srcIP = "";

        /* DEBUG 
        int c = 1;
        long t0;
        */

        #endregion

        #region Form Methods

        private void FormMain_Load(object sender, EventArgs e)
        {
            lblStatus.Text = "Ready. Select a source IP(v4) and write destination IP(v4), click 'Test' and if successful then 'Connect'";

            btnConnect.Enabled = false;
            btnSend.Enabled = false;

            FillIPList();
        }

        private void txtMsg_KeyPress(object sender, KeyPressEventArgs e)
        {
            // If Enter is pressed
            if (e.KeyChar == 13)
                btnSend_Click(sender, e);
        }

        private void cmbSourceIP_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtIP.Text = cmbSourceIP.SelectedItem.ToString().Remove(cmbSourceIP.SelectedItem.ToString().LastIndexOf('.') + 1);

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (btnConnect.Text == "Connect")
            {
                if (Start())
                {
                    btnSend.Enabled = true;
                    txtIP.Enabled = false;
                    btnConnect.Text = "Disconnect";
                    lblStatus.Text = "Connected...";
                    txtMsg.Focus();
                }

            }
            else
            {
                if (Stop())
                {
                    btnSend.Enabled = false;
                    btnConnect.Enabled = false;
                    txtIP.Enabled = true;
                    btnConnect.Text = "Connect";
                    lblStatus.Text = "Disconnected...";
                }

                MessageBox.Show("Disconnect is not working for now! Please restart the application!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            if (txtIP.Text.Trim() == "")
            {
                MessageBox.Show("IP address can not be empty!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            try
            {
                Ping p = new Ping();

                PingReply pre = p.Send(txtIP.Text.Trim());

                if (pre.Status == IPStatus.Success)
                {
                    btnConnect.Enabled = true;
                    lblStatus.Text = "Connection seems good! You can connect to that IP";

                    destIP = txtIP.Text.Trim();
                    srcIP = cmbSourceIP.SelectedItem.ToString();
                }
                else
                {
                    MessageBox.Show("It seems there is a problem with the destination IP.\r\nPlease check it and try again.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    lblStatus.Text = "";
                    return;
                }


            } // try
            catch (Exception exp)
            {
                Debug.WriteLine("ERROR: TestPing: " + exp.Message);
                lblStatus.Text = "There was an error while testing the connection, check debug console!";
            }

            //Start();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                string msg = txtMsg.Text.Trim();

                if (msg == "")
                {
                    MessageBox.Show("Message can not be empty!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                lblStatus.Text = "Sending (pinging) the message";

                Ping p = new Ping();

                string ip = txtIP.Text.Trim();

                byte[] temp = new byte[bufferLen];

                int len = msg.Length - (msg.Length % bufferLen) + bufferLen;

                msg = msg.PadRight(len);

                int rep = len / bufferLen;

                for (int i = 0; i < rep; i++)
                {
                    temp = Encoding.UTF8.GetBytes(msg.Substring(32 * i, 32));

                    PingReply pr = p.Send(ip, 0x1338, temp);

                    if (pr.Status != IPStatus.Success)
                        lblStatus.Text = "!!! " + pr.Status.ToString();
                }

                rtxtChat.SelectionColor = Color.Black;
                rtxtChat.AppendText(msg.Trim());
                rtxtChat.AppendText(Environment.NewLine);

                lblStatus.Text = "Send success";
                txtMsg.Clear();

            } // try
            catch (Exception exp)
            {
                Debug.WriteLine("ERROR: PingSend: " + exp.Message);
                lblStatus.Text = "There was an error while sending the ping, check debug console!";
            }
        }

        #endregion

        #region Methods

        bool Start()
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);

                IPAddress ip = IPAddress.Parse(cmbSourceIP.SelectedItem.ToString().Trim());

                IPEndPoint ipEP = new IPEndPoint(ip, 0);

                _socket.Bind(ipEP);

                int rcvall;
                
                unchecked
                {
                    rcvall = (int)2550136833;
                }

                _socket.IOControl(rcvall, new byte[] { 1, 0, 0, 0 }, new byte[] { 0, 0, 0, 0 });

                _socket.ReceiveBufferSize = _buffer.Length;

                _socket.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref remoteRawEndPoint, new AsyncCallback(ResultArrived), _socket);

                AppendText("Started..." + Environment.NewLine);

                /* DEBUG
                t0 = DateTime.Now.Ticks;
                */

                return true;

            } // try
            catch (Exception exp)
            {
                Debug.WriteLine("ERROR: Start: " + exp.Message);
                return false;
            }
        }

        void ResultArrived(IAsyncResult iar)
        {
            /* DEBUG
            long t1 = DateTime.Now.Ticks;
            Debug.WriteLine("Result is arrived! " + (t1-t0).ToString().PadLeft(10) + " " + c++);
            t0 = t1;
            */

            Socket _tempsocket = (Socket)iar.AsyncState;

            try
            {
                int toRead = _tempsocket.EndReceiveFrom(iar, ref remoteRawEndPoint);

                if (toRead > 20)
                {
                    byte[] _ipad = new byte[4];
                    byte[] _ipad2 = new byte[4];

                    Array.Copy(_buffer, 12, _ipad, 0, 4);
                    Array.Copy(_buffer, 16, _ipad2, 0, 4);

                    IPAddress _ipadip = new IPAddress(_ipad);
                    IPAddress _ipadip2 = new IPAddress(_ipad2);

                    string ipad = _ipadip.ToString();
                    string ipad2 = _ipadip2.ToString();

                    Debug.WriteLine("ipad : " + ipad);
                    Debug.WriteLine("ipad2: " + ipad2);

                    if (ipad == destIP && ipad2 == srcIP && _buffer[20] == 8 && _buffer[21] == 0)
                    {
                        rtxtChat.SelectionColor = Color.Red;
                        rtxtChat.AppendText(Encoding.UTF8.GetString(_buffer, 28, toRead - 28) + Environment.NewLine);
                    }

                    //                                      AppendText("00 01 02 03 04 05 06 07 08 09 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28" + Environment.NewLine);
                    AppendText(Environment.NewLine + "                  VH ST LENGT IDENT FLAGS T  P  ChkSm |----S----| |----D----| Ty Co ChkSm IDENT SEQNU |-DATA->" + Environment.NewLine);

                    /*
                    AppendText("S: " + (new IPAddress(BitConverter.ToInt32(_buffer,12)).ToString()) + 
                              " D: " + (new IPAddress(BitConverter.ToInt32(_buffer,16)).ToString()) + Environment.NewLine);
                    */
                    AppendText(ipad.PadRight(15) + "H: " + BitConverter.ToString(_buffer, 0, 28).Replace('-', ' ') +
                               " " + Encoding.UTF8.GetString(_buffer, 28, toRead - 28) + Environment.NewLine);

                }


            } // try
            catch (Exception exp)
            {
                Debug.WriteLine("ERROR: ResultArrived: " + exp.Message);
            }
            finally
            {
                _tempsocket.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref remoteRawEndPoint, new AsyncCallback(ResultArrived), _tempsocket);
            }

        }

        bool Stop()
        {
            try
            {
                if (_socket != null)
                {
                    //_socket.Shutdown(SocketShutdown.Both);
                    //_socket.Disconnect(true);
                    //_socket.Close();
                    //_socket.Dispose();
                    //_socket = null;
                }

                return false;

            } // try
            catch (Exception exp)
            {
                Debug.WriteLine("ERROR: Stop: " + exp.Message);
                return false;
            }

        }

        void AppendText(string str)
        {
            try
            {
                Debug.Write(str);
                //rtxtChat.AppendText(str);
                //rtxtChat.SelectionStart = rtxtChat.Text.Length;

            } // try
            catch (Exception exp)
            {
                Debug.WriteLine("ERROR: AppendText: " + exp.Message);
            }

        }

        void FillIPList()
        {
            IPHostEntry ipHE = Dns.GetHostEntry(Dns.GetHostName());

            cmbSourceIP.Items.Clear();

            foreach (IPAddress _ip in ipHE.AddressList)
                if (_ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    cmbSourceIP.Items.Add(_ip.ToString());
                }

            cmbSourceIP.SelectedIndex = 0;
        }

        #endregion

    }
}
