using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ServerApplication {
    class Program {
        private static Thread consoleThread;
        private static bool consoleRunning;
        static void Main(string[] args) {
            consoleThread = new Thread(new ThreadStart(ConsoleThread));
            consoleThread.Start();
            Network.instance.ServerStart();
        }

        private static void ConsoleThread() {
            string line;
            consoleRunning = true;

            while (consoleRunning) {
                line = Console.ReadLine();
                if (String.IsNullOrWhiteSpace(line)) {
                    //consoleRunning = false;
                    //return;
                } else if (line.ToLower() == "exit") {
                    consoleRunning = false;
                    return;
                } else if (line.ToLower() == "creategame") {
                    Network.instance.gameHandler.CreateGame();
                } else {

                }
            }
        }
    }
}
