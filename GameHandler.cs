﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication {
    class GameHandler {
        private Game[] _games = new Game[100];

        //Find an available spot and create a game 
        //(return the index of the game or -1 if it failed to create game)
        //this is used for creating a game from console not from a connected client
        public int CreateGame() {
            for (int i = 0; i < 100; i++) {
                if (_games[i] == null) {
                    _games[i] = new Game(i);
                    Console.WriteLine("Game created at index " + i);
                    return i;
                }
            }
            Console.WriteLine("Game was unable to be created no empty spots available");
            return -1;
        }

        //Find an available spot and create a game 
        //(return the index of the game or -1 if it failed to create game)
        public int CreateGame(int ClientIndex) {
            for(int i = 0; i<100; i++) {
                if(_games[i] == null) {
                    _games[i] = new Game(i, ClientIndex);
                    Console.WriteLine("Game created at index " + i + ", by user: " + Network.Clients[ClientIndex].player.GetUsername());
                    return i;
                }
            }
            Console.WriteLine("Game was unable to be created no empty spots available");
            return -1;
        }

        public bool JoinGame(int ClientIndex ,int GameIndex) {
            if (_games[GameIndex] != null) {
                if (_games[GameIndex].AddPlayer(ClientIndex)) {
                    Console.WriteLine("Player " + Network.Clients[ClientIndex].player.GetUsername() + " has joined game (index " + GameIndex + ")");
                    return true;
                }else {
                    Console.WriteLine("Player " + Network.Clients[ClientIndex].player.GetUsername() + " was unable to join game (index " + GameIndex + ")");
                    return false;
                }
            } else {
                return false;
            }
        }
    }

    class Game {
        public int GameIndex;
        public int[] connectedClients = new int[100];

        private GameState _state;
        private enum GameState {
            Searching,
            Empty,
            Full
        }

        public Game(int index) {
            this.GameIndex = index;
            for(int i=0; i<100; i++) {
                connectedClients[i] = -1;
            }
            _state = GameState.Empty;
        }

        public Game(int index, int ClientIndex) {
            this.GameIndex = index;
            for (int i = 0; i < 100; i++) {
                connectedClients[i] = -1;
            }
            _state = GameState.Empty;
            AddPlayer(ClientIndex);
        }

        public bool AddPlayer(int ClientIndex) {
            if (_state == GameState.Full) return false;
            int count = 0;
            bool found = false;
            while(!found && count < 100) {
                if (connectedClients[count] == -1) {
                    connectedClients[count] = ClientIndex;
                    found = true;
                }
                count++;
            }

            if (found) {
                if (count + 1 == 100) {
                    _state = GameState.Full;
                } else {
                    _state = GameState.Searching;
                }
                return true;
            } else
                return false;            
        }

        public bool AddTeams(int ClientOneIndex, int ClientTwoIndex) {
            return false;
        }
    }
}
