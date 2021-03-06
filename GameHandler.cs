﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

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
                    _games[i].StartTimer();
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
                    _games[i].StartTimer();
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

        //Joining a game as a duo and getting the Team index as a return or -1 if error
        public int JoinGame(int ClientOneIndex, int ClientTwoIndex, int GameIndex) {
            if (_games[GameIndex] != null) {
                int teamIndex = _games[GameIndex].AddDuo(ClientOneIndex, ClientTwoIndex);
                if (teamIndex != -1) {
                    Console.WriteLine("Player " + Network.Clients[ClientOneIndex].player.GetUsername() + " together with "
                                        + Network.Clients[ClientTwoIndex].player.GetUsername() + " have joined game (index " + GameIndex + ")");
                    return teamIndex;
                } else {
                    Console.WriteLine("Player " + Network.Clients[ClientOneIndex].player.GetUsername() + " together with "
                                        + Network.Clients[ClientTwoIndex].player.GetUsername()+ " were unable to join game (index " + GameIndex + ")");
                    return -1;
                }
            } else {
                return -1;
            }
        }

        public int[] GetAlivePlayersInGame(int gameIndex, int clientIndex) {
            if(_games[gameIndex] != null) {
                return _games[gameIndex].GetAlivePlayersBut(clientIndex);
            }
            return null;
        }

        public int[] GetPlayersInGame(int gameIndex, int clientIndex) {
            if (_games[gameIndex] != null) {
                return _games[gameIndex].GetConnectedPlayersBut(clientIndex);
            }
            return null;
        }

        public int[] GetAlivePlayersInGame(int gameIndex) {
            if (_games[gameIndex] != null) {
                return _games[gameIndex].GetConnectedPlayers();
            }

            return null;
        }

        public void LeaveGame(int gameIndex, int clientIndex) {
            if (_games[gameIndex] != null) {
                _games[gameIndex].LeaveGame(clientIndex);
            }
        }

        public float GetStartTimer(int gameIndex) {
            if (_games[gameIndex] != null) {
                return Settings.MAX_START_TIMER - _games[gameIndex].timeElapsed;
            }

            return 1f;
        }

        public void RestartStartTimer(int gameIndex) {
            if (_games[gameIndex] != null) {
                _games[gameIndex].RestartTimer();
            }

            return;
        }

        public bool HandlePlayerDeath(int teamIndex, int gameIndex, int clientIndex) {
            bool ret = false;
            if (_games[gameIndex] != null) {
                ret = (_games[gameIndex].RegisterPlayerDeath(teamIndex, clientIndex));
            }

            return ret;
        } 
        
        public bool HandleGameEnd(int gameIndex) {
            bool ret = false;
            if(_games[gameIndex] != null) {
                ret = (_games[gameIndex].isGameEnd());
            }
            return ret;
        }

        public void NewGame(int gameIndex) {
            if (_games[gameIndex] != null) {
                _games[gameIndex].NewGame();
            }
        }

        public int[] GetAllPlayers(int gameIndex) {
            if (_games[gameIndex] != null) {
                return _games[gameIndex].GetConnectedPlayers();
            }

            return null;
        }

        public int GetTeammateId(int gameIndex, int index, int teamIndex) {
            if(_games[gameIndex] != null) {
                return _games[gameIndex].GetTeammateClientIndex(index, teamIndex);
            }
            return -1;
        }

        public void RestartGame(int gameIndex) {
            if (_games[gameIndex] != null) {
                _games[gameIndex].StopCircleTimer();                
            }
            _games[gameIndex] = new Game(gameIndex);
        }

        public bool PlayerReady(int gameIndex, int connectedClients) {
            if (_games[gameIndex] != null) {
                return _games[gameIndex].PlayerReady(connectedClients);
            }

            return false;
        }

        public void SetTimerToStart(int gameIndex) {
            if (_games[gameIndex] != null) {
                _games[gameIndex].GameReady();
            }

            return;
        }

        public int GetManualStart(int gameIndex) {
            if (_games[gameIndex] != null) {
                return _games[gameIndex].isGameManuallyStarted();
            }

            return 0;
        }

        public void ManualStart(int gameIndex) {
            if (_games[gameIndex] != null) {
                _games[gameIndex].ManualStart();
            }
        }
    }

    class Game {
        private int GameIndex;
        private int[] connectedClients = new int[Settings.MAX_PLAYERS];
        private int NumberOfConnectedClients = 0;
        private Team[] Teams = new Team[Settings.MAX_PLAYERS / 2];
        private int activeTeams = 0;
        Timer startTimer = new Timer();
        Timer circleTimer = new Timer();
        public float circleTimeElapsed = 0f;
        public float timeElapsed = 0f;
        private int playersReady = 0;
        private GameState _state;
        private bool manualStart = false;

        private List<int> playersStillAlive = new List<int>();
        private enum GameState {
            Searching,
            Empty,
            Full
        }

        public Game(int gameIndex) {
            this.GameIndex = gameIndex;
            for (int i = 0; i < Settings.MAX_PLAYERS; i++) {
                connectedClients[i] = -1;
                if (i < Settings.MAX_PLAYERS / 2) {
                    Teams[i] = new Team(i, GameIndex);
                }
            }
            _state = GameState.Empty;
        }

        public Game(int gameIndex, int ClientIndex) {
            this.GameIndex = gameIndex;
            for (int i = 0; i < Settings.MAX_PLAYERS; i++) {
                connectedClients[i] = -1;
                if (i < Settings.MAX_PLAYERS / 2) {
                    Teams[i] = new Team(i, GameIndex);
                }
            }
            _state = GameState.Empty;
            AddPlayer(ClientIndex);
        }

        public bool AddPlayer(int ClientIndex) {
            if (_state == GameState.Full) return false;
            int count = 0;
            bool found = false;
            while (!found && count < Settings.MAX_PLAYERS) {
                if (connectedClients[count] == -1) {
                    connectedClients[count] = ClientIndex;
                    found = true;
                }
                count++;
            }

            if (found) {
                NumberOfConnectedClients += 1;
                if (NumberOfConnectedClients == Settings.MAX_PLAYERS) {
                    _state = GameState.Full;
                } else {
                    _state = GameState.Searching;
                }
                return true;
            } else
                return false;
        }

        public int AddDuo(int ClientOneIndex, int ClientTwoIndex) {
            if (_state == GameState.Full) return -1;
            int count = 0;
            int found = 0;
            int indexOne = 0;
            int indexTwo = 0;
            NumberOfConnectedClients += 2;
            StartCircleTimer();
            while (found < 2 && count < Settings.MAX_PLAYERS) {
                if (connectedClients[count] == -1) {
                    if (found == 0) indexOne = count;
                    else indexTwo = count;
                    found++;
                }
                count++;
            }

            if (found > 1) {
                connectedClients[indexOne] = ClientOneIndex;
                connectedClients[indexTwo] = ClientTwoIndex;
                int teamIndex = AddTeam(ClientOneIndex, ClientTwoIndex);
                playersStillAlive.Add(ClientOneIndex);
                playersStillAlive.Add(ClientTwoIndex);
                // NumberOfConnectedClients += 2;
                if (NumberOfConnectedClients == Settings.MAX_PLAYERS) {
                    _state = GameState.Full;
                } else {
                    _state = GameState.Searching;
                }
                return teamIndex;
            } else
                return -1;
        }

        int AddTeam(int ClientOneIndex, int ClientTwoIndex) {
            foreach (Team team in Teams) {
                if (team.isEmpty()) {
                    team.createTeam(ClientOneIndex, ClientTwoIndex);
                    activeTeams += 1;
                    return team.getIndex();
                }
            }
            return -1;
        }

        public int[] GetConnectedPlayers() {
            List<int> players = new List<int>();
            for (int i = 0; i < Settings.MAX_PLAYERS; i++) {
                if (connectedClients[i] != -1) {
                    players.Add(connectedClients[i]);
                }
            }
            return players.ToArray();
        }

        public int[] GetAlivePlayersBut(int clientIndex) {
            List<int> players = new List<int>();

            //for (int i = 0; i < Settings.MAX_PLAYERS; i++) {
            //    if (connectedClients[i] != -1 && connectedClients[i] != index) {
            //        if(Network.Clients[connectedClients[i]].player.IsAlive())
            //            players.Add(connectedClients[i]);
            //    }
            //}
            foreach (int index in playersStillAlive) {
                if (index != clientIndex)
                    players.Add(index);
            }
            return players.ToArray();
        }

        public int[] GetConnectedPlayersBut(int index) {
            List<int> players = new List<int>();

            for (int i = 0; i < Settings.MAX_PLAYERS; i++) {
                if (connectedClients[i] != -1 && connectedClients[i] != index) {
                    players.Add(connectedClients[i]);
                }
            }
            return players.ToArray();
        }

        public int GetTeammateClientIndex(int index, int teamIndex) {
            return Teams[teamIndex].GetTeammate(index);
        }

        public void LeaveGame(int clientIndex) {
            for (int i = 0; i < Settings.MAX_PLAYERS; i++) {
                if (connectedClients[i] == clientIndex) {
                    connectedClients[i] = -1;
                    NumberOfConnectedClients -= 1;
                }
            }

            if (NumberOfConnectedClients <= 0) {
                RestartTimer();
            }
        }

        public void StartCircleTimer() {
            this.circleTimeElapsed = 0f;
            this.circleTimer.Elapsed += new ElapsedEventHandler(IncrementCircleTimer);
            circleTimer.Interval = 2000;
            circleTimer.Enabled = true;
        }

        private void IncrementCircleTimer(object source, ElapsedEventArgs e) {
            this.circleTimeElapsed += 1.5f;
            if (circleTimeElapsed >= Settings.MAX_GAME_TIMER) {
                this.circleTimer.Stop();
                circleTimeElapsed = Settings.MAX_GAME_TIMER;
            }
            ServerHandlePackets.SendCircleShrinkData(this.GameIndex, circleTimeElapsed);
        }

        public void StopCircleTimer() {
            this.circleTimeElapsed = 0f;
            this.circleTimer.Stop();
        }


        public void StartTimer() {
            this.timeElapsed = 0f;
            this.startTimer.Elapsed += new ElapsedEventHandler(IncrementTimer);
            startTimer.Interval = 5000;
            startTimer.Enabled = true;
        }

        private void IncrementTimer(object source, ElapsedEventArgs e) {
            this.timeElapsed += 5f;
            if (timeElapsed >= Settings.MAX_START_TIMER) {
                this.startTimer.Stop();
                timeElapsed = Settings.MAX_START_TIMER;
            }
        }

        public void RestartTimer() {
            this.timeElapsed = 0f;
            this.StartTimer();
        }

        //returns true if both members of the team died.
        public bool RegisterPlayerDeath(int teamIndex, int clientIndex) {
            Teams[teamIndex].SetAliveStatus(clientIndex, false);

            if (!Teams[teamIndex].isTeamAlive()) {
                playersStillAlive.Remove(clientIndex);
                playersStillAlive.Remove(Teams[teamIndex].GetTeammate(clientIndex));
                this.activeTeams -= 1;
                return false;
            }

            return true;
        }

        public void NewGame() {
            foreach (Team t in Teams) {
                if (t != null)
                    t.ResetPlayers();
            }

            RestartTimer();
        }

        public bool PlayerReady(int connectedClients) {
            this.playersReady += 1;
            return playersReady == connectedClients;
        }

        public bool isGameEnd() {
            int condition1 = 0;
            foreach (int index in playersStillAlive)
                condition1++;

        
            return condition1<=2;
        }

        public void GameReady() {
            this.timeElapsed = Settings.MAX_GAME_TIMER - 15;
            return;
        }

        public void ManualStart() {
            this.manualStart = true;
        }

        public int isGameManuallyStarted() {
            if (manualStart) return 1;
            return 0;
        }
    }
}
