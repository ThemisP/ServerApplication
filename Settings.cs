﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication {
    class Settings {
        public const int MAX_PLAYERS = 100;
        public const int SEND_BUFFER_SIZE = 4096;
        public const int RECEIVE_BUFFER_SIZE = 4096;
        public const int MAX_ROOMS = 64;
        public const int MIN_ROOMS = 20;
        public const float MAX_START_TIMER = 300f;
        public const float MAX_GAME_TIMER = 900f;
    }
}
