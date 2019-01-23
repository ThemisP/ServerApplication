using System;
using System.Net.Sockets;
using System.Net;

namespace ServerApplication {
    class Client {
        public int Index;
        public IPEndPoint IP;
        public IPEndPoint UdpIP;
        public TcpClient TcpClient;
        public UdpClient UdpClient;
        public NetworkStream TcpStream;
        public Player player;
        private byte[] readbuff;
        

        public void Start() {
            TcpClient.SendBufferSize = Settings.SEND_BUFFER_SIZE;
            TcpClient.ReceiveBufferSize = Settings.RECEIVE_BUFFER_SIZE;            
            TcpStream = TcpClient.GetStream();
            Array.Resize(ref readbuff, TcpClient.ReceiveBufferSize);
            TcpStream.BeginRead(readbuff, 0, TcpClient.ReceiveBufferSize, OnReceiveTcpData, null);
        }

        public void StartUdp(IPEndPoint ipend) {
            UdpIP = ipend;
            UdpClient = new UdpClient();
            UdpClient.Connect(ipend);
        }

        void CloseTcpConnection() {
            TcpClient.Close();
            TcpClient = null;
            Console.WriteLine("Player Disconnected (tcp):" + IP.ToString());
        }
        
        void OnReceiveTcpData(IAsyncResult result) {
            try {
                int readBytes = TcpStream.EndRead(result);
                if (TcpClient == null) {
                    return;
                }

                if (readBytes <= 0) {
                    CloseTcpConnection();
                    return;
                }

                byte[] newBytes = null;
                Array.Resize(ref newBytes, readBytes);
                Buffer.BlockCopy(readbuff, 0, newBytes, 0, readBytes);

                //Handle data
                ServerHandlePackets.instance.HandleData(this.Index, newBytes);

                if(TcpClient == null) {
                    return;
                }
                TcpStream.BeginRead(readbuff, 0, TcpClient.ReceiveBufferSize, OnReceiveTcpData, null);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                CloseTcpConnection();
                return;
            }
        }
    }
}
