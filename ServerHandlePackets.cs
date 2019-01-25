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

            PacketsTcp.Add(-1, HandlePlayerDeath);

            PacketsUdp = new Dictionary<int, Packet_>();
            PacketsUdp.Add(1, HandleInitial);
            PacketsUdp.Add(2, HandlePlayerLocation);
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
        void HandleInitial(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            float number = buffer.ReadFloat();
            Console.WriteLine("Initial Udp connection established");
        }

        void HandlePlayerLocation(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            string datetime = buffer.ReadString();
            float posX = buffer.ReadFloat();
            float posY = buffer.ReadFloat();
            float posZ = buffer.ReadFloat();
            float rotY = buffer.ReadFloat();
            Console.WriteLine("Player " + index + " send location at (" + posX + ", " + posY + ", " + posZ + ")");
            Network.Clients[index].player.SetLocation(posX, posY, posZ);
            Network.Clients[index].player.SetRotation(rotY);

            //reply using this:  \/
            //Console.WriteLine("packet " + packetnum + " message: " + posX);
            //buffer.Clear();
            //buffer.WriteFloat(15.2f);
            //Console.WriteLine("ip from client saved: " + Network.Clients[index].UdpIP.ToString());
            //Network.instance.UdpClient.Send(buffer.BuffToArray(), buffer.Length(), Network.Clients[index].UdpIP);
        }
        #endregion

        #region "Handle TCP packets"
        void HandleLogin(int index, byte[] data) {            
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            string username = buffer.ReadString();
            buffer = null;
            Player player = Network.Clients[index].player;
            if(player == null) {
                Network.Clients[index].player = new Player(username);
            } else {
                player.ChangeUsername(username);
            }
            Console.WriteLine("Login attempt from: " + username);
        }

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
            foreach (int clientIndex in clientsInRoom ){
                if (clientIndex != -1) {
                    string username = Network.Clients[clientIndex].player.GetUsername();
                    Console.WriteLine(username);
                    buffer.WriteString(username);
                }
            }
            Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
        }

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

        //WIP ... a client instantiates an object and has to broadcast it to other clients so they know.
        void HandleInstantiationOfPrefabs(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            int roomIndex = buffer.ReadInt();
        }

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
                buffer.WriteInt(playerTwoIndex);
                if(playerTwoIndex!=-1)
                    buffer.WriteString(Network.Clients[playerTwoIndex].player.GetUsername());
            }
                       
            Console.WriteLine($"Player {index} with {playerTwoIndex} are trying to join Game {GameIndex}");
            
            Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
            if (playerTwoIndex!= -1) {
                ByteBuffer.ByteBuffer buffer2 = new ByteBuffer.ByteBuffer();
                buffer2.WriteInt(6);
                buffer2.WriteInt((teamIndex != -1) ? 1 : 0);
                buffer2.WriteInt(GameIndex);
                buffer2.WriteInt(teamIndex);
                buffer2.WriteInt(index);
                buffer2.WriteString(Network.Clients[index].player.GetUsername());
                
                Network.Clients[playerTwoIndex].TcpStream.Write(buffer2.BuffToArray(), 0, buffer2.Length());
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
                buffer.WriteInt(player.GetTeamNumber());
                buffer.WriteString(player.GetUsername());                
                buffer.WriteFloat(player.GetPosX());
                buffer.WriteFloat(player.GetPosY());
                buffer.WriteFloat(player.GetPosZ());
                buffer.WriteFloat(player.GetRotY());
            }
            Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
            
        }

        void HandlePlayerDeath(int index, byte[] data)
        {
            Network.Clients[index].player.SetIsAlive(false);
            players[index].SetIsAlive(false);
            Console.WriteLine($"Player {index} has died.");
        }
        #endregion
        public Dictionary<int, Player> getPlayers()
        {
            return players;
        }
    }
}
