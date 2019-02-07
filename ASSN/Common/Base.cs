// common code used by server and client
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace ASSN
{
    public abstract class Base
    {
        //数据包列
        protected ConcurrentQueue<Pack> receive_queue = new ConcurrentQueue<Pack>();

        //列深过高会导致缓慢
        public static int message_queue_size_warning = 100000;

        public bool GetNextMessage(out Pack pack)=>receive_queue.TryDequeue(out pack);

        //降低CPU但提高带宽,关闭了Nagle's algorithm
        public bool no_delay = true;

        public int send_timeout = 5000;

        protected static bool SendMessagesBlocking(NetworkStream stream, byte[][] messages)
        {
            try
            {
                var packet_size = messages.Sum(t => sizeof(int) + t.Length);
                // 构造数据包
                var payload = new byte[packet_size];
                var position = 0;
                foreach (var t in messages)
                {
                    var header = Utils.IntToBytesBigEndian(t.Length);
                    Array.Copy(header, 0, payload, position, header.Length);
                    Array.Copy(t, 0, payload, position + header.Length, t.Length);
                    position += header.Length + t.Length;
                }
                stream.Write(payload, 0, payload.Length);
                return true;
            }
            catch (Exception exception)
            {
                Logger.Log("Send: stream.Write exception: " + exception);
                return false;
            }
        }

        // 解包
        protected static bool ReadMessageBlocking(NetworkStream stream, out byte[] content)
        {
            content = null;
            // 数据标头信息
            var header = new byte[4];
            if (!stream.ReadExactly(header, 4))
                return false;
            var size = Utils.BytesToIntBigEndian(header);
            // 读取内容
            content = new byte[size];
            return stream.ReadExactly(content, size);
        }

        // 循环检测收包
        protected static void ReceiveLoop(int connection_id, TcpClient client, ConcurrentQueue<Pack> receive_queue)
        {
            var stream = client.GetStream();
            var message_queue_last_warning = DateTime.Now;
            try
            {
                // 存包
                receive_queue.Enqueue(new Pack(connection_id, PackType.Connected, null));

                while (true)
                {
                    // 读取下一个包
                    if (!ReadMessageBlocking(stream, out var content))break;

                    // 推入列
                    receive_queue.Enqueue(new Pack(connection_id, PackType.Data, content));
                    if (receive_queue.Count <= message_queue_size_warning) continue;
                    var elapsed = DateTime.Now - message_queue_last_warning;
                    if (!(elapsed.TotalSeconds > 10)) continue;
                    Logger.LogWarning("ReceiveLoop: messageQueue is getting big(" + receive_queue.Count + "), try calling GetNextMessage more often. You can call it more than once per frame!");
                    message_queue_last_warning = DateTime.Now;
                }
            }
            catch (Exception exception)
            {
                Logger.Log("ReceiveLoop: finished receive function for connectionId=" + connection_id + " reason: " + exception);
            }
            stream.Close();
            client.Close();

            // 储存一个断开链接的特殊包
            receive_queue.Enqueue(new Pack(connection_id, PackType.Disconnected, null));
        }

        // 循环发包
        protected static void SendLoop(int connectionId, TcpClient client, AssnQueue<byte[]> sendQueue, ManualResetEvent sendPending)
        {
            var stream = client.GetStream();

            try
            {
                while (client.Connected) 
                {
                    sendPending.Reset(); 
                    if (sendQueue.TryDequeueAll(out var messages))
                        if (!SendMessagesBlocking(stream, messages))return;
                    sendPending.WaitOne();
                }
            }
            catch (ThreadAbortException)
            {

            }
            catch (ThreadInterruptedException)
            {
               
            }
            catch (Exception exception)
            {
                Logger.Log("SendLoop Exception: connectionId=" + connectionId + " reason: " + exception);
            }
        }
    }
}
