Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Text

Module Program
    Sub Main(args As String())

        Console.WriteLine("Sender")

        Try

            Dim ClientUdp = New UdpClient()

            Dim ServerEp = New IPEndPoint(IPAddress.Any, 1024)

            ClientUdp.EnableBroadcast = True

            While True
                Dim txt As String = Console.ReadLine()
                Dim RequestData = Encoding.UTF32.GetBytes(txt)
                ClientUdp.Send(RequestData, RequestData.Length, New IPEndPoint(IPAddress.Broadcast, 1024))
                If (txt.Equals("exit")) Then
                    ClientUdp.Close()
                    Environment.Exit(0)
                End If
                Dim ServerResponseData = ClientUdp.Receive(ServerEp)
                Dim ServerResponse = Encoding.UTF32.GetString(ServerResponseData)
                Console.WriteLine("Recived {0} from {1}", ServerResponse, ServerEp.Address.ToString())
            End While
            ClientUdp.Close()

        Catch e As Exception

            Console.WriteLine(e.ToString())
        End Try

        Console.WriteLine("Press Any Key to Continue")
        Console.ReadKey()
    End Sub
End Module
