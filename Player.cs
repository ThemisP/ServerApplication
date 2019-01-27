using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication {
    class Player {
        private int ID; //(index inside game used for clients) W.I.P. used like an id

        private string username;
        private float x;
        private float y;
        private float z;
        private bool isAlive;

        private float rotY;

        private int ClientTeammember;
        private int roomNumber;
        private int gameRoomNumber;        
        private int teamNumber;
        
        public Player(string username, int id) {
            this.username = username;
            this.ID = id;
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

        public void SetTeamNumber(int teamNumber) {
            this.teamNumber = teamNumber;
        }

        public void SetLocation(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void SetRotation(float rotY) {
            this.rotY = rotY;
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

        public int GetId() {
            return this.ID;
        }
        public int GetRoomIndex() {
            return this.roomNumber;
        }
        public int GetGameRoomIndex() {
            return this.gameRoomNumber;
        }
        public int GetTeammemberClient() {
            return this.ClientTeammember;
        }        
        public int GetTeamNumber() {
            return this.teamNumber;
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
        public float GetRotY() {
            return this.rotY;
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
