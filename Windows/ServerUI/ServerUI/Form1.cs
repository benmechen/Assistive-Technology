using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using ArkaneSystems.Arkane.Zeroconf;
using System.Threading;

namespace ServerUI
{
    public partial class fmServer : Form
    {

        readonly UdpClient receiver = new UdpClient(1024);
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 1024);
        string tcp_message;
        static readonly RegisterService service = new RegisterService();
        Thread ctThread;

        public fmServer()
        {
            InitializeComponent();
        }

        private void fmServer_Load(object sender, EventArgs e)
        {

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Sample service publisher using arkane.Mono.Zeroconf version\n");
            service.Name = "Assistive Technology Server";
            service.RegType = "_assistive-tech._udp";
            service.ReplyDomain = "local.";
            service.Port = 1024;


            TxtRecord txt_record = new TxtRecord
            {
                { "service", "Assistive Technology Technology" },
                { "version", "1.0.0" }
            };
            service.TxtRecord = txt_record;

            try
            {
                service.Register();
                
                string txtmsg = "service has been registered";
                Console.WriteLine("{0} " + txtmsg, service.Name);
                tcp_message = txtmsg;
                msg();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Service Error: {0}", ex.ToString());
            }

            ctThread = new Thread(getMessage);
            ctThread.Start();
            btnStart.Enabled = false;
        }


        private void getMessage()
        {
            string hostName = Dns.GetHostName();
            string ipaddress = Dns.GetHostEntry(hostName).AddressList[0].ToString();
            while (true)
            {
                byte[] rmessage = receiver.Receive(ref sender);
                string smessage = System.Text.Encoding.UTF8.GetString(rmessage);
                if(!string.IsNullOrEmpty(smessage))
                {
                    tcp_message = "Client: " + smessage;
                    msg();
                    sendMessage("astv_ack", receiver, sender);
                    tcp_message = "Server: astv_ack";
                    msg();
                    if (smessage == "astv_discover")
                    {
                        Console.WriteLine("Discover call from client: " + sender.Address.ToString());
                        sendMessage("astv_shake:" + ipaddress, receiver, sender);
                        tcp_message = "astv_shake:" + ipaddress;
                        msg();
                        Console.WriteLine("Sent handshake: astv_shake to address:" + sender.Address.ToString());
                    }
                    else if (smessage == "astv_disconnect") break;
                }
            }
        }

        private void sendMessage(string message, UdpClient udp, IPEndPoint end)
        {
            byte[] messageByte = System.Text.Encoding.UTF8.GetBytes(message);
            var byteSent = udp.Send(messageByte, messageByte.Length, end);
            tcp_message = "Server: " + message;
            msg();
            Console.WriteLine("Sent to client: bytes: {0} - {1}", byteSent, message);
        }

        private void msg()
        {
            if(this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(msg));
            }
            else
            {
                lstTranscript.Items.Add(tcp_message);
            }
        }

        private void fmServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            string closeMessage = "\n Shutting down assistive technology server [" + DateTime.Now + "]";
            Console.WriteLine(closeMessage);
            if (this.sender.Address != null && this.sender.Address.ToString() != "0.0.0.0")
            {
                sendMessage("astv_disconnect", receiver, this.sender);
            }
            if(ctThread.IsAlive) ctThread.Abort();
            service.Dispose();
            receiver.Close();
        }
    }
}
