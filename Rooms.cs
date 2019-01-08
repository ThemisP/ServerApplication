using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication {
    class Rooms {
        private Room[] _rooms = new Room[100];

        //Tries to find a room with the specified index, if it doesn't exist then it creates it.
        //Might not be necessary to implement fully (yet)
        public void JoinOrCreateRoom(int clientIndex, int roomIndex, int maxPlayers) {
            if (_rooms[roomIndex] == null) {
                _rooms[roomIndex] = new Room(roomIndex, maxPlayers, clientIndex);
                Console.WriteLine("Player " + Network.Clients[clientIndex].player.GetUsername() + " successfully created room " + roomIndex);
            } else {
                JoinRoom(clientIndex, roomIndex);
            }
        }

        //Finds the next available spot and creates a room with the specified index
        public int CreateRoom(int clientIndex, int maxPlayers) {
            for (int i = 0; i < 100; i++) {
                if (_rooms[i] == null) {
                    _rooms[i] = new Room(i, maxPlayers, clientIndex);
                    Console.WriteLine("Room created at index " + i + " successfully by player " + Network.Clients[clientIndex].player.GetUsername());
                    return i;
                }
            }
            Console.WriteLine("Room unable to be created, no empty spots to create a new one");
            return -1;
        }

        //Given a room index, tries to join the specified room
        public bool JoinRoom(int clientIndex, int roomIndex) {
            if (_rooms[roomIndex].addPlayer(clientIndex)) {
                Console.WriteLine("Player " + Network.Clients[clientIndex].player.GetUsername() + " successfully joined room " + roomIndex);
                return true;
            } else {
                Console.WriteLine("Player " + Network.Clients[clientIndex].player.GetUsername() + " was not able to join room");
                return false;
            }
        }

        public int[] GetPlayersInRoom(int roomIndex) {
            return _rooms[roomIndex].GetPlayers();
        }
    }

    class Room {
        public int roomIndex;
        public int[] players;
        public int maxPlayers;

        public RoomState _state;
        public enum RoomState {
            Searching,
            Empty,
            Full
        }

        public Room(int roomIndex, int maxPlayers) {
            _state = RoomState.Empty;
            this.roomIndex = roomIndex;
            this.maxPlayers = maxPlayers;

            Array.Resize(ref players, maxPlayers);
            for (int i = 0; i < maxPlayers; i++) {
                players[i] = -1;
            }
        }

        public Room(int roomIndex, int maxPlayers, int playerIndex) {
            if (maxPlayers < 2) return;
            this.roomIndex = roomIndex;
            this.maxPlayers = maxPlayers;

            Array.Resize(ref players, maxPlayers);
            players[0] = playerIndex;
            for (int i = 1; i < maxPlayers; i++) {
                players[i] = -1;
            }
            _state = RoomState.Searching;
        }

        public bool addPlayer(int playerIndex) {
            if (_state == RoomState.Full) return false;
            int count = 0;
            bool running = true;
            while(running && count < maxPlayers) {
                if(players[count] == -1) {
                    players[count] = playerIndex;
                    running = false;
                }
                count++;
            }

            if(count == maxPlayers) {
                _state = RoomState.Full;
            }
            return true;
        }

        public int[] GetPlayers() {
            return players;
        }
    }
}
