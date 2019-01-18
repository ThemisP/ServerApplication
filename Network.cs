using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;

namespace ServerApplication {
    class Network {
        
        public TcpListener ServerSocket;
        public static Network instance = new Network();
        public static Client[] Clients = new Client[Settings.MAX_PLAYERS];

        public Rooms roomHandler;

        public void ServerStart() {
            for(int i=0; i<100; i++) {
                Clients[i] = new Client();
            }
            roomHandler = new Rooms();

            ServerSocket = new TcpListener(IPAddress.Any, 5500);
            ServerSocket.Start();
            ServerSocket.BeginAcceptTcpClient(OnClientConnect, null);

            ServerHandlePackets.instance.InitMessages();
            Console.WriteLine("Server has successfully started.");
        }

        void OnClientConnect(IAsyncResult result) {
            TcpClient client = ServerSocket.EndAcceptTcpClient(result);
            client.NoDelay = false;
            ServerSocket.BeginAcceptTcpClient(OnClientConnect, null);

            for(int i=0; i<100; i++) {
                if(Clients[i].Socket == null) {
                    Clients[i].Socket = client;
                    Clients[i].Index = i;
                    Clients[i].IP = client.Client.RemoteEndPoint.ToString();
                    Clients[i].Start();
                    Console.WriteLine("Incomming connection from " + Clients[i].IP + " || index: " + i);
                    //send welcome messages
                    ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                    buffer.WriteInt(1);
                    buffer.WriteString("Welcome to Server!");
                    client.GetStream().BeginWrite(buffer.BuffToArray(), 0, buffer.Length(), null, null);
                    return;
                }
            }
        }

        // Called periodically???
        public static void SendDataTo(int index, byte[] data) {
            Dictionary<int, Player> server = ServerHandlePackets.instance.getPlayers();
            Clients[index].myStream.BeginWrite(data, 0, data.Length, new AsyncCallback(BroadCastResponse), Clients[index].myStream);
        }

        public static void BroadCast()
        {
            Dictionary<int, Player> handler = ServerHandlePackets.instance.getPlayers();
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    if (j == i) continue;
                    float x;
                    float y;
                    ByteBuffer.ByteBuffer buff = new ByteBuffer.ByteBuffer();
                    x = handler[j].GetPosX();
                    y = handler[j].GetPosY();
                    buff.WriteFloat(x);
                    buff.WriteFloat(y);
                    SendDataTo(i, buff.BuffToArray());
                }
            }
        }

        static void BroadCastResponse(IAsyncResult ar)
        {

        }

    }
}
