using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using ArkaneSystems.Arkane.Zeroconf;
using System.Threading;
using InputGenerator;

namespace WpfServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly UdpClient receiver = new UdpClient(1024);
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 1024);
        string tcp_message;
        static readonly RegisterService service = new RegisterService();
        bool service_Running = false;
        bool service_registered = false;
        Thread ctThread;

        InputGenerator.Keyboard keyboard = new InputGenerator.Keyboard();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnWASD_Click(object sender, RoutedEventArgs e)
        {
            if(btnArrowKeys.IsEnabled == false)
            {
                btnArrowKeys.IsEnabled = true;
                btnWASD.IsEnabled = false;
            }
        }

        private void btnArrowKeys_Click(object sender, RoutedEventArgs e)
        {
            if (btnWASD.IsEnabled == false)
            {
                btnWASD.IsEnabled = true;
                btnArrowKeys.IsEnabled = false;
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (service_Running == false)
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

                    if (!service_registered)
                    {
                        service.Register();
                        service_registered = true;
                    }
                    string txtmsg = "service has been registered";
                    Console.WriteLine("{0} " + txtmsg, service.Name);
                    displayMessage(txtmsg, true);
                    btnStart.Content = "Stop Service";
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
                btnStart.Content = "Start Services";
            }
        }

        private void getMessage()
        {
            string client_name = "";
            string hostName = Dns.GetHostName();
            string ipaddress = Dns.GetHostEntry(hostName).AddressList[0].ToString();

            while (true)
            {
                
                byte[] rmessage = receiver.Receive(ref sender);
                string smessage = System.Text.Encoding.UTF8.GetString(rmessage);
                if (!string.IsNullOrEmpty(smessage))
                {
                    displayMessage(smessage, false);
                    sendMessage("astv_ack", receiver, sender);
                    if (smessage.Contains("astv_discover"))
                    {
                        client_name = smessage.Substring(smessage.IndexOf(":") + 1);

                        if (client_name.Length < 1) lblDevice.Content = "Error getting device name";
                        else
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                lblDevice.Content = client_name;
                            });
                        }

                        string tempMessage = "Discover call from client " + client_name + ": " + sender.Address.ToString();
                        Console.WriteLine(tempMessage);
                        displayMessage(tempMessage, true);
                        sendMessage("astv_shake:" + ipaddress, receiver, sender);
                        Console.WriteLine("Sent handshake: astv_shake to address:" + sender.Address.ToString());
                    }
                    else if (smessage == "astv_disconnect")
                    {
                        endZeroconfService();
                        btnStart.Name = "Start Services";
                        btnStart.IsEnabled = true;
                        break;
                    }
                    else
                    {
                        generateInput(smessage);
                    }
                }
            }
        }

        private void generateInput(string client_input)
        {
            try
            {
                if (btnArrowKeys.IsEnabled == false)
                {
                    if (client_input == "astv_up") keyboard.Send(InputGenerator.Keyboard.ScanCodeShort.KEY_W);
                    else if (client_input == "astv_down") keyboard.Send(InputGenerator.Keyboard.ScanCodeShort.KEY_S);
                    else if (client_input == "astv_left") keyboard.Send(InputGenerator.Keyboard.ScanCodeShort.KEY_A);
                    else if (client_input == "astv_right") keyboard.Send(InputGenerator.Keyboard.ScanCodeShort.KEY_D);
                    else
                    {
                        displayMessage("Input not recognised", true);
                        Console.WriteLine("Input not recognised");
                        return;
                    }
                }
                else
                {
                    if (client_input == "astv_up") keyboard.Send(InputGenerator.Keyboard.ScanCodeShort.UP);
                    else if (client_input == "astv_down") keyboard.Send(InputGenerator.Keyboard.ScanCodeShort.DOWN);
                    else if (client_input == "astv_left") keyboard.Send(InputGenerator.Keyboard.ScanCodeShort.LEFT);
                    else if (client_input == "astv_right") keyboard.Send(InputGenerator.Keyboard.ScanCodeShort.RIGHT);
                    else
                    {
                        displayMessage("Input not recognised", true);
                        Console.WriteLine("Input not recognised");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                displayMessage(ex.ToString(), true);
                Console.WriteLine("Error generating input: {0}", ex.ToString());
            }
        }

        private void displayMessage(string message, bool fromServer)
        {
            if (!string.IsNullOrEmpty(message))
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
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    lstTranscript.Items.Add(tcp_message);
                }));
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
                if (ctThread != null)
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (service_Running) endZeroconfService();
            service.Dispose();
            receiver.Close();
            receiver.Dispose();
            Console.WriteLine("Service and TCP receiver have been disposed!");
        }
    }
}
