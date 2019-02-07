using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;

namespace ASSN
{
    public class Client : Base
    {
        public TcpClient client;
        private Thread receive_thread;
        private Thread send_thread;

        public bool Connected => client?.Client != null && client.Client.Connected;

        private volatile bool connecting;
        public bool Connecting => connecting;

        private readonly AssnQueue<byte[]> send_queue = new AssnQueue<byte[]>();

        private readonly ManualResetEvent send_pending = new ManualResetEvent(false);

        // 多线程函数
        private void ReceiveThreadFunction(string ip, int port)
        {
            try
            {
                client.Connect(ip, port);
                connecting = false;

                send_thread = new Thread(() => { SendLoop(0, client, send_queue, send_pending); })
                {
                    IsBackground = true
                };
                send_thread.Start();

                ReceiveLoop(0, client, receive_queue);
            }
            catch (SocketException exception)
            {
                Logger.Log("Client Recv: failed to connect to ip=" + ip + " port=" + port + " reason=" + exception);
                receive_queue.Enqueue(new Pack(0, PackType.Disconnected, null));
            }
            catch (Exception exception)
            {
                Logger.LogError("Client Recv Exception: " + exception);
            }
            send_thread?.Interrupt();
            connecting = false;
            client.Close();
        }

        public void Connect(string ip, int port)
        {
            if (Connecting || Connected) return;
            connecting = true;
            client = new TcpClient {NoDelay = no_delay, SendTimeout = send_timeout};
            receive_queue = new ConcurrentQueue<Pack>();
            send_queue.Clear();
            receive_thread = new Thread(() => { ReceiveThreadFunction(ip, port); }) {IsBackground = true};
            receive_thread.Start();
        }

        public void Disconnect()
        {
            if (!Connecting && !Connected) return;
            client.Close();
            receive_thread?.Join();
            send_queue.Clear();
            client = null;
        }

        public bool Send(byte[] data)
        {
            if (Connected)
            {
                send_queue.Enqueue(data);
                send_pending.Set();
                return true;
            }
            Logger.LogWarning("Client.Send: not connected!");
            return false;
        }
    }
}
