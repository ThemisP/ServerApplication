using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace ServerApplication {
    class Network {
        
        public TcpListener ServerSocket;
        public static Network instance = new Network();
        public static Client[] Clients = new Client[Settings.MAX_PLAYERS];
        public UdpClient UdpClient;
        public RoomHandler roomHandler;
        public GameHandler gameHandler;
        private Timer checkConnectionTimer = new Timer();


        public void ServerStart() {
            for(int i=0; i<Settings.MAX_PLAYERS; i++) {
                Clients[i] = new Client();
            }
            roomHandler = new RoomHandler();
            gameHandler = new GameHandler();

            ServerSocket = new TcpListener(IPAddress.Any, 5500);
            ServerSocket.Start();
            ServerSocket.BeginAcceptTcpClient(OnClientConnect, null);

            ServerHandlePackets.instance.InitMessages();

            UdpClient = new UdpClient(5501);
            UdpClient.BeginReceive(new AsyncCallback(OnReceiveUdpData), null);
            Console.WriteLine("Server has successfully started.");
            //StartConnectionTimer();
        }
        //public void StartConnectionTimer() {
        //    checkConnectionTimer.Elapsed += new ElapsedEventHandler(SendCheckConnection);
        //    checkConnectionTimer.Interval = 2000;
        //    checkConnectionTimer.Enabled = true;
        //}

        //private void SendCheckConnection(object source, ElapsedEventArgs arg) {
        //    ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
        //    buffer.WriteInt(23);
        //    Console.WriteLine("in");
        //    foreach(Client client in Clients) {
        //        if(client.TcpClient != null) {
        //            client.TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
        //            client.TcpStream.Read()
        //        }
        //    }
        //}

        void OnClientConnect(IAsyncResult result) {
            TcpClient client = ServerSocket.EndAcceptTcpClient(result);
            client.NoDelay = false;
            ServerSocket.BeginAcceptTcpClient(OnClientConnect, null);

            for(int i=0; i<Settings.MAX_PLAYERS; i++) {
                if(Clients[i].TcpClient == null) {
                    Clients[i].TcpClient = client;
                    Clients[i].Index = i;
                    Clients[i].IP = (IPEndPoint)client.Client.RemoteEndPoint;
                    Clients[i].Start();
                    Console.WriteLine("Incoming connection from " + Clients[i].IP.ToString() + " || index: " + i);
                    //send welcome messages
                    ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                    buffer.WriteInt(1);
                    buffer.WriteString("Welcome to Server!");
                    buffer.WriteInt(i);
                    client.GetStream().BeginWrite(buffer.BuffToArray(), 0, buffer.Length(), null, null);
                    return;
                }
            }
        }

        void OnReceiveUdpData(IAsyncResult result) {
            try {
                IPEndPoint IpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] readBytes = UdpClient.EndReceive(result, ref IpEndPoint);
                if (UdpClient == null) {
                    return;
                }
                
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(readBytes);
                int index = buffer.ReadInt();
                if (Network.Clients[index] != null) {
                    byte[] croppedData = readBytes.Skip(4).ToArray();
                    Clients[index].UdpIP = IpEndPoint;
                    ServerHandlePackets.instance.HandleUdpData(index, croppedData);
                }
                UdpClient.BeginReceive(new AsyncCallback(OnReceiveUdpData), null);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return;
            }
        }

        public void CleanPlayers() {
            foreach(Client client in Clients) {
                int id = client.Index;
                client.player = new Player("username", id);
            }
        }

        static void BroadCastResponse(IAsyncResult ar)
        {

        }

    }
}
