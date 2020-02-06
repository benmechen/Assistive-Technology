Imports System
Imports System.Net.Sockets
Imports System.Net
Imports Zeroconf
Imports System.Text
Imports System.Threading

Module Program
    Public PORT As Integer = 1024

    'Variables
    Private UdpBroadcaster As New UdpClient(PORT)
    Private UdpBroadcasterEndpoint As New IPEndPoint(IPAddress.Any, PORT)

    Public Sub Main()


        Console.WriteLine("Server")

        StartUdpListener()
    End Sub
    Private Sub StartUdpListener()


        Try
            Do
                Dim ReceivedBytes() As Byte = UdpBroadcaster.Receive(UdpBroadcasterEndpoint)
                Dim ReceivedString As String = System.Text.Encoding.UTF32.GetString(ReceivedBytes)
                Console.WriteLine("Received Message: " & ReceivedString)
                If (ReceivedString.Equals("exit")) Then
                    Environment.Exit(0)
                End If
                Send("Got your message!!")
            Loop
        Catch
            If Not UdpBroadcaster Is Nothing Then
                UdpBroadcaster.Close()
                UdpBroadcaster = Nothing
            End If

            Console.WriteLine("UDP connection lost, please try again later.")
        End Try
    End Sub

    Private Sub Send(txt As String)
        Dim BytesSent
        Dim BroadcastBytes() As Byte = System.Text.Encoding.UTF32.GetBytes("Receeived this message:: " & txt)
        BytesSent = UdpBroadcaster.Send(BroadcastBytes, BroadcastBytes.Length, UdpBroadcasterEndpoint)
        Console.WriteLine("UDP request sent successfully - Bytes Sent = " & BytesSent)
    End Sub
End Module
