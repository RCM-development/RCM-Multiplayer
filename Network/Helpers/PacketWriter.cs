using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RCM_Coop.Network.GameProtocols;
namespace RCM_Coop.Network.Helpers{

    public class PacketWriter{
        List<byte> written_data = new();
        public byte[] GetData() => written_data.ToArray();
        public void SerializeString(string s)
        {
            byte[] buffer = new byte[s.Length + 1];
            Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, 0);
            written_data.AddRange(buffer);
        }
        public void SerializeByte(byte b) => written_data.Add(b);
        public void SerializeShort(short value){
            written_data.Add((byte)(value & 0xFF));
            written_data.Add((byte)((value >> 8) & 0xFF));
        }
        public void SerializeInt(int value){
            written_data.Add((byte)(value & 0xFF));
            written_data.Add((byte)((value >> 8) & 0xFF));
            written_data.Add((byte)((value >> 16) & 0xFF));
            written_data.Add((byte)((value >> 24) & 0xFF));
        }
    }
}
