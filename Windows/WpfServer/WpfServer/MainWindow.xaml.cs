using System;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using ArkaneSystems.Arkane.Zeroconf;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

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
        readonly InputSimulator inputSimulator = new InputSimulator();

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Changes the input type to be simulated to W,A,S & D keys when BtnWASD is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected internal void BtnWASD_Click(object sender, RoutedEventArgs e)
        {
            if(BtnArrowKeys.IsEnabled == false)
            {
                BtnArrowKeys.IsEnabled = true;
                BtnWASD.IsEnabled = false;
            }
        }

        /// <summary>
        /// Changes the input type to be simulated to the arrow keys when BtnArrowKeys is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected internal void BtnArrowKeys_Click(object sender, RoutedEventArgs e)
        {
            if (BtnWASD.IsEnabled == false)
            {
                BtnWASD.IsEnabled = true;
                BtnArrowKeys.IsEnabled = false;
            }
        }

        /// <summary>
        /// Register the Zeroconf service and initialises the receiving thread for communication with the client.
        /// If pressed again, the button will terminate the receiving thread to end communication with the client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected internal void BtnStart_Click(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// Receives and handles message from client through a UDP connection on port:1024.
        /// If message is ast_discover, the server will retrieve the sender's name and IP address and
        /// send an acknowledgement message with the server's IP address.
        /// 
        /// If the message is astv_disconnect, the service will be ended.
        /// 
        /// Anything else received will be assumed to be an input and passed to the GenerateInput(string) method.
        /// </summary>
        protected internal void GetMessage()
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

                    DisplayMessage("Service Ended", true);

                    //  use dispatcher to access object from a different thread
                    this.Dispatcher.Invoke(() =>
                    {
                        BtnStart.Content = "Start Services";
                    });

                    break;
                }

                if (!string.IsNullOrEmpty(smessage) && service_Running)
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

        /// <summary>
        /// Simluates keyboard inputs. 
        /// Currently, it only generates W, A, S & D keys or Up, Down, Left & Right arrow keys.
        /// anything else will not be recognised as a valid input.
        /// </summary>
        /// <param name="client_input">Client's message</param>
        protected internal void GenerateInput(string client_input)
        {
            try
            {
                if (BtnArrowKeys.IsEnabled == false)
                {
                    if (client_input == "astv_up") inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_W);
                    else if (client_input == "astv_down") inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_S);
                    else if (client_input == "astv_left") inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_A);
                    else if (client_input == "astv_right") inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_R);
                    else
                    {
                        DisplayMessage("Input not recognised", true);
                        Console.WriteLine("Input not recognised");
                        return;
                    }
                }
                else
                {
                    if (client_input == "astv_up") inputSimulator.Keyboard.KeyPress(VirtualKeyCode.UP);
                    else if (client_input == "astv_down") inputSimulator.Keyboard.KeyPress(VirtualKeyCode.DOWN);
                    else if (client_input == "astv_left") inputSimulator.Keyboard.KeyPress(VirtualKeyCode.LEFT);
                    else if (client_input == "astv_right") inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RIGHT);
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

        /// <summary>
        /// Sets the message to be displayed in the server's list box.
        /// </summary>
        /// <param name="message">Message to be displayed</param>
        /// <param name="fromServer">If the message is from the server or the client</param>
        protected internal void DisplayMessage(string message, bool fromServer)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message">Message to be sent to client</param>
        /// <param name="udp">UDP client used to send message from server</param>
        /// <param name="end">IP endpoint representing the client's machine</param>
        protected internal void SendMessage(string message, UdpClient udp, IPEndPoint end)
        {
            byte[] messageByte = System.Text.Encoding.UTF8.GetBytes(message);
            var byteSent = udp.Send(messageByte, messageByte.Length, end);
            DisplayMessage(message, true);
            Console.WriteLine("Sent to client: bytes: {0} - {1}", byteSent, message);
        }

        /// <summary>
        /// Updates the server's listbox.
        /// Because this method is used in both, the main and receiving threads, it uses the Dispatcher.checkAccess()
        /// to handle cases where thread cannot access the method.
        /// This is handled by using Dispatcher.BeginInvoke().
        /// </summary>
        protected internal void Msg()
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

        /// <summary>
        /// Ends the Zeroconf service by closing the communication between the client and server through the termination
        /// of the receiving thread.
        /// </summary>
        protected internal void EndZeroconfService()
        {
            if (!service_Running) return;
            try
            {
                string closeMessage = "\n Shutting down assistive technology server [" + DateTime.Now + "]";
                Console.WriteLine(closeMessage);
                DisplayMessage(closeMessage, true);

                //  Send the last disconnect message if the client's information is still available
                if (this.sender.Address != null && this.sender.Address.ToString() != "0.0.0.0")
                {
                    SendMessage("astv_disconnect", receiver, this.sender);
                }

                service_Running = false;

                //  Terminate thread
                if (ctThread != null)
                    if (ctThread.IsAlive)
                    {
                        ctThread.Abort();
                    }

                
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
