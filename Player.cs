using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication {
    class Player {

        private string username;
        private float x;
        private float y;

        private float rotZ;
        
        public Player(string username) {
            this.username = username;
        }

        public Player(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public void ChangeUsername(string username) {
            this.username = username;
        }

        public void SetLocation(float x, float y) {
            this.x = x;
            this.y = y;
        }

        public void SetRotation(float rotZ) {
            this.rotZ = rotZ;
        }

        public float GetPosX() {
            return this.x;
        }
        public float GetPosY() {
            return this.y;
        }
        public float GetRotZ() {
            return this.rotZ;
        }
        public string GetUsername() {
            return this.username;
        }
    }
}
