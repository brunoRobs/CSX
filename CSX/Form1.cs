using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace CSX
{
    public partial class Form1 : Form
    {
        clsCSX Server = new clsCSX();

        Thread ServerThread;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btnStart.Enabled = true;

            string IP = "";

            IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());

            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                {
                    IP = address.ToString();
                    break;
                }
            }

            txtIP.Text = IP;

            txtPort.Text = "80";

            txtLog.Text = "Log";

            txtSize.Text = "1024";

            txtClearTime.Text = "10";
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;

            btnExit.Enabled = false;

            btnStop.Enabled = true;

            ServerThread = new Thread(StartServer)
            {
                Name = "ServerThread",
                IsBackground = true
            };

            ServerThread.Start();
        }

        private void StartServer()
        {
            Server.Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory) + $"{txtLog.Text}.log";

            Server.LogSize = Int32.Parse(txtSize.Text);

            Server.ClearTime = Int32.Parse(txtClearTime.Text);

            Server.Start(txtIP.Text, txtPort.Text);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = false;

            btnStart.Enabled = true;

            btnExit.Enabled = true;

            ServerThread = null;

            Server.Stop();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
