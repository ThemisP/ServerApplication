using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication {
    class Team {
        private int index;
        private int gameIndex;

        private Player player1;
        private Player player2;

        public bool empty = true;

        public Team(int i, int gameIndex) {
            this.index = i;
            this.gameIndex = gameIndex;
        }

        public void createTeam(int ClientOneIndex, int ClientTwoIndex) {
            player1 = Network.Clients[ClientOneIndex].player;
            player1.SetTeamNumber(this.index);
            player2 = Network.Clients[ClientTwoIndex].player;
            player2.SetTeamNumber(this.index);
            empty = false;
        }

        public int getIndex() {
            return this.index;
        }
        public int getGameIndex() {
            return this.gameIndex;
        }

        public bool isEmpty() {
            return empty;
        }

    }
}
