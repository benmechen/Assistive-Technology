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
        bool service_Running = false;
        bool service_registered = false;
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
            if(service_Running == false)
            {
                service_Running = true;
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
                    
                    if(!service_registered)
                    {
                        service.Register();
                        service_registered = true;
                    }
                    string txtmsg = "service has been registered";
                    Console.WriteLine("{0} " + txtmsg, service.Name);
                    displayMessage(txtmsg, true);
                    btnStart.Text = "Stop Service";
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Service Error: {0}", ex.ToString());
                }

                ctThread = new Thread(getMessage);
                ctThread.Start();
            }
            else
            {
                endZeroconfService();
                btnStart.Text = "Start Services";
            }
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
                    displayMessage(smessage, false);
                    sendMessage("astv_ack", receiver, sender);
                    if (smessage == "astv_discover")
                    {
                        Console.WriteLine("Discover call from client: " + sender.Address.ToString());
                        sendMessage("astv_shake:" + ipaddress, receiver, sender);
                        Console.WriteLine("Sent handshake: astv_shake to address:" + sender.Address.ToString());
                    }
                    else if (smessage == "astv_disconnect")
                    {
                        endZeroconfService();
                        btnStart.Name = "Start Services";
                        btnStart.Enabled = true;
                        break;
                    }
                }
            }
        }

        private void displayMessage(string message, bool fromServer)
        {
            if(!string.IsNullOrEmpty(message))
            {
                if (fromServer)
                {
                    tcp_message = "Server: " + message;
                }
                else
                {
                    tcp_message = "Client: " + message;
                }
                msg();
            }
        }

        private void sendMessage(string message, UdpClient udp, IPEndPoint end)
        {
            byte[] messageByte = System.Text.Encoding.UTF8.GetBytes(message);
            var byteSent = udp.Send(messageByte, messageByte.Length, end);
            displayMessage(message, true);
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

        private void endZeroconfService()
        {
            try
            {
                string closeMessage = "\n Shutting down assistive technology server [" + DateTime.Now + "]";
                Console.WriteLine(closeMessage);
                displayMessage(closeMessage, true);
                if (this.sender.Address != null && this.sender.Address.ToString() != "0.0.0.0")
                {
                    sendMessage("astv_disconnect", receiver, this.sender);
                }
                if(ctThread != null)
                    if (ctThread.IsAlive) ctThread.Abort();

                
                service_Running = false;
                closeMessage = "Service Ended";
                displayMessage(closeMessage, true);
                Console.WriteLine(closeMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Stopping Service Error: {0}", ex.ToString());
            }

            
        }

        private void fmServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(service_Running) endZeroconfService();
            service.Dispose();
            receiver.Close();
            receiver.Dispose();
        }


    }
}
