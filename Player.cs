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
        private float z;

        private float rotZ;

        private int ClientTeammember;
        
        public Player(string username) {
            this.username = username;
        }

        public Player(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        #region "Setters"
        public void ChangeUsername(string username) {
            this.username = username;
        }

        public void SetLocation(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void SetRotation(float rotZ) {
            this.rotZ = rotZ;
        }

        public void SetTeammember(int ClientIndex) {
            this.ClientTeammember = ClientIndex;
        }
        #endregion

        #region "Getters"

        public int GetTeammemberClient() {
            return this.ClientTeammember;
        }        
        public float GetPosX() {
            return this.x;
        }
        public float GetPosY() {
            return this.y;
        }
        public float GetPosZ() {
            return this.z;
        }
        public float GetRotZ() {
            return this.rotZ;
        }
        public string GetUsername() {
            return this.username;
        }
        #endregion
    }
}
