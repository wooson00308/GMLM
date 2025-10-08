using NPOI.SS.Formula.Functions;
using UnityEngine;

namespace GMLM.Game
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 12f;
        [SerializeField] private float _lifeTime = 3f;
        [SerializeField] private bool _isHoming = true; // true: 추적, false: 직선 비유도
        [SerializeField] private GameObject _hitEffect;
        [SerializeField] private GameObject _destroyEffect;
        private Transform _target;
        private int _damage;
        private int _shooterTeam;
        private float _timeLeft;
        private Rigidbody2D _rb;
        private Collider2D _trigger;
        private Vector3 _initialDir; // 비유도 초기 비행 방향(XY)

        // Telemetry for sensors
        public bool IsHoming => _isHoming;
        public int ShooterTeam => _shooterTeam;
        public float Speed => _speed;
        public Vector2 DirectionXY
        {
            get
            {
                if (_isHoming && _target != null)
                {
                    Vector3 selfPos = transform.position; selfPos.z = 0f;
                    Vector3 tgtPos = _target.position; tgtPos.z = 0f;
                    Vector3 d = (tgtPos - selfPos);
                    return d.sqrMagnitude > 0f ? ((Vector2)d.normalized) : (Vector2)_initialDir;
                }
                return (Vector2)_initialDir;
            }
        }

        // 명시적인 초기 방향만 허용
        public void Initialize(Transform target, int shooterTeam, int damage)
        {
            _target = target;
            _shooterTeam = shooterTeam;
            _damage = Mathf.Max(0, damage);
            _timeLeft = _lifeTime;

            Vector3 dirXY = target.position - transform.position; dirXY.z = 0f;
            _initialDir = dirXY.normalized;
        }

		// 비유도 발사: 초기 방향을 직접 지정
		public void InitializeWithDirection(Vector2 initialDirection, int shooterTeam, int damage)
		{
			_target = null;
			_isHoming = false;
			_shooterTeam = shooterTeam;
			_damage = Mathf.Max(0, damage);
			_timeLeft = _lifeTime;
			Vector3 dir = new Vector3(initialDirection.x, initialDirection.y, 0f);
			if (dir.sqrMagnitude <= 0f)
			{
				dir = transform.right;
			}
			_initialDir = dir.normalized;
		}

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _trigger = GetComponent<BoxCollider2D>();
            if (_trigger == null)
            {
                _trigger = gameObject.AddComponent<BoxCollider2D>();
            }
            _trigger.isTrigger = true;
        }

        private void OnEnable()
        {
            _timeLeft = _lifeTime;
        }

        private void Update()
        {
            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f)
            {
                if (_destroyEffect != null)
                {
                    Instantiate(_destroyEffect, transform.position, transform.rotation);
                }
                Destroy(gameObject);
                return;
            }

            // XY 평면 고정
            Vector3 selfPos = transform.position; selfPos.z = 0f;
            Vector3 dir = (_isHoming && _target != null)
                ? (new Vector3(_target.position.x, _target.position.y, 0f) - selfPos).normalized
                : _initialDir;
            if (dir.sqrMagnitude > 0f)
            {
                transform.position = selfPos + dir * _speed * Time.deltaTime;
                transform.right = dir; // 이동 방향을 바라보게 회전 (호밍 시 타겟 방향)
            }

            // Trigger-based collision handles hits
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var mecha = other.GetComponent<Mecha>();
            if (mecha == null) return;
            if (!mecha.IsAlive) return;
            if (mecha.TeamId == _shooterTeam) return;
            mecha.TakeDamage(_damage);
            if (_hitEffect != null)
            {
                Instantiate(_hitEffect, transform.position, transform.rotation);
            }
            Destroy(gameObject);
        }
    }
}


