using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestMod;
using UnityEngine;
using static RCM_Coop.CoopManager;
using static RCM_Coop.Network.GameProtocols;
namespace RCM_Coop.Network{

    public static class PlayerManager{
        public class Player{
            public byte id;
            public string username;
            public Color color;
            public float money = 0;
        }
        static byte our_player_id = 255;
        static List<Player> players = new();

        public static void AddOurselves(byte id, Color color){
            players.Add(new() { username = "local player", id = id, color = color });
            our_player_id = id;
        }
        public static byte GetHostPlayerID() => 0;
        public static byte GetOurPlayerID() {
            return our_player_id;
        }
        public static void AddPlayer(string username, byte id, Color color){
            if (id != GetOurPlayerID())
                players.Add(new() { username = username, id = id, color = color });
        }
            
        public static void RemovePlayer(byte id){
            for (int i = 0; i < players.Count; i++){
                if (players[i].id == id){
                    players.RemoveAt(i);
                    return;
        }}}

        public static bool IsUsernameTaken(string username){
            bool is_unique_username = true;
            foreach (var player in players)
                if (username == player.username)
                    is_unique_username = false;
            return is_unique_username;
        }
        public static Player GetPlayer(byte id){
            foreach (var item in players)
                if (item.id == id) 
                    return item;
            return null;
        }
        public static List<Player> GetPlayersList() => players;

        public static IEnumerable<Player> AllPlayers(){
            foreach (var p in players)
                yield return p;
        }

        public static int GetMoney(byte player){
            // fallback to host's money if nil player
            if (player == 255) player = GetHostPlayerID();
            Player p = GetPlayer(player);
            if (p == null){
                // assume no players, so just draw from actual game's bank
                return Bank.ActualBalance("Player");
            }
            else return (int)p.money;
        }
        public static void AddMoney(float amount){
            if (amount == 0.0f) return;

            foreach (Player p in players){
                if (p.id != GetHostPlayerID()){
                    p.money += amount;
                    CoopManager.SendServerInGamePacket(new ServerMoneyUpdate(p.id, p.money));
                }
            }
            // update & send host's money as well
            Patch_Bank_Deposit.Original("Player", amount);
            CoopManager.SendServerInGamePacket(new ServerMoneyUpdate(GetHostPlayerID(), Bank.ActualBalance("Player")));
        }
        public static void AddMoneyTo(float amount, byte player_id){
            if (amount == 0.0f) return;
            if (player_id == 255) player_id = GetHostPlayerID();

            Player p = GetPlayer(player_id);
            if (p == null){
                // fallback for if no multiplayer session
                if (player_id == GetHostPlayerID()){
                    Patch_Bank_Deposit.Original("Player", amount);
                    return;
                }
                RCMManager.Log($"[Co-op] failed to find player {player_id} to write money value to");
                return;
            }
            p.money += amount;
            if (player_id == GetOurPlayerID()){
                Patch_Bank_Deposit.Original("Player", amount);
            }   
            // then send host's money as well
            CoopManager.SendServerInGamePacket(new ServerMoneyUpdate(p.id, p.money));
        }
        public static void RecieveMoneyUpdate(ServerMoneyUpdate value){
            var player = GetPlayer(value.player_id);
            if (player != null){
                player.money = value.money;
                if (player.id == GetOurPlayerID()){
                    // calc money delta
                    float delta = value.money - Bank.ActualBalance("Player");
                    Patch_Bank_Deposit.Original("Player", delta);
                }
            }
            else RCMManager.Log($"[Co-op] Player with ID {value.player_id} not found for money deposit");

        }

    }
}
