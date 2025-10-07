using UnityEngine;

namespace GMLM.Game
{
    public struct ProjectileSpawnData
    {
        public Transform target;
        public int shooterTeam;
        public int damage;
        public float speed;
        public float hitRadius;
        public float lifeTime;

        public ProjectileSpawnData(Transform target, int shooterTeam, int damage, float speed, float hitRadius, float lifeTime)
        {
            this.target = target;
            this.shooterTeam = shooterTeam;
            this.damage = damage;
            this.speed = speed;
            this.hitRadius = hitRadius;
            this.lifeTime = lifeTime;
        }
    }
}


