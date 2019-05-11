using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication {
    class Player {
        private int ID; //(index inside game used for clients) W.I.P. used like an id

        private string username;
        private float health;
        //Position (x,y,z)
        private float x;
        private float y;
        private float z;
        //velocity (x,y,z)
        private float xVel;
        private float yVel;
        private float zVel;

        private bool isAlive;

        //rotation(0,rotY,0)
        private float rotY;

        private int ClientTeammember;
        private int roomNumber;
        private int gameRoomNumber;        
        private int teamNumber;
        private float damageDealt = 0;
        private int kills = 0;
        private bool inGame = false;
        private bool inLobby = false;
        public Player(string username, int id) {
            this.username = username;
            this.ID = id;
        }

        public Player(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void JoinGame(int gameRoomNumber, int teammate, int teamnumber) {
            this.inGame = true;
            this.gameRoomNumber = gameRoomNumber;
            kills = 0;
            damageDealt = 0;
            this.ClientTeammember = teammate;
            this.teamNumber = teamnumber;
            this.isAlive = true;
            health = 100f;
        }

        public void LeaveGame() {
            this.inGame = false;
        }

        #region "Extra"
        public void TakeDamage(float amount) {
            this.health -= amount;
            if (this.health <= 0f) {
                this.isAlive = false;
                this.health = 0f;
            }
        }
        public bool IsInGame() {
            return this.inGame;
        }
        public bool IsAlive() {
            return this.isAlive;
        }
        #endregion

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

        public void SetVelocity(float x, float y, float z) {
            this.xVel = x;
            this.yVel = y;
            this.zVel = z;
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

        public void SetHealth(float health) {
            this.health = health;
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
        public float GetVelX() {
            return this.xVel;
        }
        public float GetVelY() {
            return this.yVel;
        }
        public float GetVelZ() {
            return this.zVel;
        }
        public float GetRotY() {
            return this.rotY;
        }
        public string GetUsername() {
            return this.username;
        }
        public float GetHealth() {
            return this.health;
        }
        #endregion

        #region Update
        public void UpdateDamageDealt(float d) {
            this.damageDealt += d;
        }

        public void AddKill() {
            this.kills +=1;
        }

        public void LeaveRoom() {
            this.inLobby = false;
            this.roomNumber = -1;            
        }
        #endregion

        public void Reset() {
            kills = 0;
            damageDealt = 0;
            this.isAlive = true;
            health = 100f;
        }

        public void Revive() {
            this.isAlive = true;
            health = 30f;
        }
    }
}
