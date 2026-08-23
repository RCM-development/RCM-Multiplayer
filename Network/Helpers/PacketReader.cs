using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RCM_Coop.Network.GameProtocols;
namespace RCM_Coop.Network.Helpers{

    public class PacketReader{
        int read_index = 0;
        byte[] packet;
        public PacketReader(byte[] packet) {
            this.packet = packet;
        }
        public string DeserializeString(){
            int endIndex = Array.IndexOf(packet, (byte)0, read_index);
            if (endIndex == -1) return "";
            string result = Encoding.UTF8.GetString(packet, read_index, endIndex - read_index);
            read_index = endIndex + 1;
            return result;
        }
        public byte DeserializeByte(){
            read_index += 1;
            if (read_index > packet.Length) return 0;
            return packet[read_index-1];
        }
        public short DeserializeShort(){
            read_index += 2;
            if (read_index > packet.Length) return 0;
            return (short)(packet[read_index-2] | (packet[read_index-1] << 8));
        }
        public int DeserializeInt(){
            read_index += 4;
            if (read_index > packet.Length) return 0;
            return packet[read_index-4] | (packet[read_index-3] << 8) | (packet[read_index-2] << 16) | (packet[read_index-1] << 24);
        }
        public float DeserializeFloat(){
            read_index += 4;
            if (read_index > packet.Length) return 0;
            return BitConverter.ToSingle(packet, read_index-4);
        }
    }
}
