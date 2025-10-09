using UnityEngine;
using Sirenix.OdinInspector;

namespace GMLM.Game
{
    public class Weapon : MonoBehaviour
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Image { get; private set; }

        [Header("Stats")]
        [SerializeField, MinValue(0)] private int _attackPower = 10;
        [SerializeField, MinValue(0.01f)] private float _attackSpeed = 1.0f; // attacks/sec
        [SerializeField, MinValue(0f)] private float _range = 8f; // meters

        [Header("Mode")]
        [SerializeField] private WeaponType _type = WeaponType.Ranged;
        [ShowIf("IsRanged")]
        [SerializeField] private Projectile _projectilePrefab;
        [ShowIf("IsRanged")]
        [SerializeField] private Transform _muzzle;
        [SerializeField] private Transform _fireEffect;

		[Header("Spread (Ranged)")]
		[ShowIf("IsRanged")]
		[SerializeField, Tooltip("발사 시 ±각도로 무작위 편차(도)"), Range(0f, 180f)] private float _spreadDeg = 2.5f;

        // Optional legacy-like fields (not used directly by logic yet)
        public WeaponType Type { get { return _type; } }
        public int Damage { get; private set; }
        public int Range { get { return Mathf.RoundToInt(_range); } }
        public int FireRate { get; private set; }
        public int MagazineSize { get; private set; } 
        public int ReloadTime { get; private set; }
        public int Accuracy { get; private set; }

        private float _cooldownTimer;

        // Odin helper
        private bool IsRanged => _type == WeaponType.Ranged;

        public float AttackRange => _range;
        public float RemainingCooldown => Mathf.Max(0f, _cooldownTimer);

		// Exposed descriptors for aiming logic (read-only)
		public float ProjectileSpeed => _projectilePrefab != null ? _projectilePrefab.Speed : 0f;
		public bool IsProjectileHoming => _projectilePrefab != null && _projectilePrefab.IsHoming;

		private void Update()
		{
			if (_cooldownTimer > 0f)
			{
				_cooldownTimer -= Time.deltaTime;
			}
		}

        public bool TryAttack(Mecha self, GameObject targetGo)
        {
            if (self == null || targetGo == null) return false;
            var target = targetGo.GetComponent<Mecha>();
            return TryAttack(self, target);
        }

        public bool TryAttack(Mecha self, Mecha target)
        {
            if (self == null || target == null || !target.IsAlive) return false;
            if (_cooldownTimer > 0f) return false;
            if (self.TeamId == target.TeamId) return false;

            float distance = Vector3.Distance(self.transform.position, target.transform.position);
            if (distance > _range) return false;

            int damage = Mathf.Max(0, _attackPower);
            if (_fireEffect != null)
            {
                var parent = _muzzle != null ? _muzzle : self.transform;
                var spawnPos = parent.position;
                var spawnRot = parent.rotation;
                var fx = Instantiate(_fireEffect, spawnPos, spawnRot);
                fx.transform.SetParent(parent);
            }
            if (_type == WeaponType.Melee)
            {
                target.TakeDamage(damage);
            }
			else
            {
				if (_projectilePrefab == null)
                {
                    // Fallback to hitscan if no projectile prefab assigned
                    target.TakeDamage(damage);
                }
				else
				{
					// Fire strictly along current muzzle orientation. Predictive aiming is handled upstream (AttackAction)
					var spawnPos = (_muzzle != null ? _muzzle.position : self.transform.position);
					var spawnRot = (_muzzle != null ? _muzzle.rotation : self.transform.rotation);
					var proj = Instantiate(_projectilePrefab, spawnPos, spawnRot);
					if (proj.IsHoming)
					{
						proj.Initialize(target.transform, self.TeamId, damage);
					}
					else
					{
						// derive direction from current muzzle, then apply small spread
						Vector3 dir = spawnRot * Vector3.up; dir.z = 0f;
						if (dir.sqrMagnitude <= 1e-6f) dir = Vector3.right;
						dir.Normalize();
						float yaw = Random.Range(-_spreadDeg, _spreadDeg);
						Vector3 spreadDir = (Quaternion.Euler(0f, 0f, yaw) * dir).normalized;
						proj.InitializeWithDirection(new Vector2(spreadDir.x, spreadDir.y), self.TeamId, damage);
					}
				}
            }

			float atkSpd = Mathf.Max(0.01f, _attackSpeed);
			_cooldownTimer = 1f / atkSpd;
			// no spread accumulation; single-field spread only
            return true;
        }
    }
}