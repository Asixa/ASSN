using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ASSN_Client
{
    internal class Client
    {
     
        private static readonly Socket ClientSocket = new Socket
           (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private const int Port = 100;
        private const int BufferSize = 1024;

        // ReSharper disable twice InconsistentNaming
        public delegate void OnReceiveDataDelegate(byte[] data);
        public static OnReceiveDataDelegate OnReceiveData;
        public static void ConnectToServer()
        {
            var attempts = 0;

            while (!ClientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection attempt " + attempts);
                    ClientSocket.Connect(IPAddress.Loopback, Port);
                }
                catch (SocketException)
                {
                    Console.Clear();
                }
            }

            Console.Clear();
            Console.WriteLine("Connected");
        }

        public static void RequestLoop()
        {
            while (true)
            {
                SendRequest();
                ReceiveResponse();
            }
        }

        /// <summary> Close socket and exit program.</summary>
        public static void Exit()
        {
            SendString("exit"); // Tell the server we are exiting
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            Environment.Exit(0);
        }

        private static void SendRequest()
        {
            Console.Write("<<: ");
            var request = Console.ReadLine();
            SendString(request);

            if (request != null && request.ToLower() == "exit") Exit();
            
        }

        ///<summary> Sends a string to the server with ASCII encoding. </summary>
        public static void SendString(string text)
        {
            var buffer = Encoding.ASCII.GetBytes(text);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        ///<summary> Sends bytes to the server. </summary>
        public static void SendData(byte[] buffer)=>ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        
        private static void ReceiveResponse()
        {
            var buffer = new byte[BufferSize];
            var received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            OnReceiveData.Invoke(data);
        }
    }
}
