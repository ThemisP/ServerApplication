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

        public void InitMessegaes() {
            Packets = new Dictionary<int, Packet_>();
            Packets.Add(1, HandleLogin);
        }

        public void HandleData(int index, byte[] data) {
            int packetnum;
            Packet_ packet;
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            packetnum = buffer.ReadInt();
            buffer = null;
            if (packetnum == 0) return;

            if(Packets.TryGetValue(packetnum, out packet)) {
                packet.Invoke(index, data);
            }
        }

        void HandleLogin(int index, byte[] data) {

        }
    }
}
