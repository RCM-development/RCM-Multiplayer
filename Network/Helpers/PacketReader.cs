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
            if (read_index >= packet.Length) return 0;
            read_index += 1;
            return packet[read_index-1];
        }
    }
}
