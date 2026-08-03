using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RCM_Coop.Network.GameProtocols;
namespace RCM_Coop.Network.Helpers{

    internal class PacketReader{
        int read_index = 0;
        byte[] packet;
        PacketReader(byte[] packet) {
            this.packet = packet;
        }
        public packet_protocol DeserializeProtocol(){
            if (read_index >= packet.Length) return packet_protocol.none;
            packet_protocol protocol = (packet_protocol)packet[read_index];
            read_index += 1;
            return protocol;
        }
        public string DeserializeString(){
            int endIndex = Array.IndexOf(packet, (byte)0, read_index);
            if (endIndex == -1) throw new Exception("Null terminator not found in packet");
            string result = Encoding.UTF8.GetString(packet, read_index, endIndex - read_index);
            read_index = endIndex + 1;
            return result;
        }
    }
}
