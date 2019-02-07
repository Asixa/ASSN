namespace ASSN
{
    public static class Utils
    {
        public static byte[] IntToBytesBigEndian(int value)=>
            new[] {(byte)(value >> 24),(byte)(value >> 16),(byte)(value >> 8),(byte)value};
        public static int BytesToIntBigEndian(byte[] bytes)=>
            (bytes[0] << 24) |(bytes[1] << 16) |(bytes[2] << 8) |bytes[3];   
    }
}