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
                    var spawnPos = _muzzle != null ? _muzzle.position : self.transform.position;
                    var spawnRot = _muzzle != null ? _muzzle.rotation : self.transform.rotation;
                    var proj = Instantiate(_projectilePrefab, spawnPos, spawnRot);
                    proj.Initialize(target.transform, self.TeamId, damage);
                }
            }

            float atkSpd = Mathf.Max(0.01f, _attackSpeed);
            _cooldownTimer = 1f / atkSpd;
            return true;
        }
    }
}