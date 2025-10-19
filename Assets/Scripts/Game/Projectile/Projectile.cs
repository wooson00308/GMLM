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
        
        [Header("Threat Level")]
        [SerializeField, Tooltip("고위협 투사체 (바주카/미사일): 삐삐삐 경고 + 강제 대시 유발")]
        private bool _isHighThreat = false;
        
        [Header("Homing Settings (AC6 Style)")]
        [SerializeField] private float _homingSpeed =10f;
        [SerializeField, Tooltip("발사 후 호밍 활성화 지연 (초)")] 
        private float _homingDelay = 0.2f;
        [SerializeField, Range(0f, 1f), Tooltip("호밍 강도 (0=직진, 1=최대 추적)")]
        private float _homingStrength = 1.0f;
        [SerializeField, Tooltip("최대 회전 속도 (deg/sec)")]
        private float _maxTurnRateDeg = 180f;
        [SerializeField, Tooltip("호밍 지속 시간 (초, 이후 직진)")]
        private float _homingDuration = 5.0f;
        [SerializeField, Tooltip("추적 가능 최대 각도 (deg, 초과 시 호밍 해제)")]
        private float _maxTrackingAngleDeg = 150f;
        
        private Transform _target;
        private int _damage;
        private int _impactValue;
        private int _shooterTeam;
        private float _timeLeft;
        private float _elapsedTime = 0f; // 발사 후 경과 시간
        private bool _isHomingActive = false;
        private Rigidbody2D _rb;
        private Collider2D _trigger;
        private Vector3 _initialDir; // 비유도 초기 비행 방향(XY)
        private Vector3 _currentDir;  // 현재 비행 방향 (호밍용)

        // Telemetry for sensors
        public bool IsHoming => _isHoming;
        public bool IsHighThreat => _isHighThreat;
        public int ShooterTeam => _shooterTeam;
        public float Speed => _speed;
        public Vector2 DirectionXY
        {
            get
            {
                // 현재 비행 방향 반환 (호밍 여부 무관)
                return (Vector2)_currentDir;
            }
        }

        // 명시적인 초기 방향만 허용
        public void Initialize(Transform target, int shooterTeam, int damage, int impact = 0)
        {
            _target = target;
            _shooterTeam = shooterTeam;
            _damage = Mathf.Max(0, damage);
            _impactValue = Mathf.Max(0, impact);
            _timeLeft = _lifeTime;
            _elapsedTime = 0f;

            Vector3 dirXY = target.position - transform.position; dirXY.z = 0f;
            _initialDir = dirXY.normalized;
            _currentDir = _initialDir;
        }

		// 비유도 발사: 초기 방향을 직접 지정
		public void InitializeWithDirection(Vector2 initialDirection, int shooterTeam, int damage, int impact = 0)
		{
			_target = null;
			_isHoming = false;
			_shooterTeam = shooterTeam;
			_damage = Mathf.Max(0, damage);
			_impactValue = Mathf.Max(0, impact);
			_timeLeft = _lifeTime;
            _elapsedTime = 0f;
            Vector3 dir = new Vector3(initialDirection.x, initialDirection.y, 0f);
			if (dir.sqrMagnitude <= 0f)
			{
                dir = transform.up; // up을 진행방향으로 사용
			}
			_initialDir = dir.normalized;
            _currentDir = _initialDir;
		}

		// 호밍 발사: 초기 방사 방향으로 시작하되 타겟 추적
		public void InitializeWithDirectionAndTarget(Vector2 initialDirection, Transform target, int shooterTeam, int damage, int impact = 0)
		{
			_target = target;
			// _isHoming은 프리팹 설정값 유지 (true여야 함)
			_shooterTeam = shooterTeam;
			_damage = Mathf.Max(0, damage);
			_impactValue = Mathf.Max(0, impact);
			_timeLeft = _lifeTime;
            _elapsedTime = 0f;
            Vector3 dir = new Vector3(initialDirection.x, initialDirection.y, 0f);
			if (dir.sqrMagnitude <= 0f)
			{
                dir = transform.up; // up을 진행방향으로 사용
			}
			_initialDir = dir.normalized;
            _currentDir = _initialDir;
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
            _elapsedTime = 0f;
        }

        private void Update()
        {
            _timeLeft -= Time.deltaTime;
            _elapsedTime += Time.deltaTime;
            
            if (_timeLeft <= 0f)
            {
                if (_destroyEffect != null)
                {
                    Instantiate(_destroyEffect, transform.position, _destroyEffect.transform.rotation);
                }
                Destroy(gameObject);
                return;
            }

            // XY 평면 고정
            Vector3 selfPos = transform.position; selfPos.z = 0f;
            Vector3 dir = _currentDir;
            
            // AC6 스타일 호밍 로직
            if (_isHoming && _target != null)
            {
                // 1) 호밍 활성화 조건 체크
                bool homingActive = _elapsedTime >= _homingDelay 
                                 && _elapsedTime <= (_homingDelay + _homingDuration);

                _isHomingActive = homingActive;
                
                if (homingActive && _homingStrength > 0f)
                {
                    Vector3 toTarget = new Vector3(_target.position.x, _target.position.y, 0f) - selfPos;
                    toTarget.z = 0f;
                    
                    if (toTarget.sqrMagnitude > 1e-6f)
                    {
                        Vector3 targetDir = toTarget.normalized;
                        
                        // 2) 추적 각도 체크
                        float angleToTarget = Vector3.Angle(_currentDir, targetDir);
                        
                        if (angleToTarget <= _maxTrackingAngleDeg)
                        {
                            // 3) 제한된 선회로 타겟 방향으로 회전
                            float maxRotRad = Mathf.Deg2Rad * _maxTurnRateDeg * Time.deltaTime;
                            Vector3 newDir = Vector3.RotateTowards(_currentDir, targetDir, maxRotRad, 0f);
                            
                            // 4) 호밍 강도 적용 (보간)
                            dir = Vector3.Lerp(_currentDir, newDir, _homingStrength).normalized;
                        }
                        // else: 타겟이 시야각 밖 → 직진 유지
                    }
                }
                // else: 호밍 비활성 구간 → 직진
            }
            else
            {
                // 비유도탄은 초기 방향 유지
                dir = _initialDir;
            }
            
            _currentDir = dir;
            
            if (dir.sqrMagnitude > 0f)
            {
                float speed = _isHomingActive ? _homingSpeed : _speed;
                transform.position = selfPos + dir * speed * Time.deltaTime;
                // 시각만 90도 보정 (이동 방향은 유지)
                transform.rotation = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(0f, 0f, 90f);
            }

            // Trigger-based collision handles hits
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var mecha = other.GetComponent<Mecha>();
            if (mecha == null) return;
            if (!mecha.IsAlive) return;
            if (mecha.TeamId == _shooterTeam) return;
            mecha.TakeDamage(_damage, _impactValue);
            if (_hitEffect != null)
            {
                Instantiate(_hitEffect, transform.position, _hitEffect.transform.rotation);
            }
            Destroy(gameObject);
        }
    }
}


