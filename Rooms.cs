using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication {
    class Rooms {
        public static Room[] _rooms = new Room[100];
        public static Rooms roomsInstance = new Rooms();

        public void JoinOrCreateRoom(int player, int roomIndex, int maxPlayers) {
            bool found = false;
            for(int i = 0; i<100; i++) {
                if(_rooms[i].roomIndex == roomIndex) {
                    if(_rooms[i]._state == Room.RoomState.Full) {
                        Console.WriteLine("Room " + roomIndex + " is full");
                    } else {
                        _rooms[i].addPlayer(player);
                    }
                    found = true;
                }
            }
            if (!found) {
                for(int i = 0; i<100; i++) {
                    if(_rooms[i] == null) {
                        _rooms[i] = new Room(roomIndex, maxPlayers, player);
                    }
                }
            }
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

        public void addPlayer(int playerIndex) {
            if (_state == RoomState.Full) return;
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
        }
    }
}
