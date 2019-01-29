using System;
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

        public void AddBullet(int gameIndex, string bulletId, float x, float y, float z, float rotY, float speed, float lifetime) {
            if(_games[gameIndex] != null) {
                _games[gameIndex].AddBullet(bulletId, x, y, z, rotY, speed, lifetime);
            } else {
                Console.WriteLine("Player trying to spawn a bullet in game index " + gameIndex + " but game does not exist");
            }
        }

        public void RemoveBullet(int gameIndex, string bulletid) {
            if (_games[gameIndex] != null) {
                _games[gameIndex].RemoveBullet(bulletid);
            } else {
                Console.WriteLine("Player trying to destroy a bullet in game index " + gameIndex + " but game does not exist");
            }
        }

        public int[] GetPlayersInGame(int gameIndex, int clientIndex) {
            if(_games[gameIndex] != null) {
                return _games[gameIndex].GetConnectedPlayersBut(clientIndex);
            }
            return null;
        }
    }

    class Game {
        private int GameIndex;
        private int[] connectedClients = new int[100];
        private int NumberOfConnectedClients = 0;
        private Team[] Teams = new Team[50];
        private Dictionary<string, Bullet> Bullets;
        

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
                if (i < 50) {
                    Teams[i] = new Team(i, GameIndex);
                }
            }
            _state = GameState.Empty;
            Bullets = new Dictionary<string, Bullet>();
        }

        public Game(int index, int ClientIndex) {
            this.GameIndex = index;
            for (int i = 0; i < 100; i++) {
                connectedClients[i] = -1;
                if (i < 50) {
                    Teams[i] = new Team(i, GameIndex);
                }
            }
            _state = GameState.Empty;
            AddPlayer(ClientIndex);
            Bullets = new Dictionary<string, Bullet>();
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
                NumberOfConnectedClients += 1;
                if (NumberOfConnectedClients==100) {
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
            while (found<2 && count < 100) {
                if (connectedClients[count] == -1) {
                    if (found == 0) indexOne = count;
                    else indexTwo = count;
                    found++;
                }
                count++;
            }

            if (found>1) {
                connectedClients[indexOne] = ClientOneIndex;
                connectedClients[indexTwo] = ClientTwoIndex;
                int teamIndex = AddTeam(ClientOneIndex, ClientTwoIndex);
                NumberOfConnectedClients += 2;
                if (NumberOfConnectedClients==100) {
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
                    team.createTeam(ClientOneIndex,ClientTwoIndex);
                    return team.getIndex();
                }
            }
            return -1;
        }

        public void AddBullet(string bulletId,float x, float y, float z, float rotY, float speed, float lifetime) {
            Bullets.Add(bulletId, new Bullet(x, y, z, rotY, speed, lifetime));
        }

        public void RemoveBullet(string bulletId) {
            Bullet bullet;
            if(Bullets.TryGetValue(bulletId, out bullet)){
                Bullets.Remove(bulletId);
            }
        }
        public int[] GetConnectedPlayers() {
            List<int> players = new List<int>();
            for(int i=0; i<100; i++) {
                if(connectedClients[i] != -1) {
                    players.Add(connectedClients[i]);
                }
            }
            return players.ToArray();
        }

        public int[] GetConnectedPlayersBut(int index) {
            List<int> players = new List<int>();
            
            for (int i = 0; i < 100; i++) {
                if (connectedClients[i] != -1 && connectedClients[i] != index) {
                    players.Add(connectedClients[i]);
                }
            }
            return players.ToArray();
        }

    }
}
