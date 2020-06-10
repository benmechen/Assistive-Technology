using System;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using ArkaneSystems.Arkane.Zeroconf;
using System.Threading;

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

        readonly InputGenerator.Keyboard keyboard = new InputGenerator.Keyboard();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnWASD_Click(object sender, RoutedEventArgs e)
        {
            if(BtnArrowKeys.IsEnabled == false)
            {
                BtnArrowKeys.IsEnabled = true;
                BtnWASD.IsEnabled = false;
            }
        }

        private void BtnArrowKeys_Click(object sender, RoutedEventArgs e)
        {
            if (BtnWASD.IsEnabled == false)
            {
                BtnWASD.IsEnabled = true;
                BtnArrowKeys.IsEnabled = false;
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (service_Running == false)
            {
                service_Running = true;
                Console.WriteLine("Sample service publisher using arkane.Mono.Zeroconf version\n");
                service.Name = "Assistive Technology Server";
                service.RegType = "_assistive-tech._udp";
                service.ReplyDomain = "local.";
                service.Port = 1024;
                //set timeout time to 10 seconds
                receiver.Client.ReceiveTimeout = 10000;
                TxtRecord txt_record = new TxtRecord
                {
                    { "service", "Assistive Technology Technology" },
                    { "version", "1.0.0" }
                };
                service.TxtRecord = txt_record;
                try
                {
                    //  Register the service if it is not registered
                    //  The if mainly serves to prevent a second registration
                    if (!service_registered)
                    {
                        service.Register();
                        service_registered = true;
                    }
                    string txtMsg = "service has been registered";
                    Console.WriteLine("{0} " + txtMsg, service.Name);
                    DisplayMessage(txtMsg, true);

                    //  Change the text of the Start button to Stop Services
                    BtnStart.Content = "Stop Service";
                }
                catch (Exception ex)
                {
                    DisplayMessage(ex.Message, true);
                    Console.WriteLine("Service Error: {0}", ex.ToString());
                }

                //  Start a thread with the method GetMessge
                ctThread = new Thread(GetMessage);
                ctThread.Start();
            }
            else
            {
                EndZeroconfService();
                BtnStart.Content = "Start Services";
            }
        }

        private void GetMessage()
        {
            string client_name = "";

            //  Get the current device's name and IP address
            string hostName = Dns.GetHostName();
            string ipaddress = Dns.GetHostEntry(hostName).AddressList[0].ToString();

            while (service_Running)
            {
                byte[] rmessage;
                string smessage = "";
                try
                {
                    //  Read data comming through port 1024 using UDP
                    rmessage = receiver.Receive(ref sender);

                    //  Convert byte mesage from client to string
                    smessage = System.Text.Encoding.UTF8.GetString(rmessage);
                }
                catch(Exception ex)
                {
                    DisplayMessage(ex.Message, true);
                    Console.WriteLine("Failure to receive UDP message - {0}", ex.ToString());
                    EndZeroconfService();
                    break;
                }
                if (!string.IsNullOrEmpty(smessage))
                {
                    DisplayMessage(smessage, false);
                    SendMessage("astv_ack", receiver, sender);
                    if (smessage.Contains("astv_discover"))
                    {
                        //  Retreives substring of client message starting from ":"
                        //  This should contain the client's name
                        client_name = smessage.Substring(smessage.IndexOf(":") + 1);

                        if (client_name.Length < 1) lblDevice.Content = "Error getting device name";
                        else
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                lblDevice.Content = client_name;
                            });
                        }

                        //  Display name and IP address of client
                        string tempMessage = "Discover call from client " + client_name + ": " + sender.Address.ToString();
                        Console.WriteLine(tempMessage);
                        DisplayMessage(tempMessage, true);

                        //  Send acknowledgement message back to client
                        SendMessage("astv_shake:" + ipaddress, receiver, sender);
                        Console.WriteLine("Sent handshake: astv_shake to address:" + sender.Address.ToString());
                    }
                    else if (smessage == "astv_disconnect")
                    {
                        EndZeroconfService();
                        break;
                    }
                    else
                    {
                        GenerateInput(smessage);
                    }
                }
            }
        }

        private void GenerateInput(string client_input)
        {
            try
            {
                if (BtnArrowKeys.IsEnabled == false)
                {
                    if (client_input == "astv_up") keyboard.Send(InputGenerator.Keyboard.ScanCodeShort.KEY_W);
                    else if (client_input == "astv_down") keyboard.Send(InputGenerator.Keyboard.ScanCodeShort.KEY_S);
                    else if (client_input == "astv_left") keyboard.Send(InputGenerator.Keyboard.ScanCodeShort.KEY_A);
                    else if (client_input == "astv_right") keyboard.Send(InputGenerator.Keyboard.ScanCodeShort.KEY_D);
                    else
                    {
                        DisplayMessage("Input not recognised", true);
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
                        DisplayMessage("Input not recognised", true);
                        Console.WriteLine("Input not recognised");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayMessage(ex.Message, true);
                Console.WriteLine("Error generating input: {0}", ex.ToString());
            }
        }

        private void DisplayMessage(string message, bool fromServer)
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
                Msg();
            }
        }

        private void SendMessage(string message, UdpClient udp, IPEndPoint end)
        {
            byte[] messageByte = System.Text.Encoding.UTF8.GetBytes(message);
            var byteSent = udp.Send(messageByte, messageByte.Length, end);
            DisplayMessage(message, true);
            Console.WriteLine("Sent to client: bytes: {0} - {1}", byteSent, message);
        }

        private void Msg()
        {
            //  If the method is unnaccessible to the calling thread invoke it.
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    LstTranscript.Items.Add(tcp_message);
                }));
            }
            else
            {
                LstTranscript.Items.Add(tcp_message);
            }
        }

        private void EndZeroconfService()
        {
            try
            {
                string closeMessage = "\n Shutting down assistive technology server [" + DateTime.Now + "]";
                Console.WriteLine(closeMessage);
                DisplayMessage(closeMessage, true);
                if (this.sender.Address != null && this.sender.Address.ToString() != "0.0.0.0")
                {
                    SendMessage("astv_disconnect", receiver, this.sender);
                }

                service_Running = false;
                if (ctThread != null)
                    if (ctThread.IsAlive)
                    {
                        
                        ctThread.Abort();
                        ctThread.Join();
                    }
                this.Dispatcher.Invoke(() =>
                {
                    BtnStart.Content = "Start Services";
                    BtnStart.IsEnabled = true;
                });

                DisplayMessage("Service Ended", true);
            }
            catch (Exception ex)
            {
                DisplayMessage(ex.Message, true);
                Console.WriteLine("Stopping Service Error: {0}", ex.ToString());
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (service_Running) EndZeroconfService();
            service.Dispose();
            receiver.Close();
            receiver.Dispose();
            Console.WriteLine("Service and TCP receiver have been disposed!");
        }
    }
}
