using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCM_Coop.Network
{
    internal class GameProtocols
    {

        public enum packet_protocol : byte
        {
            none = 0,
            client_join_request,
            server_join_response,

            // server will send one of these to a client when they've connected
            server_game_status_loading, // includes map stage info for client to join & generate off of
            server_game_status_waiting, // includes initial game state data in packet
            server_game_status_started,

            client_game_status_ready,


        }

        void DeserializeProtocols(byte[] data)
        {
            //// read the first byte to determine the protocol
            //packet_protocol protocol = (packet_protocol)data[0];
            //switch (protocol)
            //{
            //    case packet_protocol.game_status:
            //        // read the next byte to determine the game status
            //        byte game_status = data[1];
            //        break;
            //    default:
            //        RCMManager.Log($"[Co-op] Unknown protocol {protocol}");
            //        break;
            //}
        }


        public static byte[] SerializeClientJoinRequest(string username, string password){
            int buffer_size = username.Length + password.Length + 3; // +2 for null terminators + 1 for protocol byte
            byte[] buffer = new byte[buffer_size];
            buffer[0] = (byte)packet_protocol.client_join_request;
            Encoding.UTF8.GetBytes(username, 0, username.Length, buffer, 1);
            Encoding.UTF8.GetBytes(password, 0, password.Length, buffer, username.Length + 2);
            return buffer;
        }


        
        //void DeserializeClientJoinRequest(byte[] data, out string username, out string password)
        //{

        //}
    }
}
