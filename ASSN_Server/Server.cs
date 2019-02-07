using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ASSN_Server
{
    class Server
    {
        private const int BufferSize = 1024;
        private const int MaxConnection = 100;
        private const int Port = 100;
        public static readonly Socket ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static readonly List<Socket> ClientSockets = new List<Socket>();
   
        private static readonly byte[] buffer = new byte[BufferSize];

        // ReSharper disable twice InconsistentNaming
        public delegate void OnReceiveDataDelegate(byte[] data, Socket sender);
        public static OnReceiveDataDelegate OnReceiveData;

        public static void SetupServer()
        {
            ServerSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
            ServerSocket.Listen(MaxConnection);
            ServerSocket.BeginAccept(AcceptCallback, null);
        }

        /// <summary>
        /// Close all connected client (we do not need to shutdown the server socket as its connections
        /// are already closed with the clients).
        /// </summary>
        public static void CloseAllSockets()
        {
            foreach (Socket socket in ClientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            ServerSocket.Close();
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;
            try
            {
                socket = ServerSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
                return;
            }

            ClientSockets.Add(socket);
            socket.BeginReceive(buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client connected");
            ServerSocket.BeginAccept(AcceptCallback, null);
        }

        public void SendData()
        {

        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            var current = (Socket)AR.AsyncState;
            int received;
            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                current.Close();
                ClientSockets.Remove(current);
                return;
            }

            var rec_buf = new byte[received];
            Array.Copy(buffer, rec_buf, received);

            OnReceiveData.Invoke(rec_buf, current);
            try
            {
                current.BeginReceive(buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, current);
            }
            catch (Exception e)
            {
                // ignored
            }
        }


    }
}
