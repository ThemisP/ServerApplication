using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication {
    class ServerHandlePackets {
        public static ServerHandlePackets instance = new ServerHandlePackets();
        private delegate void Packet_(int index, byte[] data);
        private Dictionary<int, Packet_> Packets;
        private static Dictionary<int, Player> players;

        public void InitMessages() {
            Packets = new Dictionary<int, Packet_>();
            Packets.Add(1, HandleLogin);
            Packets.Add(2, HandleCreateRoom);
            Packets.Add(3, HandlePosition);

            Packets.Add(5, HandleGetPlayersInRoom);
            Packets.Add(6, HandleJoinRoom);
            Packets.Add(7, HandleInstantiationOfPrefabs);
            Packets.Add(8, HandleJoinGameSolo);
            Packets.Add(9, HandleJoinGameDuo);
        }

        public void HandleData(int index, byte[] data) {
            int packetnum;
            Packet_ packet;
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            packetnum = buffer.ReadInt();
            buffer = null;
            if (packetnum == 0) return;
           
            if (Packets.TryGetValue(packetnum, out packet)) {
                packet.Invoke(index, data);
            } else {
                Console.WriteLine("Packet number from client " + index + " does not exist");
            }
        }

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
            Network.Clients[index].myStream.Write(buffer.BuffToArray(), 0, buffer.Length());
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
            Network.Clients[index].myStream.Write(buffer.BuffToArray() ,0,buffer.Length());

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
            Network.Clients[index].myStream.Write(buffer.BuffToArray(), 0, buffer.Length());
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
            Network.Clients[index].myStream.Write(buffer.BuffToArray(), 0, buffer.Length());
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
            bool joined;

            if (playerTwoIndex != -1)
                joined = Network.instance.gameHandler.JoinGame(index, playerTwoIndex, GameIndex);
            else joined = false;

            buffer.Clear();
            buffer.WriteInt(6);
            buffer.WriteInt((joined) ? 1 : 0);
            buffer.WriteInt(GameIndex);
            Console.WriteLine($"Player {index} with {playerTwoIndex} are trying to join Game {GameIndex}");
            Network.Clients[index].myStream.Write(buffer.BuffToArray(), 0, buffer.Length());
            Network.Clients[playerTwoIndex].myStream.Write(buffer.BuffToArray(), 0, buffer.Length());
        }

        public Dictionary<int, Player> getPlayers()
        {
            return players;
        }
    }
}
