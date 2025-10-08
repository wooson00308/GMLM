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

            var center = (Vector2)transform.position;
            var hits = Physics2D.OverlapCircleAll(center, _senseRadius, _projectileMask);
            foreach (var h in hits)
            {
                var proj = h.GetComponent<Projectile>();
                if (proj == null) continue;
                if (_mecha != null && proj.ShooterTeam == _mecha.TeamId) continue; // 아군 무시 (Mecha 없으면 스킵)

                // 상대 위치/속도
                Vector2 r = (Vector2)proj.transform.position - center;
                Vector2 v = proj.DirectionXY * proj.Speed;
                if (v.sqrMagnitude <= 1e-6f) continue;
                // 다가오는지 확인
                if (Vector2.Dot(r, v) >= 0f) continue;

                float tStar = -Vector2.Dot(r, v) / v.sqrMagnitude; // 최소 근접 시간
					if (tStar < 0f) continue;
                Vector2 closest = r + v * tStar;
                float dMin = closest.magnitude;
                float hitRadius = _mechaRadius + _projectileRadius;
                if (dMin <= hitRadius)
                {
                    if (tStar < IncomingTTI)
                    {
							IncomingTTI = tStar;
							IncomingDir = v.normalized; // 투사체 진행 방향
							IncomingOrigin = (Vector2)proj.transform.position;
							IncomingVelocity = v;
                    }
                }
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


