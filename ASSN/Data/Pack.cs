namespace ASSN
{
    public enum PackType
    {
        Connected,
        Data,
        Disconnected
    }
    public class Pack
    {
        public int connection_id;
        public PackType pack_type;
        public byte[] data;
        public Pack(int connection_id, PackType pack_type, byte[] data)
        {
            this.connection_id = connection_id;
            this.pack_type = pack_type;
            this.data = data;
        }
    }
}