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

        public void InitMessegaes() {
            Packets = new Dictionary<int, Packet_>();
            Packets.Add(1, HandleLogin);
            Packets.Add(2, HandleCreateRoom);
            Packets.Add(5, HandleGetPlayersInRoom);
            Packets.Add(3, HandlePosition);
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
            buffer.WriteInt(clientsInRoom.Length);
            foreach (int clientIndex in clientsInRoom ){
                string username = Network.Clients[clientIndex].player.GetUsername();
                buffer.WriteString(username);
            }
            
            lengthBuffer.WriteInt(buffer.Length());
            

            Network.Clients[index].myStream.Write(lengthBuffer.BuffToArray(), 0, lengthBuffer.Length());

            Network.Clients[index].myStream.Write(buffer.BuffToArray() ,0,buffer.Length());
        }

        void HandleCreateRoom(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            int packetnum = buffer.ReadInt();
            int maxPlayers = buffer.ReadInt();
            int roomIndex = Network.instance.roomHandler.CreateRoom(index, maxPlayers);
            buffer.Clear();
            buffer.WriteInt((roomIndex!=-1)? 1:0);
            buffer.WriteInt(roomIndex);
            Network.Clients[index].myStream.Write(buffer.BuffToArray() ,0,buffer.Length());

        }

        void HandlePosition(int index, byte[] data)
        {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            buffer.ReadInt(); // Packet identified
            float x = buffer.ReadFloat();
            float y = buffer.ReadFloat();
            buffer = null;
            if (players[index] == null)
            {
                players[index] = new Player(x, y);
            }
            else
            {
                players[index].SetLocation(x, y);
            }
        }

        public Dictionary<int, Player> getPlayers()
        {
            return players;
        }
    }
}
