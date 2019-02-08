using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ServerApplication {
    class ServerHandlePackets {
        public static ServerHandlePackets instance = new ServerHandlePackets();
        private delegate void Packet_(int index, byte[] data);
        private Dictionary<int, Packet_> PacketsTcp;
        private Dictionary<int, Packet_> PacketsUdp;
        private static Dictionary<int, Player> players;

        public void InitMessages() {
            PacketsTcp = new Dictionary<int, Packet_>();
            PacketsTcp.Add(1, HandleLogin);
            PacketsTcp.Add(2, HandleCreateRoom);
            PacketsTcp.Add(3, HandlePosition);

            PacketsTcp.Add(5, HandleGetPlayersInRoom);
            PacketsTcp.Add(6, HandleJoinRoom);
            PacketsTcp.Add(7, HandleInstantiationOfPrefabs);
            PacketsTcp.Add(8, HandleJoinGameSolo);
            PacketsTcp.Add(9, HandleJoinGameDuo);
            PacketsTcp.Add(10, HandleGetPlayersInGame);
            PacketsTcp.Add(11, HandleDestroyBullet);
            PacketsTcp.Add(12, HandlePlayerDamageTaken);

            PacketsTcp.Add(-1, HandlePlayerDeath);

            PacketsUdp = new Dictionary<int, Packet_>();
            PacketsUdp.Add(1, HandleInitial);
            PacketsUdp.Add(2, HandlePlayerLocation);
            PacketsUdp.Add(3, HandleBulletSpawn);
        }

        public void HandleData(int index, byte[] data) {
            int packetnum;
            Packet_ packet;
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            packetnum = buffer.ReadInt();
            buffer = null;
            if (packetnum == 0) return;

            if (PacketsTcp.TryGetValue(packetnum, out packet)) {
                packet.Invoke(index, data);
            } else {
                Console.WriteLine("Packet number | " + packetnum + " | from client " + index + " does not exist");
            }
        }

        public void HandleUdpData(int index, byte[] data) {
            int packetnum;
            Packet_ packet;
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            packetnum = buffer.ReadInt();
            buffer = null;
            if (packetnum == 0) return;

            if (PacketsUdp.TryGetValue(packetnum, out packet)) {
                packet.Invoke(index, data);
            } else {
                Console.WriteLine("Packet number | " + packetnum + " | from client " + index + " does not exist");
            }
        }

        #region "Handle Udp packets"
        //Packetnum = 1
        void HandleInitial(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            float number = buffer.ReadFloat();
            Console.WriteLine("Initial Udp connection established");
        }
        //Packetnum = 2
        void HandlePlayerLocation(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            string datetime = buffer.ReadString();
            float posX = buffer.ReadFloat();
            float posY = buffer.ReadFloat();
            float posZ = buffer.ReadFloat();

            float velX = buffer.ReadFloat();
            float velY = buffer.ReadFloat();
            float velZ = buffer.ReadFloat();

            float rotY = buffer.ReadFloat();

            Player player = Network.Clients[index].player;
            player.SetLocation(posX, posY, posZ);
            player.SetVelocity(velX, velY, velZ);
            player.SetRotation(rotY);
            //Console.WriteLine("Player " + index + " send location at (" + player.GetPosX() + ", " + player.GetPosY() + ", " + player.GetPosZ() + ")");

            //EveryTime a player sends its location, the server responds by sending that player the locations of other players
            buffer.Clear();
            int gameRoomIndex = player.GetGameRoomIndex();
            int[] playersInRoom = Network.instance.gameHandler.GetPlayersInGame(gameRoomIndex, index);
            buffer.WriteInt(2);
            buffer.WriteInt(playersInRoom.Length);
            foreach (int clientIndex in playersInRoom) {
                Player playerOther = Network.Clients[clientIndex].player;
                buffer.WriteInt(playerOther.GetId());
                buffer.WriteFloat(playerOther.GetPosX());
                buffer.WriteFloat(playerOther.GetPosY());
                buffer.WriteFloat(playerOther.GetPosZ());
                buffer.WriteFloat(playerOther.GetVelX());
                buffer.WriteFloat(playerOther.GetVelY());
                buffer.WriteFloat(playerOther.GetVelZ());
                buffer.WriteFloat(playerOther.GetRotY());
            }
            Network.instance.UdpClient.Send(buffer.BuffToArray(), buffer.Length(), Network.Clients[index].UdpIP);
        }
        //Packetnum = 3
        void HandleBulletSpawn(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            string datetime = buffer.ReadString();
            string bulletID = buffer.ReadString();
            float posX = buffer.ReadFloat();
            float posY = buffer.ReadFloat();
            float posZ = buffer.ReadFloat();
            float rotY = buffer.ReadFloat();
            float speed = buffer.ReadFloat();
            float lifetime = buffer.ReadFloat();

            int gameRoomIndex = Network.Clients[index].player.GetGameRoomIndex();


            Network.instance.gameHandler.AddBullet(gameRoomIndex, bulletID, posX, posY, posZ, rotY, speed, lifetime);

            //EveryTime a player sends its location, the server responds by sending that player the locations of other players
            buffer.Clear();
            int[] playersInRoom = Network.instance.gameHandler.GetPlayersInGame(gameRoomIndex, index);
            foreach (int clientIndex in playersInRoom) {
                buffer.WriteInt(3);// 3 is for client to handle bullet spawn;
                buffer.WriteString(bulletID);
                buffer.WriteFloat(posX);
                buffer.WriteFloat(posY);
                buffer.WriteFloat(posZ);
                buffer.WriteFloat(rotY);
                buffer.WriteFloat(speed);
                buffer.WriteFloat(lifetime);
                //Console.WriteLine("Sending player " + playerOther.GetId() + " with location ("
                //    + playerOther.GetPosX() + playerOther.GetPosY() + playerOther.GetPosZ());
                Network.instance.UdpClient.Send(buffer.BuffToArray(), buffer.Length(), Network.Clients[clientIndex].UdpIP);
            }
        }
        #endregion

        #region "Handle TCP packets"
        //Packetnum = 1
        void HandleLogin(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            string username = buffer.ReadString();
            buffer = null;
            Player player = Network.Clients[index].player;
            if(player == null) {
                Network.Clients[index].player = new Player(username, index);
            } else {
                player.ChangeUsername(username);
            }
            Console.WriteLine("Login attempt from: " + username);
        }
        
        //Packetnum = 2
        void HandleCreateRoom(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            int maxPlayers = buffer.ReadInt();

            int roomIndex = Network.instance.roomHandler.CreateRoom(index, maxPlayers);

            buffer.Clear();
            buffer.WriteInt(3);
            buffer.WriteInt((roomIndex!=-1)? 1:0);
            buffer.WriteInt(roomIndex);
            if (roomIndex != -1) Network.Clients[index].player.SetRoomNumber(roomIndex);
            Network.Clients[index].TcpStream.Write(buffer.BuffToArray() ,0,buffer.Length());
        }
        //Packetnum = 3
        void HandlePosition(int index, byte[] data)
        {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            buffer.ReadInt(); // Packet identified
            float x = buffer.ReadFloat();
            float y = buffer.ReadFloat();
            float z = buffer.ReadFloat();
            float rotZ = buffer.ReadFloat();
            buffer = null;
            if (players[index] == null)
            {
                players[index] = new Player(x, y, z);
                players[index].SetRotation(rotZ);
            }
            else
            {
                players[index].SetRotation(rotZ);
                players[index].SetLocation(x, y, z);
            }
        }
        //Packetnum = 5
        void HandleGetPlayersInRoom(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            ByteBuffer.ByteBuffer lengthBuffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            int roomIndex = buffer.ReadInt();
            int[] clientsInRoom = Network.instance.roomHandler.GetPlayersInRoom(roomIndex);

            buffer.Clear();
            buffer.WriteInt(4);
            int count = 0;
            foreach (int clientIndex in clientsInRoom) {
                if (clientIndex != -1) count++;
            }
            buffer.WriteInt(count);
            foreach (int clientIndex in clientsInRoom) {
                if (clientIndex != -1) {
                    string username = Network.Clients[clientIndex].player.GetUsername();
                    //Console.WriteLine(username);
                    buffer.WriteString(username);
                }
            }
            Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
        }
        //Packetnum = 6
        void HandleJoinRoom(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            int roomIndex = buffer.ReadInt();
            bool joined = Network.instance.roomHandler.JoinRoom(index, roomIndex);
            buffer.Clear();
            buffer.WriteInt(5);
            buffer.WriteInt((joined) ? 1 : 0);
            buffer.WriteInt(roomIndex);
            if (joined) Network.Clients[index].player.SetRoomNumber(roomIndex);
            Console.WriteLine($"Player {index} is trying to join room {roomIndex}");
            Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
        }
        //Packetnum = 7
        //WIP ... a client instantiates an object and has to broadcast it to other clients so they know.
        void HandleInstantiationOfPrefabs(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            int roomIndex = buffer.ReadInt();
        }
        //Packetnum = 8
        void HandleJoinGameSolo(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            int GameIndex = buffer.ReadInt();

            bool joined = Network.instance.gameHandler.JoinGame(index, GameIndex);

            buffer.Clear();
            buffer.WriteInt(6);
            buffer.WriteInt((joined) ? 1 : 0);
            buffer.WriteInt(GameIndex);
            Console.WriteLine($"Player {index} is trying to join Game {GameIndex}");
            Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
        }
        //Packetnum = 9
        void HandleJoinGameDuo(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            int GameIndex = buffer.ReadInt();

            int roomIndex = Network.Clients[index].player.GetRoomIndex();
            int[] playersInRoom = Network.instance.roomHandler.GetPlayersInRoom(roomIndex);
            int playerTwoIndex = playersInRoom[0];
            if (playerTwoIndex == index) playerTwoIndex = playersInRoom[1];
            int teamIndex;

            if (playerTwoIndex != -1)
                teamIndex = Network.instance.gameHandler.JoinGame(index, playerTwoIndex, GameIndex);
            else teamIndex = -1;

            buffer.Clear();
            buffer.WriteInt(6);
            buffer.WriteInt((teamIndex != -1) ? 1 : 0);
            if (teamIndex != -1) {
                buffer.WriteInt(GameIndex);
                buffer.WriteInt(teamIndex);
                buffer.WriteInt(1);//Player number in team
                buffer.WriteInt(playerTwoIndex);
                if (playerTwoIndex != -1) {
                    buffer.WriteString(Network.Clients[playerTwoIndex].player.GetUsername());
                }
            }

            Console.WriteLine($"Player {index} with {playerTwoIndex} are trying to join Game {GameIndex}");

            Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
            if (playerTwoIndex!= -1) {
                ByteBuffer.ByteBuffer buffer2 = new ByteBuffer.ByteBuffer();
                buffer2.WriteInt(6);
                buffer2.WriteInt((teamIndex != -1) ? 1 : 0);
                buffer2.WriteInt(GameIndex);
                buffer2.WriteInt(teamIndex);
                buffer2.WriteInt(2);//Player number in team
                buffer2.WriteInt(index);
                buffer2.WriteString(Network.Clients[index].player.GetUsername());
                Network.Clients[playerTwoIndex].TcpStream.Write(buffer2.BuffToArray(), 0, buffer2.Length());
            }

            int[] playersInGame = Network.instance.gameHandler.GetPlayersInGame(roomIndex, index);
            Player playerOne = Network.Clients[index].player;
            Player playerTwo = Network.Clients[playerTwoIndex].player;
            playerOne.JoinGame(GameIndex, playerTwoIndex, teamIndex);
            playerTwo.JoinGame(GameIndex, index, teamIndex);
            buffer.Clear();
            buffer.WriteInt(7);
            buffer.WriteInt(2);

            buffer.WriteInt(playerOne.GetId());
            buffer.WriteInt(playerOne.GetTeamNumber());
            buffer.WriteString(playerOne.GetUsername());
            buffer.WriteFloat(playerOne.GetPosX());
            buffer.WriteFloat(playerOne.GetPosY());
            buffer.WriteFloat(playerOne.GetPosZ());
            buffer.WriteFloat(playerOne.GetRotY());
            
            buffer.WriteInt(playerTwo.GetId());
            buffer.WriteInt(playerTwo.GetTeamNumber());
            buffer.WriteString(playerTwo.GetUsername());
            buffer.WriteFloat(playerTwo.GetPosX());
            buffer.WriteFloat(playerTwo.GetPosY());
            buffer.WriteFloat(playerTwo.GetPosZ());
            buffer.WriteFloat(playerTwo.GetRotY());
            foreach (int clientIndex in playersInGame) {
                Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                Console.WriteLine(clientIndex);
            }
        }

        //Packetnum = 10
        void HandleGetPlayersInGame(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            int gameRoomIndex = Network.Clients[index].player.GetGameRoomIndex();

            int[] playersInRoom = Network.instance.gameHandler.GetPlayersInGame(gameRoomIndex, index);
            buffer.Clear();
            buffer.WriteInt(7);
            buffer.WriteInt(playersInRoom.Length);
            foreach(int clientIndex in playersInRoom) {
                Player player = Network.Clients[clientIndex].player;
                buffer.WriteInt(player.GetId());
                buffer.WriteInt(player.GetTeamNumber());
                buffer.WriteString(player.GetUsername());
                buffer.WriteFloat(player.GetPosX());
                buffer.WriteFloat(player.GetPosY());
                buffer.WriteFloat(player.GetPosZ());
                buffer.WriteFloat(player.GetRotY());
            }
            Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
        }
        //Packetnum = 11
        void HandleDestroyBullet(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            string bullet_id = buffer.ReadString();
            int gameRoomIndex = Network.Clients[index].player.GetGameRoomIndex();
            Network.instance.gameHandler.RemoveBullet(gameRoomIndex, bullet_id);
            int[] playersInRoom = Network.instance.gameHandler.GetPlayersInGame(gameRoomIndex, index);

            foreach (int clientIndex in playersInRoom) {
                buffer.Clear();
                buffer.WriteInt(8);
                buffer.WriteString(bullet_id);

                Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
            }
        }

        // PacketNum = 12
        void HandlePlayerDamageTaken(int index, byte[] data){
            ByteBuffer.ByteBuffer buffer  = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            string bullet_id = buffer.ReadString();
            float damageTaken = buffer.ReadFloat();
            
            Player player = Network.Clients[index].player;
            player.TakeDamage(damageTaken);
            if (!player.IsAlive()) { Console.WriteLine("died"); }

            int gameRoomIndex = Network.Clients[index].player.GetGameRoomIndex();
            Network.instance.gameHandler.RemoveBullet(0, bullet_id); // TODO: Get correct
            int[] playersInRoom = Network.instance.gameHandler.GetPlayersInGame(gameRoomIndex, index);

            foreach (int clientIndex in playersInRoom) {
                buffer.Clear();
                buffer.WriteInt(8);
                buffer.WriteString(bullet_id);

                Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
            }
            // Need to correctly attribute damage to player fired bullet
            //string[] bulletIdentifiers = bullet_id.Split(',');
            //int indexOfShooter = int.Parse(bulletIdentifiers[0]);
            //Network.Clients[indexOfShooter].player.UpdateDamageDealt(damageTaken);
            //// Update players dealt damage count
            //buffer.Clear();
            //buffer.WriteInt(12);
            //buffer.WriteFloat(damageTaken);
            //Network.Clients[indexOfShooter].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
        }

        void HandlePlayerDeath(int index, byte[] data)
        {
           ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
           buffer.WriteBytes(data);
           int packetnum = buffer.ReadInt();
           string[] bulletIdentifiers = buffer.ReadString().Split('_');
           int indexOfShooter = int.Parse(bulletIdentifiers[0]);
           Network.Clients[indexOfShooter].player.AddKill();
        }
        #endregion
        public Dictionary<int, Player> getPlayers()
        {
            return players;
        }
    }
}
