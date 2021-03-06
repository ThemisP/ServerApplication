﻿using System;
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
        public int NumberOfConnectedClients = 0;
        public void InitMessages() {
            PacketsTcp = new Dictionary<int, Packet_>();
            PacketsTcp.Add(1, HandleLogin);
            PacketsTcp.Add(2, HandleCreateRoom);
            PacketsTcp.Add(3, HandleBulletSpawn);

            PacketsTcp.Add(5, HandleGetPlayersInRoom);
            PacketsTcp.Add(6, HandleJoinRoom);
            PacketsTcp.Add(7, HandleInstantiationOfPrefabs);
            PacketsTcp.Add(8, HandleJoinGameSolo);
            PacketsTcp.Add(9, HandleJoinGameDuo);
            PacketsTcp.Add(10, HandleGetPlayersInGame);
            PacketsTcp.Add(12, HandlePlayerDamageTaken);
            PacketsTcp.Add(13, HandleLeaveGame);
            PacketsTcp.Add(14, HandleLeaveRoom);
            PacketsTcp.Add(15, HandleIsGameReady);
            PacketsTcp.Add(16, HandlePlayerDeath);
            PacketsTcp.Add(17, HandlePlayerKnockDown);
            PacketsTcp.Add(18, HandlePlayerRevive);
            PacketsTcp.Add(19, HandleWaitForAllPlayersReceiveGameReady);
            PacketsTcp.Add(20, HandleDisableCollectibles);
            PacketsTcp.Add(21, HandlePlayerHealth);

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
            try {
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
                
                //EveryTime a player sends its location, the server responds by sending that player the locations of other players
                buffer.Clear();
                int gameRoomIndex = player.GetGameRoomIndex();
                int[] playersInRoom = Network.instance.gameHandler.GetAlivePlayersInGame(gameRoomIndex, index);
                buffer.WriteInt(2);
                buffer.WriteInt(playersInRoom.Length);
                foreach (int clientIndex in playersInRoom) {
                    Player playerOther = Network.Clients[clientIndex].player;
                    buffer.WriteInt(playerOther.GetId());
                    buffer.WriteString(playerOther.GetUsername());
                    buffer.WriteInt(playerOther.GetTeamNumber());
                    buffer.WriteFloat(playerOther.GetPosX());
                    buffer.WriteFloat(playerOther.GetPosY());
                    buffer.WriteFloat(playerOther.GetPosZ());
                    buffer.WriteFloat(playerOther.GetVelX());
                    buffer.WriteFloat(playerOther.GetVelY());
                    buffer.WriteFloat(playerOther.GetVelZ());
                    buffer.WriteFloat(playerOther.GetRotY());
                    buffer.WriteFloat(playerOther.GetHealth());
                }
                Network.instance.UdpClient.Send(buffer.BuffToArray(), buffer.Length(), Network.Clients[index].UdpIP);
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

        }
        
        #endregion

        #region "Handle TCP packets"
        //Packetnum = 1
        void HandleLogin(int index, byte[] data) {
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                int packetnum = buffer.ReadInt();
                string username = buffer.ReadString();
                bool allowed = true;
                foreach (Client client in Network.Clients) {
                    if (client.Index != index) {
                        if (client.player != null) {
                            if (client.player.GetUsername() == username) allowed = false;
                        }
                    }
                }
                Player player = Network.Clients[index].player;
                if (allowed) {
                    if (player == null) {
                        Network.Clients[index].player = new Player(username, index);                        
                        Console.WriteLine("New Player: " + username);
                    } else {
                        player.ChangeUsername(username);
                        Console.WriteLine("Change username: " + username);
                    }
                    buffer.Clear();
                    buffer.WriteInt(2);
                    buffer.WriteInt(1); // 0 false unsuccesfull
                    buffer.WriteString("Welcome " + username);
                    Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                } else {
                    buffer.Clear();
                    buffer.WriteInt(2);
                    buffer.WriteInt(0); // 0 false unsuccesfull
                    buffer.WriteString("Username already exists");
                    Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                }
            } catch(Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        //Packetnum = 2
        void HandleCreateRoom(int index, byte[] data) {
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                if (Network.instance.roomHandler.AllRoomsTaken()) {
                    buffer.WriteInt(3);
                    buffer.WriteInt(-1);
                    buffer.WriteInt(-1);
                    Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                    return;
                }
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
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        //Packetnum = 3
        void HandleBulletSpawn(int index, byte[] data) {
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                int packetnum = buffer.ReadInt();
                string datetime = buffer.ReadString();
                string bulletID = buffer.ReadString();
                int bulletTeam = buffer.ReadInt();
                float posX = buffer.ReadFloat();
                float posY = buffer.ReadFloat();
                float posZ = buffer.ReadFloat();
                float rotY = buffer.ReadFloat();
                float speed = buffer.ReadFloat();
                float lifetime = buffer.ReadFloat();
                float damage = buffer.ReadFloat();

                int gameRoomIndex = Network.Clients[index].player.GetGameRoomIndex();


                //Network.instance.gameHandler.AddBullet(gameRoomIndex, bulletID, posX, posY, posZ, rotY, speed, lifetime);

                //EveryTime a player sends its location, the server responds by sending that player the locations of other players
                buffer.Clear();
                int[] playersInRoom = Network.instance.gameHandler.GetAlivePlayersInGame(gameRoomIndex, index);
                buffer.WriteInt(14);// 3 is for client to handle bullet spawn;
                buffer.WriteString(bulletID);
                buffer.WriteInt(bulletTeam);
                buffer.WriteFloat(posX);
                buffer.WriteFloat(posY);
                buffer.WriteFloat(posZ);
                buffer.WriteFloat(rotY);
                buffer.WriteFloat(speed);
                buffer.WriteFloat(lifetime);
                buffer.WriteFloat(damage);
                foreach (int clientIndex in playersInRoom) {
                    Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        //Packetnum = 5
        void HandleGetPlayersInRoom(int index, byte[] data) {
            try  {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
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
                        buffer.WriteString(username);
                    }
                }
                Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
            } catch(Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
        //Packetnum = 6
        void HandleJoinRoom(int index, byte[] data) {
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                int packetnum = buffer.ReadInt();
                int roomIndex = buffer.ReadInt();
                bool joined = Network.instance.roomHandler.JoinRoom(index, roomIndex);
                buffer.Clear();
                buffer.WriteInt(5);
                buffer.WriteInt((joined) ? 1 : 0);
                buffer.WriteInt(roomIndex);
                if (joined) {
                    Network.Clients[index].player.SetRoomNumber(roomIndex);
                    NumberOfConnectedClients += 1;
                }
                Console.WriteLine($"Player {index} tried to join room {roomIndex} with success={joined}");
                Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
        //Packetnum = 7
        //WIP ... a client instantiates an object and has to broadcast it to other clients so they know.
        void HandleInstantiationOfPrefabs(int index, byte[] data) {
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                int packetnum = buffer.ReadInt();
                int roomIndex = buffer.ReadInt();
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
        //Packetnum = 8
        void HandleJoinGameSolo(int index, byte[] data) {
            try {
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
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
        //Packetnum = 9
        void HandleJoinGameDuo(int index, byte[] data) {
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                int packetnum = buffer.ReadInt();
                int GameIndex = buffer.ReadInt();

                int roomIndex = Network.Clients[index].player.GetRoomIndex();
                int[] playersInRoom = Network.instance.roomHandler.GetPlayersInRoom(roomIndex);
                
                int playerTwoIndex = playersInRoom[0];
                if (playerTwoIndex == index) playerTwoIndex = playersInRoom[1];
                if (playerTwoIndex == -1) {
                    buffer.Clear();
                    buffer.WriteInt(6);
                    buffer.WriteInt(0);
                    Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                    return;
                }
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

                    // Network.instance.roomHandler.LeaveRoom(roomIndex, index);
                    // Network.instance.roomHandler.LeaveRoom(roomIndex, playerTwoIndex);
                }

                int[] playersInGame = Network.instance.gameHandler.GetAlivePlayersInGame(GameIndex, index);
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
                buffer.WriteFloat(playerOne.GetHealth());

                buffer.WriteInt(playerTwo.GetId());
                buffer.WriteInt(playerTwo.GetTeamNumber());
                buffer.WriteString(playerTwo.GetUsername());
                buffer.WriteFloat(playerTwo.GetPosX());
                buffer.WriteFloat(playerTwo.GetPosY());
                buffer.WriteFloat(playerTwo.GetPosZ());
                buffer.WriteFloat(playerTwo.GetRotY());
                buffer.WriteFloat(playerTwo.GetHealth());
                foreach (int clientIndex in playersInGame) {
                    Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                    Console.WriteLine(clientIndex);
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

        }

        //Packetnum = 10
        void HandleGetPlayersInGame(int index, byte[] data) {
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                int packetnum = buffer.ReadInt();
                int gameRoomIndex = Network.Clients[index].player.GetGameRoomIndex();

                int[] playersInRoom = Network.instance.gameHandler.GetAlivePlayersInGame(gameRoomIndex, index);
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
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
        //Packetnum = 11
        void HandleDestroyBullet(int index, byte[] data) {
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                int packetnum = buffer.ReadInt();
                string bullet_id = buffer.ReadString();
                int gameRoomIndex = Network.Clients[index].player.GetGameRoomIndex();
                //Network.instance.gameHandler.RemoveBullet(gameRoomIndex, bullet_id);
                int[] playersInRoom = Network.instance.gameHandler.GetAlivePlayersInGame(gameRoomIndex, index);

                foreach (int clientIndex in playersInRoom) {
                    buffer.Clear();
                    buffer.WriteInt(8);
                    buffer.WriteString(bullet_id);

                    Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                } 
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        // PacketNum = 12
        void HandlePlayerDamageTaken(int index, byte[] data){
            try {
                ByteBuffer.ByteBuffer buffer  = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                int packetnum = buffer.ReadInt();
                string bullet_id = buffer.ReadString();
                float damageTaken = buffer.ReadFloat();

                Player player = Network.Clients[index].player;
                player.TakeDamage(damageTaken);
                int teamIndex = player.GetTeamNumber();
                int gameIndex = player.GetGameRoomIndex();

                if (!player.IsAlive()) {
                    buffer.Clear();
                    buffer.WriteInt(10);
                    Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());

                    bool teamAlive = Network.instance.gameHandler.HandlePlayerDeath(teamIndex, gameIndex, index);
                    bool gameOver = Network.instance.gameHandler.HandleGameEnd(gameIndex);
                    Console.WriteLine("TeamAlive: " + teamAlive + " and gameOver: " + gameOver);
                    if (!teamAlive) {
                        int teammateIndex = player.GetTeammemberClient();
                        buffer.Clear();
                        buffer.WriteInt(17);
                        buffer.WriteInt(0);
                        Console.WriteLine("teammate: " + teammateIndex);
                        Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                        Network.Clients[teammateIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                        Network.Clients[index].player = new Player("username", index);
                        Network.Clients[teammateIndex].player = new Player("username", teammateIndex);
                        int[] playersInRoom = Network.instance.gameHandler.GetAlivePlayersInGame(gameIndex, index);
                        buffer.Clear();
                        buffer.WriteInt(24);
                        buffer.WriteInt(index);
                        buffer.WriteInt(teammateIndex);
                        foreach(int otherIndex in playersInRoom) {
                            Network.Clients[otherIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                        }
                    } else {
                        int[] playersInRoom = Network.instance.gameHandler.GetPlayersInGame(gameIndex, index);
                        buffer.Clear();
                        buffer.WriteInt(9);
                        buffer.WriteInt(index);
                        buffer.WriteInt(player.GetTeamNumber());
                        buffer.WriteString(bullet_id);
                        buffer.WriteInt((player.IsAlive()) ? 1 : 0);
                        buffer.WriteFloat(player.GetHealth());
                        foreach (int clientIndex in playersInRoom) {
                            Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                        }
                    }
                    if (gameOver) {
                        int[] players = Network.instance.gameHandler.GetAlivePlayersInGame(gameIndex);
                        buffer.Clear();
                        buffer.WriteInt(17);
                        buffer.WriteInt(1);
                        foreach (int clientIndex in players) {
                            Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                        }
                        AfterGameCleanUp(gameIndex);
                    }
                } else {
                    int[] playersInRoom = Network.instance.gameHandler.GetPlayersInGame(gameIndex, index);
                    buffer.Clear();
                    buffer.WriteInt(9);
                    buffer.WriteInt(index);
                    buffer.WriteInt(player.GetTeamNumber());
                    buffer.WriteString(bullet_id);
                    buffer.WriteInt((player.IsAlive()) ? 1 : 0);
                    buffer.WriteFloat(player.GetHealth());
                    foreach (int clientIndex in playersInRoom) {
                        Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                    }
                }
                
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        //PacketNum = 13
        void HandleLeaveGame(int index, byte[] data) {
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                int packetnum = buffer.ReadInt();

                int gameRoomIndex = Network.Clients[index].player.GetGameRoomIndex();
                Network.instance.gameHandler.LeaveGame(gameRoomIndex, index);

                buffer.Clear();
                buffer.WriteInt(13);
                Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());

                int[] playersInRoom = Network.instance.gameHandler.GetPlayersInGame(gameRoomIndex, index);
                buffer.Clear();
                buffer.WriteInt(11);
                buffer.WriteInt(index);
                buffer.WriteInt(Network.Clients[index].player.GetTeamNumber());

                foreach (int clientIndex in playersInRoom) {
                    Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

        }

        //PacketNum = 14
        void HandleLeaveRoom(int index, byte[] data) {
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                int packetnum = buffer.ReadInt();
                int roomIndex = Network.Clients[index].player.GetRoomIndex();

                Network.instance.roomHandler.LeaveRoom(roomIndex, index);
                NumberOfConnectedClients -= 1;
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        // PacketNum = 15
        void HandleIsGameReady(int index, byte[] data) {
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                int packetNum = buffer.ReadInt();
                int gameIndex = buffer.ReadInt();
                buffer.Clear();
                int numberOfFullRooms = Network.instance.roomHandler.GetNumberOfFullRooms();
                float timer = Network.instance.gameHandler.GetStartTimer(gameIndex);
                // if (timer <= 0f && numberOfFullRooms < Settings.MIN_ROOMS) { // Reset timer, not enough players in game
                //     Network.instance.gameHandler.RestartStartTimer(gameIndex);
                //     timer = Network.instance.gameHandler.GetStartTimer(gameIndex);
                // }
                //int gameReady = (numberOfFullRooms == Settings.MAX_ROOMS || ( (numberOfFullRooms >= Settings.MIN_ROOMS) || (timer <= 0f))) ? 1 : 0;
                int gameReady = Network.instance.gameHandler.GetManualStart(gameIndex);
                Console.WriteLine("gameReady? :" + gameReady);
                buffer.Clear();
                buffer.WriteInt(15);
                buffer.WriteInt(gameReady);
                buffer.WriteFloat(timer);
                Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());

                if (gameReady == 1) {
                    buffer.Clear();
                    buffer.WriteInt(19);
                    foreach (int clientIndex in waiting) {
                        Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                    }
                    Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                    waiting = new List<int>();
                    return;
                }
                if (timer <= 0f) { // Reset timer, not enough players in game
                    Network.instance.gameHandler.RestartStartTimer(gameIndex);
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        
        // PacketNum = 16
        void HandlePlayerDeath(int index, byte[] data) {
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                buffer.ReadInt();
                int teamIndex = buffer.ReadInt();
                int gameIndex =  buffer.ReadInt();
                int roomIndex = buffer.ReadInt();
                bool teamAlive = Network.instance.gameHandler.HandlePlayerDeath(teamIndex, gameIndex, index);
                bool gameOver = Network.instance.gameHandler.HandleGameEnd(gameIndex);
                Console.WriteLine("TeamAlive: " + teamAlive + " and gameOver: " + gameOver);
                if (!teamAlive) {
                    int teammateIndex = Network.instance.gameHandler.GetTeammateId(teamIndex, gameIndex, index);
                    buffer.Clear();
                    buffer.WriteInt(17);
                    buffer.WriteInt(0);
                    Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                    Network.Clients[teammateIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                }
                if (gameOver) {
                    int[] players = Network.instance.gameHandler.GetAllPlayers(gameIndex);
                    buffer.Clear();
                    buffer.WriteInt(17);
                    foreach(int clientIndex in players) {
                        Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                    }
                    AfterGameCleanUp(gameIndex);
                } 
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
            return;
 
        }

        //PacketNum = 17
        void HandlePlayerKnockDown(int index, byte[] data) {
            //WIP
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                buffer.ReadInt();
                int teamIndex = buffer.ReadInt();
                int gameIndex = buffer.ReadInt();
                int roomIndex = buffer.ReadInt();
                bool teamAlive = Network.instance.gameHandler.HandlePlayerDeath(teamIndex, gameIndex, index);
                bool gameOver = Network.instance.gameHandler.HandleGameEnd(gameIndex);
                Console.WriteLine("TeamAlive: " + teamAlive + " and gameOver: " + gameOver);
                if (!teamAlive) {
                    int teammateIndex = Network.instance.gameHandler.GetTeammateId(teamIndex, gameIndex, index);
                    buffer.Clear();
                    buffer.WriteInt(17);
                    buffer.WriteInt(0);
                    Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                    Network.Clients[teammateIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                }
                if (gameOver) {
                    Network.instance.gameHandler.RestartStartTimer(gameIndex);
                    buffer.Clear();
                    buffer.WriteInt(17);
                    buffer.WriteInt(1);
                    foreach (int clientIndex in Network.instance.gameHandler.GetAllPlayers(gameIndex)) {
                        Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
            return;
        }

        //PacketNum = 18
        void HandlePlayerRevive(int index, byte[] data) {
            //WIP
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                buffer.ReadInt();
                int teammateIndex = buffer.ReadInt();
                Network.Clients[teammateIndex].player.Revive();
                int gameRoomIndex = Network.Clients[index].player.GetGameRoomIndex();
                int[] playersInRoom = Network.instance.gameHandler.GetAllPlayers(gameRoomIndex);
                buffer.Clear();
                buffer.WriteInt(18);
                buffer.WriteInt(teammateIndex);
                foreach (int clientIndex in playersInRoom) {
                    Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
            return;
        }

        //PacketNum = 19
        List<int> waiting = new List<int>();
        void HandleWaitForAllPlayersReceiveGameReady(int index, byte[] data) {
            try {
                ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
                buffer.WriteBytes(data);
                buffer.ReadInt();
                int gameIndex = buffer.ReadInt();
                bool ready = Network.instance.gameHandler.PlayerReady(gameIndex, NumberOfConnectedClients);
                Console.WriteLine("ready? : " + ready);
                if (!waiting.Contains(index)) {
                    waiting.Add(index);
                }
                //if (ready) {
                //    buffer.Clear();
                //    buffer.WriteInt(19);
                //    foreach (int clientIndex in waiting) {
                //        Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                //    }
                //    Network.Clients[index].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
                //    waiting = new List<int>();
                //}
                //else {
                //    waiting.Add(index);
                //}
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        //PacketNum = 20
        void HandleDisableCollectibles(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            buffer.ReadInt();
            int coinOrPill = buffer.ReadInt();
            string id = buffer.ReadString();

            int gameRoomIndex = Network.Clients[index].player.GetGameRoomIndex();
            int[] playersInRoom = Network.instance.gameHandler.GetAllPlayers(gameRoomIndex);
            buffer.Clear();
            buffer.WriteInt(21);
            buffer.WriteInt(coinOrPill);
            buffer.WriteString(id);
            foreach (int clientIndex in playersInRoom) {
                if(clientIndex!=index)
                    Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
            }
        }

        //PacketNum = 21
        void HandlePlayerHealth(int index, byte[] data) {
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteBytes(data);
            buffer.ReadInt();
            float health = buffer.ReadFloat();
            Network.Clients[index].player.SetHealth(health);
            //int team = Network.Clients[index].player.GetTeamNumber();
            //int gameRoomIndex = Network.Clients[index].player.GetGameRoomIndex();
            //int[] playersInRoom = Network.instance.gameHandler.GetAllPlayers(gameRoomIndex);
            //buffer.Clear();
            //buffer.WriteInt(22);
            //buffer.WriteInt(index);
            //buffer.WriteInt(team);
            //buffer.WriteFloat(health);
            //foreach (int clientIndex in playersInRoom) {
            //    if(clientIndex!=index)
            //        Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
            //}
        }
        #endregion

        #region "Server send data"
        public static void SendCircleShrinkData(int gameIndex, float circleTimer) {
            int[] playersInRoom = Network.instance.gameHandler.GetAllPlayers(gameIndex);
            ByteBuffer.ByteBuffer buffer = new ByteBuffer.ByteBuffer();
            buffer.WriteInt(20);
            buffer.WriteFloat(circleTimer);
            foreach (int clientIndex in playersInRoom) {
                Network.Clients[clientIndex].TcpStream.Write(buffer.BuffToArray(), 0, buffer.Length());
            }
        }
        #endregion

        void AfterGameCleanUp(int gameIndex) {
            // CleanUp room
            try {            
                Network.instance.roomHandler.EmptyRooms();
                Network.instance.gameHandler.RestartGame(gameIndex);
                Network.instance.gameHandler.RestartStartTimer(gameIndex);
                Network.instance.CleanPlayers();
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
