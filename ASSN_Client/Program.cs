using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASSN_Client
{
    class Program
    {
        public static int Main(string[] args)
        {
            Console.Title = "Client";
            Client.OnReceiveData += OutputData;
            Client.ConnectToServer();
            Client.RequestLoop();
            Client.Exit();
            return 0;
        }


        ///<summary> Sends bytes to the server. </summary>
        public static void OutputData(byte[] data)
        {
            var text = Encoding.ASCII.GetString(data);
            Console.WriteLine(">> " + text);
        }
    }
}
