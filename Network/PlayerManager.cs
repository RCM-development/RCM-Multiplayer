using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace RCM_Coop.Network{

    public class PlayerManager{
        public PlayerManager() { 
            self = this;
        }
        static PlayerManager self = null;
        public class Player{
            public byte id;
            public string username;
            public Color color;
        }
        byte our_player_id = 255;
        List<Player> players = new();

        public void AddOurselves(byte id, Color color){
            players.Add(new() { username = "local player", id = id, color = color });
            our_player_id = id;
        }
        public static byte GetHostPlayerID() => 0;
        public static byte GetOurPlayerID() {
            if (self == null) return 255;

            return self.our_player_id;
        }
        public void AddPlayer(string username, byte id, Color color){
            if (id != GetOurPlayerID())
                players.Add(new() { username = username, id = id, color = color });
        }
            
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
        public static Player GetPlayer(byte id){
            if (self == null) return null;
            foreach (var item in self.players)
                if (item.id == id) 
                    return item;
            return null;
        }
        public List<Player> GetPlayersList() => players;

        public static IEnumerable<Player> AllPlayers(){
            if (self == null) yield break;

            foreach (var p in self.players)
                yield return p;
        }

    }
}
