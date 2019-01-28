using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApplication {
    class Bullet {

        //position (x,y,z)
        private float x;
        private float y;
        private float z;

        //rotation
        private float yRot;

        //speed
        private float speed;
        private float lifetime;

        public Bullet(float x, float y, float z, float yRot, float speed, float lifetime) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.yRot = yRot;
            this.speed = speed;
            this.lifetime = lifetime;
        }

        #region "Getters"
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
            return this.yRot;
        }
        public float GetSpeed() {
            return this.speed;
        }
        public float GetLifetime() {
            return this.lifetime;
        }
        #endregion


    }
}
