using System;
using System.Net.Sockets;
using System.Text;

namespace ASSN_Server
{
    internal class Program
    {
        //Server
        public static void Main(string[] args)
        {
            Console.Title = "Server";
            Server.OnReceiveData += SampleServer;
            Print("Initializing...",ConsoleColor.Yellow);
            Server.SetupServer();
            Print("Server initialization completed",ConsoleColor.Green);
            Console.ReadLine(); // When we press enter close everything
            Server.CloseAllSockets();
        }

        public static void SampleServer(byte[] buffer,Socket sender)
        {
            var text = Encoding.ASCII.GetString(buffer);
            Print(">>: " + text,ConsoleColor.Yellow);

            switch (text.ToLower())
            {
                // Client requested time
                case "time":
                {
                    Print("[Request.time]",ConsoleColor.Green);
                    var data = Encoding.ASCII.GetBytes(DateTime.Now.ToLongTimeString());
                    sender.Send(data);
                    Print("[Sent back:  Time]",ConsoleColor.Green);
                    break;
                }
                // Client wants to exit gracefully
                case "exit":
                    // Always Shutdown before closing
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                    Server.ClientSockets.Remove(sender);
                    Print("[Client disconnected]",ConsoleColor.Red);
                    break;
                default:
                {
                    Print("[Request.invalid]",ConsoleColor.Red);
                    var data = Encoding.ASCII.GetBytes("Invalid request");
                    sender.Send(data);
                    Print("[Sent back:  Invalid request]",ConsoleColor.Red);
                    break;
                }
            }
        }

        public static void Print(string msg, ConsoleColor color=ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
