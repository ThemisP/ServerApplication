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
        private bool isAlive;

        private float rotZ;

        private int ClientTeammember;
        private int roomNumber;
        private int gameRoomNumber;
        
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

        public void SetRoomNumber(int roomNumber) {
            this.roomNumber = roomNumber;
        }
        public void SetGameRoomNumber(int gameRoomNumber) {
            this.gameRoomNumber = gameRoomNumber;
        }
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

        public void SetIsAlive(bool isAlive)
        {
            this.isAlive = isAlive;
        }
        #endregion

        #region "Getters"

        public int GetRoomIndex() {
            return this.roomNumber;
        }
        public int GetGameRoomIndex() {
            return this.gameRoomNumber;
        }
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
        public bool GetAliveStatus()
        {
            return this.isAlive;
        }
        #endregion
    }
}
