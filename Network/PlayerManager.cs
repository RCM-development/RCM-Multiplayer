using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace RCM_Coop.Network{

    internal class PlayerManager{
        public class Player{
            public byte id;
            public string username;
        }
        byte our_player_id = 255;
        List<Player> players = new();

        public void AddOurselves(byte id){
            players.Add(new() { username = "local player", id = id });
            our_player_id = id;
        }
        public byte GetOurPlayerID() => our_player_id;
        public void AddPlayer(string username, byte id) => players.Add(new() { username = username, id = id });
        public void RemovePlayer(byte id){
            for (int i = 0; i < players.Count; i++){
                if (players[i].id == id){
                    players.RemoveAt(i);
                    return;
        }}}

        public bool IsUsernameTaken(string username){
            bool is_unique_username = true;
            foreach (var player in players)
                if (username == player.username)
                    is_unique_username = false;
            return is_unique_username;
        }
        public List<Player> GetPlayersList() => players;
    }
}
