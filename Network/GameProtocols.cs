using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCM_Coop.Network
{
    internal class GameProtocols
    {

        enum packet_protocols : byte{
            // server will send one of these to a client when they've connected
            // server_game_status_menu, // will indicate that we're now in the menu, so to back out of the game // NOTE: will likely be redundant as end game exists prob
            server_game_status_loading, // includes map stage info for client to join & generate off of
            server_game_status_waiting, // includes initial game state data in packet
            server_game_status_started,
            server_game_status_jip_started, // server_game_status_loading & server_game_status_waiting & server_game_status_started combined, so we only have to send 1 packet to respond to JIP clients
            // client responds with these to with a ready indicator?
            client_game_status_ready,


        }

        void DeserializeProtocols(byte[] data)
        {
            // read the first byte to determine the protocol
            packet_protocols protocol = (packet_protocols)data[0];
            switch (protocol)
            {
                case packet_protocols.game_status:
                    // read the next byte to determine the game status
                    byte game_status = data[1];
                    break;
                default:
                    RCMManager.Log($"[Co-op] Unknown protocol {protocol}");
                    break;
            }
        }



    }
}
