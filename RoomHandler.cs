using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication {
    class RoomHandler {
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
                if (_rooms[i] == null || _rooms[i].GetRoomState() == Room.RoomState.Empty) {
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
            if(_rooms[roomIndex] != null) {
                if (_rooms[roomIndex].addPlayer(clientIndex)) {
                    Console.WriteLine("Player " + Network.Clients[clientIndex].player.GetUsername() + " successfully joined room " + roomIndex);
                    return true;
                } else {
                    Console.WriteLine("Player " + Network.Clients[clientIndex].player.GetUsername() + " was not able to join room");
                    return false;
                }
            } else {
                return false;
            }
        }

        public int[] GetPlayersInRoom(int roomIndex) {
            return _rooms[roomIndex].GetPlayers();
        }
        
        public Room[] GetRooms() {
            return this._rooms;
        }

        public void LeaveRoom(int roomIndex, int clientIndex) {
            if(_rooms[roomIndex] != null) {
                if (_rooms[roomIndex].LeaveRoom(clientIndex)) {
                    _rooms[roomIndex] = null;
                }
            } else {
                Console.WriteLine("Room index :" + roomIndex + "does not exist");
            }
        }

        public bool AllRoomsTaken() {
            int count = 0;
            foreach (Room r in _rooms) {
                if (r != null) {
                    if (r.GetRoomState() == Room.RoomState.Full) {
                        count ++;
                    }
                }
            }

            return count == Settings.MAX_ROOMS;
        }

        public int GetNumberOfFullRooms() {
            int count = 0;
            foreach (Room r in _rooms) {
                if (r != null) {
                    if (r.GetRoomState() == Room.RoomState.Full) {
                        count ++;
                    }
                }
            }

            return count; 
        }

        public void EmptyRooms() {
            for(int i = 0; i < Settings.MAX_ROOMS; i++) {
                if (_rooms[i] != null) {
                    int roomIndex = _rooms[i].roomIndex;
                    _rooms[i].Empty();
                    _rooms[i] = new Room(roomIndex, 2);
                }
            }
        }
    }

    class Room {
        public int roomIndex;
        public int[] players;
        public int maxPlayers;

        private RoomState _state;
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
            bool found = false;
            while(!found && count < maxPlayers) {
                if(players[count] == -1) {
                    players[count] = playerIndex;
                    found = true;
                }
                count++;
            }
            
            if (found) {
                if (count + 1 >= maxPlayers) {
                    _state = RoomState.Full;
                } else {
                    _state = RoomState.Searching;
                }
                return true;
            } else
                return false;
        }

        public int[] GetPlayers() {
            return players;
        }

        //TODO: WIP
        public bool LeaveRoom(int playerIndex) {
            if (_state == RoomState.Empty) return true;
            bool destroy = false;
            for(int i=0; i<maxPlayers; i++) {
                if(players[i] == playerIndex) {
                    players[i] = -1;
                    Network.Clients[playerIndex].player.LeaveRoom();
                    if (_state == RoomState.Full) _state = RoomState.Searching;
                    else {
                        _state = RoomState.Empty;
                        destroy = true;
                    }
                }
            }
            return destroy;
        }

        public RoomState GetRoomState() {
            return this._state;
        }

        public void Empty() {
            for (int i = 0; i < maxPlayers; i++){
                int index = players[i];
                if(index >= 0) 
                    Network.Clients[index].player.LeaveRoom();
            }
        }
    }
}
