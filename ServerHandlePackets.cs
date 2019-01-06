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
        private Dictionary<int, Player> players;

        public void InitMessegaes() {
            Packets = new Dictionary<int, Packet_>();
            Packets.Add(1, HandleLogin);
            Packets.Add(2, HandlePosition);
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
            Console.WriteLine("Login attempt from: " + username);
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
                players[index].x = x;
                players[index].y = y;
            }
        }
    }
}
