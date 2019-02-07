using System.IO;
using System.Net.Sockets;

namespace ASSN
{
    public static class StreamHelper
    {

        public static int ReadSafely(this NetworkStream stream, byte[] buffer, int offset, int size)
        {
            try
            {
                return stream.Read(buffer, offset, size);
            }
            catch (IOException)
            {
                return 0;
            }
        }

        public static bool ReadExactly(this NetworkStream stream, byte[] buffer, int amount)
        {
            var bytes_read = 0;
            while (bytes_read < amount)
            {
                var remaining = amount - bytes_read;
                var result = stream.ReadSafely(buffer, bytes_read, remaining);
                if (result == 0)return false;
                bytes_read += result;
            }
            return true;
        }
    }
}
