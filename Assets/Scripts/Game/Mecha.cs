using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace GMLM.Game
{
    internal static class MechaRegistry
    {
        private static readonly HashSet<Mecha> _all = new HashSet<Mecha>();

        public static void Register(Mecha mecha)
        {
            if (mecha == null) return;
            _all.Add(mecha);
        }

        public static void Unregister(Mecha mecha)
        {
            if (mecha == null) return;
            _all.Remove(mecha);
        }

        public static Mecha FindClosestEnemy(Vector3 position, int teamId)
        {
            Mecha best = null;
            float bestDistSq = float.PositiveInfinity;
            foreach (var m in _all)
            {
                if (m == null || !m.IsAlive || m.TeamId == teamId) continue;
                float d2 = (m.transform.position - position).sqrMagnitude;
                if (d2 < bestDistSq)
                {
                    bestDistSq = d2;
                    best = m;
                }
            }
            return best;
        }
    }

    public class Mecha : MonoBehaviour
    {
        private List<Weapon> _weapons;
        private List<Part> _parts;
        private Pilot _pilot;
        [SerializeField] private List<Weapon> _weaponsAll = new List<Weapon>();

        [Header("Team & Stats")]
        [SerializeField] private int _teamId = 1; // 1팀, 2팀 ...
        [SerializeField] private int _maxHp = 100;
        [SerializeField] private int _currentHp = 100;
        [SerializeField] private float _moveSpeed = 3.5f; // units/sec (최고 속도)
        [SerializeField] private float _acceleration = 12f;   // units/sec^2
        [SerializeField] private float _deceleration = 16f;   // units/sec^2
        [SerializeField] private float _turnRateDeg = 360f;   // deg/sec, 우측=정면 기준 회전 속도
        // moved to Weapon

        [Header("Stagger System (AC6 Style)")]
        [SerializeField, MinValue(1)] private int _maxStagger = 1000; // 스태거 임계값
        [SerializeField, MinValue(0f)] private float _staggerDecayRate = 200f; // 초당 감소량
        [SerializeField, MinValue(0.1f)] private float _staggerDuration = 2.5f; // 스태거 지속시간
        [SerializeField, MinValue(1f)] private float _staggerDamageMultiplier = 1.5f; // 스태거 상태 데미지 배율
        [SerializeField, MinValue(0f), Tooltip("스태거 회복 시작 지연 (피격 시 리셋)")] private float _staggerRecoveryDelay = 0.6f;

        [Header("Energy")]
        [SerializeField] private float _maxEnergy = 100f;
        [SerializeField] private float _currentEnergy = 100f;
        [SerializeField] private float _energyRegenPerSec = 10f;
        [SerializeField] private float _energyRegenDelay = 1.0f;

        public int TeamId => _teamId;
        public int MaxHp => _maxHp;
        public int CurrentHp => _currentHp;
        public bool IsAlive => _currentHp > 0;
        public bool IsDead => !IsAlive;
        public float MoveSpeed => _moveSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
        public float TurnRateDeg => _turnRateDeg;
        public float MaxEnergy => _maxEnergy;
        public float CurrentEnergy => _currentEnergy;
        public float EnergyRegenPerSec => _energyRegenPerSec;
        public float EnergyRegenDelay => _energyRegenDelay;
        public IReadOnlyList<Weapon> WeaponsAll => _weaponsAll;
        public Pilot Pilot => _pilot;
        // Dash parameters for AI calculations
        public float DashDistance => _dashDistance;
        public float DashSpeed => _dashSpeed;
        public float DashCooldown => _dashCooldown;
        public float DashEnergyCost => _dashEnergyCost;
        public bool IsDashing => _isDashing;
        
        // Stagger system properties
        public int MaxStagger => _maxStagger;
        public float CurrentStagger => _currentStagger;
        public bool IsStaggered => _isStaggered;
        public float StaggerProgress => _currentStagger / _maxStagger;
        public float StaggerRecoveryCooldown => _staggerRecoveryCooldown;
        public float StaggerDuration => _staggerDuration;
        
        // 전투 성향에 따라 무기를 사거리 기준으로 정렬한 리스트 반환
        public List<Weapon> GetWeaponsSortedByCombatStyle(CombatStyle style)
        {
            var sorted = new List<Weapon>(_weaponsAll);
            if (style == CombatStyle.Melee)
            {
                // 사거리 낮은 순 (오름차순)
                sorted.Sort((a, b) => a.AttackRange.CompareTo(b.AttackRange));
            }
            else // Ranged
            {
                // 사거리 높은 순 (내림차순)
                sorted.Sort((a, b) => b.AttackRange.CompareTo(a.AttackRange));
            }
            return sorted;
        }

        // 내부 상태: 현재 이동 속도(스칼라)
        private float _currentSpeed = 0f;
        private float _energyRegenCooldown = 0f;
        // Evade sensor cache (legacy - now handled by MechaProjectileSensor)
        // private Vector2 _incomingDir = Vector2.zero;
        // private float _incomingTTI = float.PositiveInfinity;
        
        // Stagger system state
        private float _currentStagger = 0f;
        private bool _isStaggered = false;
        private float _staggerTimer = 0f;
        private float _staggerRecoveryCooldown = 0f; // 회복 지연 타이머

        [Header("Animation")]
        [SerializeField] private MechaAnimation _mechaAnimation;

        [Header("Dash")]
        [SerializeField] private float _dashDistance = 3.0f;
        [SerializeField] private float _dashSpeed = 20.0f;
        [SerializeField] private float _dashCooldown = 1.0f;
        [SerializeField] private float _dashEnergyCost = 20.0f;
        private float _dashCooldownTimer = 0f;
        private bool _isDashing = false;
        private Vector3 _dashDir = Vector3.zero; // XY
        private float _dashRemainingDistance = 0f;

        // World-space velocity on XY plane (for predictive aiming, AI, FX)
        public Vector2 WorldVelocity2D { get; private set; } = Vector2.zero;
        private Vector3 _lastPosition;
        
        // 예약 시스템 제거됨: 대시 완료 후 자연스럽게 AttackAction에서 공격 처리
        private void Awake()
        {
            _pilot = GetComponent<Pilot>(); 
        }
    
        private void OnEnable()
        {
            MechaRegistry.Register(this);
            RefreshWeapons();
            if (_mechaAnimation == null)
            {
                _mechaAnimation = GetComponentInChildren<MechaAnimation>(true);
            }
            _lastPosition = transform.position;
            WorldVelocity2D = Vector2.zero;
        }

        private void OnDisable()
        {
            MechaRegistry.Unregister(this);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Dash progression
            if (_isDashing)
            {
                if (_dashRemainingDistance > 0f)
                {
                    float step = Mathf.Min(_dashSpeed * dt, _dashRemainingDistance);
                    Vector3 selfPos = transform.position; selfPos.z = 0f;
                    Vector3 next = selfPos + _dashDir * step;
                    next.z = transform.position.z;
                    transform.position = next;
                    _dashRemainingDistance -= step;
					if (_mechaAnimation != null)
					{
						_mechaAnimation.UpdateDashThrusters();
					}
                }
                else
                {
                    _isDashing = false;
					if (_mechaAnimation != null)
					{
						_mechaAnimation.StopDashFx();
					}
					
					// 예약 시스템 제거: 대시 완료 후 자연스럽게 AttackAction에서 공격 처리
                }
            }

            // Cooldowns
            if (_dashCooldownTimer > 0f) _dashCooldownTimer -= dt;
            if (_energyRegenCooldown > 0f)
            {
                _energyRegenCooldown -= dt;
            }
            else
            {
                if (_currentEnergy < _maxEnergy)
                {
                    _currentEnergy = Mathf.Min(_maxEnergy, _currentEnergy + _energyRegenPerSec * dt);
                }
            }
            
            // Stagger system update
            UpdateStaggerSystem(dt);

            // Death check safety
            if (_currentHp <= 0)
            {
                // 최소 처리: 비활성화로 종료
                gameObject.SetActive(false);
                return;
            }

            // Update world velocity (XY) after all movement for this frame
            if (dt > 0f)
            {
                Vector3 pos = transform.position; pos.z = 0f;
                Vector3 prev = _lastPosition; prev.z = 0f;
                Vector2 vel = (pos - prev) / dt;
                WorldVelocity2D = vel;
            }
            _lastPosition = transform.position;
        }

        public void InitializeTeam(int teamId)
        {
            _teamId = teamId;
        }

        public void RefreshWeapons()
        {
            _weaponsAll.Clear();
            GetComponentsInChildren(true, _weaponsAll);
			// Ensure MechaAnimation updates internal weapon presence caches (hands/external)
			if (_mechaAnimation == null)
			{
				_mechaAnimation = GetComponentInChildren<MechaAnimation>(true);
			}
			if (_mechaAnimation != null)
			{
				_mechaAnimation.RefreshWeaponPresence();
			}
        }

        public void InitializeStats(int maxHp, float moveSpeed, float attackSpeed, int attackPower)
        {
            _maxHp = Mathf.Max(1, maxHp);
            _currentHp = _maxHp;
            _moveSpeed = Mathf.Max(0f, moveSpeed);
            // kept for backwards compatibility params, but no longer stored
        }

        public void TakeDamage(int amount)
        {
            TakeDamage(amount, 0);
        }
        
        public void TakeDamage(int damage, int impact)
        {
            if (!IsAlive) return;
            
            // Apply stagger impact
            if (impact > 0)
            {
                _currentStagger += impact;
                
                // Check for stagger threshold
                if (_currentStagger >= _maxStagger && !_isStaggered)
                {
                    EnterStagger();
                }
            }
            
            // 스태거 회복 지연: 스태거 중이 아니고 게이지가 남아있다면 어떤 피해든 지연 타이머 리셋
            if (!_isStaggered && _currentStagger > 0f)
            {
                _staggerRecoveryCooldown = _staggerRecoveryDelay;
            }
            
            // Apply damage with stagger multiplier
            int finalDamage = damage;
            if (_isStaggered)
            {
                finalDamage = Mathf.RoundToInt(damage * _staggerDamageMultiplier);
            }
            
            _currentHp = Mathf.Clamp(_currentHp - Mathf.Max(0, finalDamage), 0, _maxHp);
            if (_currentHp <= 0)
            {
                gameObject.SetActive(false);
            }
        }

		public bool MoveTowards(Vector3 targetPosition)
        {
            if (_isStaggered) return false; // 스태거 상태에서는 이동 불가
            if (_isDashing) return true; // 대시 중에는 독립 이동 우선
            Vector3 selfPos = transform.position; selfPos.z = 0f;
            Vector3 tgtPos = targetPosition; tgtPos.z = 0f;
            Vector3 dir = (tgtPos - selfPos).normalized;
            if (dir.sqrMagnitude <= 0f) return false;
			AimTowards(dir);
            // 가/감속 적용
            float targetSpeed = Mathf.Max(0f, _moveSpeed);
            float rate = (_currentSpeed < targetSpeed) ? _acceleration : _deceleration;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.deltaTime);
            Vector3 next = selfPos + dir * _currentSpeed * Time.deltaTime;
            next.z = transform.position.z;
            transform.position = next;
			if (_mechaAnimation != null)
			{
				_mechaAnimation.UpdateMoveThrusters(new Vector2(dir.x, dir.y) * _currentSpeed);
			}
            return true;
        }

		public bool MoveInDirection(Vector3 direction, bool rotateToMovement = true)
        {
            if (_isStaggered) return false; // 스태거 상태에서는 이동 불가
            if (_isDashing) return true; // 대시 중에는 독립 이동 우선
            Vector3 dir = direction; dir.z = 0f;
            if (dir.sqrMagnitude <= 0f) return false;
            dir.Normalize();
			if (rotateToMovement) AimTowards(dir);
            Vector3 selfPos = transform.position; selfPos.z = 0f;
            float targetSpeed = Mathf.Max(0f, _moveSpeed);
            float rate = (_currentSpeed < targetSpeed) ? _acceleration : _deceleration;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.deltaTime);
            Vector3 next = selfPos + dir * _currentSpeed * Time.deltaTime;
            next.z = transform.position.z;
            transform.position = next;
			if (_mechaAnimation != null)
			{
				_mechaAnimation.UpdateMoveThrusters(new Vector2(dir.x, dir.y) * _currentSpeed);
			}
            return true;
        }

		public void FaceTowards(Vector3 targetPosition)
        {
            if (_isStaggered) return; // 스태거 상태에서는 회전 불가
            Vector3 selfPos = transform.position; selfPos.z = 0f;
            Vector3 tgtPos = targetPosition; tgtPos.z = 0f;
            Vector3 dir = (tgtPos - selfPos);
            if (dir.sqrMagnitude <= 0f) return;
			AimTowards(dir.normalized);
        }

		private void AimTowards(Vector3 desiredDir)
        {
            Vector3 from = transform.right; from.z = 0f;
            Vector3 to = desiredDir; to.z = 0f;
            if (to.sqrMagnitude <= 0f) return;
            from.Normalize(); to.Normalize();
			// 1) 머리/손 먼저 조준
			if (_mechaAnimation != null)
			{
				_mechaAnimation.UpdateAiming(to);
				if (_mechaAnimation.EvaluateBodyAssist(to, out float residualYawDeg, out int sign))
				{
					// 2) 보조 회전: 잔여 각도만큼, 동체 회전 속도 제한
					float stepDeg = Mathf.Min(residualYawDeg, Mathf.Max(0f, _turnRateDeg) * Time.deltaTime);
					if (stepDeg > 0f)
					{
						float stepRad = Mathf.Deg2Rad * (sign * stepDeg);
						Vector3 target = Quaternion.Euler(0f, 0f, stepDeg * sign) * from;
						if (target.sqrMagnitude > 0f) transform.right = target;
					}
				}
			}
			else
			{
				// 폴백: 기존 회전 로직
				float maxRad = Mathf.Deg2Rad * Mathf.Max(0f, _turnRateDeg) * Time.deltaTime;
				Vector3 newDir = Vector3.RotateTowards(from, to, maxRad, 0f);
				if (newDir.sqrMagnitude > 0f) transform.right = newDir;
			}
        }

        private void Reset()
        {
            // Ensure a collider for projectile hits
            var col = GetComponent<Collider2D>();
            if (col == null)
            {
                var cc = gameObject.AddComponent<CircleCollider2D>();
                cc.isTrigger = false; // Mecha is a solid target
                cc.radius = 0.5f;
            }
        }

        #region Energy & Dash API
        public bool SpendEnergy(float amount)
        {
            amount = Mathf.Max(0f, amount);
            if (_currentEnergy < amount) return false;
            _currentEnergy -= amount;
            _energyRegenCooldown = _energyRegenDelay;
            return true;
        }

        public void AddEnergy(float amount)
        {
            _currentEnergy = Mathf.Clamp(_currentEnergy + Mathf.Max(0f, amount), 0f, _maxEnergy);
        }

        public bool CanDash()
        {
            return !_isStaggered && !_isDashing && _dashCooldownTimer <= 0f && _currentEnergy >= _dashEnergyCost;
        }

        public bool TryDash(Vector3 direction)
        {
            Vector3 dir = direction; dir.z = 0f;
            if (dir.sqrMagnitude <= 0f) return false;
            if (!CanDash()) return false;
            dir.Normalize();

            if (!SpendEnergy(_dashEnergyCost)) return false;

            _isDashing = true;
            _dashDir = dir;
            _dashRemainingDistance = Mathf.Max(0f, _dashDistance);
            _dashCooldownTimer = _dashCooldown;
			if (_mechaAnimation != null)
			{
				float dashDuration = (_dashSpeed > 0f) ? (_dashDistance / _dashSpeed) : 0.15f;
				_mechaAnimation.PlayDashFx(new Vector2(dir.x, dir.y), dashDuration);
			}
            return true;
        }

        public bool TryDashForward()
        {
            return TryDash(transform.right);
        }
        
        // 예약 시스템 제거됨: 대시 완료 후 자연스럽게 AttackAction에서 공격 처리
        #endregion
        
        #region Stagger System
        private void UpdateStaggerSystem(float deltaTime)
        {
            if (_isStaggered)
            {
                // Update stagger duration timer
                _staggerTimer -= deltaTime;
                if (_staggerTimer <= 0f)
                {
                    ExitStagger();
                }
            }
            else
            {
                // 회복 지연 타이머 감소
                if (_staggerRecoveryCooldown > 0f)
                {
                    _staggerRecoveryCooldown = Mathf.Max(0f, _staggerRecoveryCooldown - deltaTime);
                }

                // 지연이 끝나면 스태거 게이지 감소 시작
                if (_staggerRecoveryCooldown <= 0f && _currentStagger > 0f)
                {
                    _currentStagger = Mathf.Max(0f, _currentStagger - _staggerDecayRate * deltaTime);
                }
            }
        }
        
        private void EnterStagger()
        {
            if (_isStaggered) return;
            _isStaggered = true;
            _staggerTimer = _staggerDuration;
            _currentStagger = _maxStagger; // Cap at max when staggered
            _staggerRecoveryCooldown = 0f; // 스태거 중에는 회복 지연 의미 없음
            
            if (_mechaAnimation != null)
            {
                _mechaAnimation.PlayStaggerEffect();
            }
        }
        
        private void ExitStagger()
        {
            _isStaggered = false;
            _staggerTimer = 0f;
            _currentStagger = 0f; // Reset stagger gauge after stagger ends
            _staggerRecoveryCooldown = _staggerRecoveryDelay; // 회복 시작까지 지연
            
            // TODO: Add stagger recovery effects here
        }
        #endregion
    }
}
