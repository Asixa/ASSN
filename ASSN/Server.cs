using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ASSN
{
    public class Server : Base
    {
        public TcpListener listener;
        private Thread listener_thread;

        private class ClientToken
        {
            public readonly TcpClient client;
            public readonly AssnQueue<byte[]> send_queue = new AssnQueue<byte[]>();

            public readonly ManualResetEvent sendPending = new ManualResetEvent(false);

            public ClientToken(TcpClient client)=>this.client = client;
            
        }
        private readonly ConcurrentDictionary<int, ClientToken> clients = new ConcurrentDictionary<int, ClientToken>();

        private static int counter = 0;

        public static int NextConnectionId()
        {
            var id = Interlocked.Increment(ref counter);
            return id;
        }

        public bool Active => listener_thread != null && listener_thread.IsAlive;

        private void Listen(int port)
        {
            try
            {
                listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
                listener.Server.NoDelay = no_delay;
                listener.Server.SendTimeout = send_timeout;
                listener.Start();
                Logger.Log("Server: listening port=" + port);
                while (true)
                {

                    var client = listener.AcceptTcpClient();
                    var connection_id = NextConnectionId();
                    var token = new ClientToken(client);
                    clients[connection_id] = token;

                    var send_thread = new Thread(() =>
                    {
                        try
                        {
                            SendLoop(connection_id, client, token.send_queue, token.sendPending);
                        }
                        catch (ThreadAbortException){}
                        catch (Exception exception)
                        {
                            Logger.LogError("Server send thread exception: " + exception);
                        }
                    }) {IsBackground = true};
                    send_thread.Start();

                    var receiveThread = new Thread(() =>
                    {
                        try
                        {
                            ReceiveLoop(connection_id, client, receive_queue);
                            clients.TryRemove(connection_id, out ClientToken _);
                            send_thread.Interrupt();
                        }
                        catch (Exception exception)
                        {
                            Logger.LogError("Server client thread exception: " + exception);
                        }
                    }) {IsBackground = true};
                    receiveThread.Start();
                }
            }
            catch (ThreadAbortException exception)
            {
                Logger.Log("Server thread aborted." + exception);
            }
            catch (SocketException exception)
            {
                Logger.Log("Server Thread stopped." + exception);
            }
            catch (Exception exception)
            {
                Logger.LogError("Server Exception: " + exception);
            }
        }

        public bool Start(int port)
        {
            if (Active) return false;
            receive_queue = new ConcurrentQueue<Pack>();
            Logger.Log("Server: Start port=" + port);
            listener_thread = new Thread(() => { Listen(port); })
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            listener_thread.Start();
            return true;
        }

        public void Stop()
        {
            if (!Active) return;
            Logger.Log("Server: stopping...");
            listener?.Stop();
            listener_thread?.Interrupt();
            listener_thread = null;

            foreach (var kvp in clients)
            {
                var client = kvp.Value.client;
                try { client.GetStream().Close(); } catch {}
                client.Close();
            }
            clients.Clear();
        }

        public bool Send(int connection_id, byte[] data)
        {
            if (clients.TryGetValue(connection_id, out var token))
            {
                token.send_queue.Enqueue(data);
                token.sendPending.Set(); // interrupt SendThread WaitOne()
                return true;
            }
            Logger.Log("Server.Send: invalid connectionId: " + connection_id);
            return false;
        }
        public bool GetConnectionInfo(int connectionId, out string address)
        {
            if (clients.TryGetValue(connectionId, out var token))
            {
                address = ((IPEndPoint)token.client.Client.RemoteEndPoint).Address.ToString();
                return true;
            }
            address = null;
            return false;
        }

        public bool Disconnect(int connectionId)
        {
            if (!clients.TryGetValue(connectionId, out var token)) return false;
            token.client.Close();
            Logger.Log("Server.Disconnect connectionId:" + connectionId);
            return true;
        }
    }
}
