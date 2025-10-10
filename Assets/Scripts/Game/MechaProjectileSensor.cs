using UnityEngine;

namespace GMLM.Game
{
    public class MechaProjectileSensor : MonoBehaviour
    {
        [SerializeField] private float _senseRadius = 6f;
        [SerializeField] private LayerMask _projectileMask = ~0;
        [SerializeField] private float _updateInterval = 0.05f;
        [SerializeField] private float _projectileRadius = 0.1f;
        [SerializeField] private float _mechaRadius = 0.5f;

        private float _timer;
        private Mecha _mecha;

        public Vector2 IncomingDir { get; private set; }
        public float IncomingTTI { get; private set; } = float.PositiveInfinity;
		public Vector2 IncomingOrigin { get; private set; }
		public Vector2 IncomingVelocity { get; private set; }
        public bool IncomingIsHighThreat { get; private set; } = false;
        
        // 호밍 투사체 대응을 위한 추가 정보
        public bool IncomingIsHoming { get; private set; } = false;
        public float IncomingProjectileSpeed { get; private set; } = 0f;

        private void Awake()
        {
            _mecha = GetComponent<Mecha>();
            if (_mecha == null)
            {
                _mecha = GetComponentInParent<Mecha>();
            }
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _updateInterval;
                ScanProjectiles();
            }
        }

        private void ScanProjectiles()
        {
			IncomingTTI = float.PositiveInfinity;
			IncomingDir = Vector2.zero;
			IncomingOrigin = Vector2.zero;
			IncomingVelocity = Vector2.zero;
            IncomingIsHighThreat = false;
            IncomingIsHoming = false;
            IncomingProjectileSpeed = 0f;

            var center = (Vector2)transform.position;
            var hits = Physics2D.OverlapCircleAll(center, _senseRadius, _projectileMask);
            
            // 두 단계 스캔: 고위협 우선, 없으면 일반
            float closestTTI = float.PositiveInfinity;
            float closestHighThreatTTI = float.PositiveInfinity;
            Projectile closestProj = null;
            Projectile closestHighThreatProj = null;
            
            foreach (var h in hits)
            {
                var proj = h.GetComponent<Projectile>();
                if (proj == null) continue;
                if (_mecha != null && proj.ShooterTeam == _mecha.TeamId) continue;

                // 상대 위치/속도
                Vector2 r = (Vector2)proj.transform.position - center;
                Vector2 v = proj.DirectionXY * proj.Speed;
                if (v.sqrMagnitude <= 1e-6f) continue;
                if (Vector2.Dot(r, v) >= 0f) continue; // 멀어지는 중

                float tStar = -Vector2.Dot(r, v) / v.sqrMagnitude;
					if (tStar < 0f) continue;
                Vector2 closest = r + v * tStar;
                float dMin = closest.magnitude;
                float hitRadius = _mechaRadius + _projectileRadius;
                
                if (dMin <= hitRadius)
                {
                    // 고위협 투사체 우선 추적
                    if (proj.IsHighThreat)
                    {
                        if (tStar < closestHighThreatTTI)
                        {
                            closestHighThreatTTI = tStar;
                            closestHighThreatProj = proj;
                        }
                    }
                    else
                    {
                        if (tStar < closestTTI)
                        {
                            closestTTI = tStar;
                            closestProj = proj;
                        }
                    }
                }
            }
            
            // 고위협이 있으면 최우선
            if (closestHighThreatProj != null)
            {
                IncomingTTI = closestHighThreatTTI;
                Vector2 v = closestHighThreatProj.DirectionXY * closestHighThreatProj.Speed;
                IncomingDir = v.normalized;
                IncomingOrigin = (Vector2)closestHighThreatProj.transform.position;
                IncomingVelocity = v;
                IncomingIsHighThreat = true;
                IncomingIsHoming = closestHighThreatProj.IsHoming;
                IncomingProjectileSpeed = closestHighThreatProj.Speed;
            }
            // 없으면 일반 투사체
            else if (closestProj != null)
            {
                IncomingTTI = closestTTI;
                Vector2 v = closestProj.DirectionXY * closestProj.Speed;
                IncomingDir = v.normalized;
                IncomingOrigin = (Vector2)closestProj.transform.position;
                IncomingVelocity = v;
                IncomingIsHighThreat = false;
                IncomingIsHoming = closestProj.IsHoming;
                IncomingProjectileSpeed = closestProj.Speed;
            }
        }

		public bool WillPathCross(Vector2 desiredDir, float pathSpeed, float horizon, float clearance, out float crossTime, out float minDistance)
		{
			crossTime = 0f;
			minDistance = float.PositiveInfinity;
			if (!float.IsFinite(pathSpeed) || pathSpeed <= 0f) return false;
			Vector2 posA = (Vector2)transform.position;
			Vector2 velA = desiredDir.normalized * pathSpeed;
			Vector2 posB = IncomingOrigin;
			Vector2 velB = IncomingVelocity;
			return PredictionUtils.WillPathsCrossWithin(posA, velA, posB, velB, horizon, clearance, out crossTime, out minDistance);
		}
    }
}


