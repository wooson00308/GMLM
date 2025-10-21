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
        
        // Assault Boost parameters
        public float AssaultBoostSpeedMultiplier => _assaultBoostSpeedMultiplier;
        public bool IsAssaultBoosting => _isAssaultBoosting;
        
        // Stagger system properties
        public int MaxStagger => _maxStagger;
        public float CurrentStagger => _currentStagger;
        public bool IsStaggered => _isStaggered;
        public float StaggerProgress => _currentStagger / _maxStagger;
        public float StaggerRecoveryCooldown => _staggerRecoveryCooldown;
        public float StaggerDuration => _staggerDuration;
        public bool IsInStartLag => _isInStartLag;
        
        // 근접무기 중 발사 가능한 무기가 있는지 검사
        private bool HasAvailableMeleeWeapon()
        {
            foreach (var weapon in _weaponsAll)
            {
                if (weapon == null) continue;
                if (weapon.Type == WeaponType.Melee && weapon.CanFire)
                    return true;
            }
            return false;
        }

        // 전투 성향에 따라 무기를 사거리 기준으로 정렬한 리스트 반환
        public List<Weapon> GetWeaponsSortedByCombatStyle(CombatStyle style, Mecha target = null)
        {
            var sorted = new List<Weapon>(_weaponsAll);
            
            // 1. 타겟 스태거 체크 (최우선) - 근접무기가 발사 가능할 때만
            if (target != null && target.IsStaggered && HasAvailableMeleeWeapon())
            {
                // 스태거 상태: 근접 무기를 모두 앞으로 배치
                var meleeWeapons = new List<Weapon>();
                var rangedWeapons = new List<Weapon>();
                
                foreach (var weapon in sorted)
                {
                    if (weapon.Type == WeaponType.Melee)
                        meleeWeapons.Add(weapon);
                    else
                        rangedWeapons.Add(weapon);
                }
                
                // 근접 무기끼리는 사거리 오름차순 정렬
                meleeWeapons.Sort((a, b) => a.AttackRange.CompareTo(b.AttackRange));
                // 원거리 무기는 사거리 내림차순 정렬
                rangedWeapons.Sort((a, b) => b.AttackRange.CompareTo(a.AttackRange));
                
                // 근접 무기 먼저, 그 다음 원거리 무기
                sorted.Clear();
                sorted.AddRange(meleeWeapons);
                sorted.AddRange(rangedWeapons);
            }
            else if (style == CombatStyle.Melee)
            {
                // 근접 성향: 모든 무기를 사거리 오름차순 정렬
                sorted.Sort((a, b) => a.AttackRange.CompareTo(b.AttackRange));
            }
            else // Ranged
            {
                // 원거리 성향: 근접 무기 배제, 원거리 무기만 사거리 내림차순 정렬
                var rangedOnly = new List<Weapon>();
                foreach (var weapon in sorted)
                {
                    if (weapon.Type != WeaponType.Melee)
                        rangedOnly.Add(weapon);
                }
                rangedOnly.Sort((a, b) => b.AttackRange.CompareTo(a.AttackRange));
                sorted = rangedOnly;
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

        // StartLag system state
        private bool _isInStartLag = false;
        private float _startLagTimer = 0f;

        [Header("Animation")]
        [SerializeField] private MechaAnimation _mechaAnimation;

        [Header("Dash")]
        [SerializeField] private float _dashDistance = 3.0f;
        [SerializeField] private float _dashSpeed = 20.0f;
        [SerializeField] private float _dashCooldown = 1.0f;
        [SerializeField] private float _dashEnergyCost = 20.0f;
        [SerializeField, Tooltip("대시 속도 커브 (0=시작, 1=끝)")] 
        private AnimationCurve _dashSpeedCurve = AnimationCurve.EaseInOut(0, 0.3f, 1, 0.1f);
        [SerializeField, Tooltip("커브 최고점에서의 속도 배율")] 
        private float _dashPeakSpeedMultiplier = 2.5f;
        private float _dashCooldownTimer = 0f;
        private bool _isDashing = false;
        private Vector3 _dashDir = Vector3.zero; // XY
        private float _dashRemainingDistance = 0f;

        [Header("Assault Boost")]
        [SerializeField] private float _assaultBoostSpeedMultiplier = 1.5f;
        [SerializeField] private float _assaultBoostActivationCost = 15.0f;
        [SerializeField] private float _assaultBoostDrainPerSec = 35.0f;
        [SerializeField, Tooltip("어썰트 부스트 가속 시간 (초)")] 
        private float _assaultBoostAccelTime = 0.4f;
        [SerializeField, Tooltip("어썰트 부스트 감속 시간 (초)")] 
        private float _assaultBoostDecelTime = 0.2f;
        [SerializeField, Tooltip("어썰트 부스트 최대 속도 배율")] 
        private float _assaultBoostMaxSpeedMultiplier = 2.0f;
        private bool _isAssaultBoosting = false;
        private float _assaultBoostCurrentMultiplier = 1.0f; // 현재 속도 배율

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
                    // AC6 스타일: 대시 진행도에 따른 가변 속도 적용
                    float dashProgress = 1f - (_dashRemainingDistance / Mathf.Max(0.01f, _dashDistance));
                    float curveValue = _dashSpeedCurve.Evaluate(dashProgress);
                    float currentDashSpeed = _dashSpeed * _dashPeakSpeedMultiplier * curveValue;
                    float step = Mathf.Min(currentDashSpeed * dt, _dashRemainingDistance);
                    
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

            // Assault Boost energy drain
            if (_isAssaultBoosting)
            {
                float drainAmount = _assaultBoostDrainPerSec * dt;
                if (_currentEnergy >= drainAmount)
                {
                    _currentEnergy -= drainAmount;
                    _energyRegenCooldown = _energyRegenDelay;
                    
                    // AC6 스타일: 점진적 가속
                    float accelRate = (_assaultBoostMaxSpeedMultiplier - 1f) / Mathf.Max(0.01f, _assaultBoostAccelTime);
                    _assaultBoostCurrentMultiplier = Mathf.Min(
                        _assaultBoostMaxSpeedMultiplier,
                        _assaultBoostCurrentMultiplier + accelRate * dt
                    );
                }
                else
                {
                    _isAssaultBoosting = false;
                }
            }
            else
            {
                // AC6 스타일: 급격한 감속
                if (_assaultBoostCurrentMultiplier > 1.0f)
                {
                    float decelRate = (_assaultBoostMaxSpeedMultiplier - 1f) / Mathf.Max(0.01f, _assaultBoostDecelTime);
                    _assaultBoostCurrentMultiplier = Mathf.Max(
                        1.0f,
                        _assaultBoostCurrentMultiplier - decelRate * dt
                    );
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

            // StartLag system update
            UpdateStartLagSystem(dt);

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
            if (_isInStartLag) return false; // StartLag 상태에서는 이동 불가
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
            if (_isInStartLag) return false; // StartLag 상태에서는 이동 불가
            if (_isDashing) return true; // 대시 중에는 독립 이동 우선
            Vector3 dir = direction; dir.z = 0f;
            if (dir.sqrMagnitude <= 0f) return false;
            dir.Normalize();
			if (rotateToMovement) AimTowards(dir);
            Vector3 selfPos = transform.position; selfPos.z = 0f;
            float targetSpeed = Mathf.Max(0f, _moveSpeed);
            
            // AC6 스타일: 현재 가속 단계 적용
            if (_isAssaultBoosting || _assaultBoostCurrentMultiplier > 1.0f)
            {
                targetSpeed *= _assaultBoostCurrentMultiplier;
            }
            
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
            if (_isInStartLag) return; // StartLag 상태에서는 회전 불가
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
            return !_isStaggered && !_isInStartLag && !_isDashing && _dashCooldownTimer <= 0f && _currentEnergy >= _dashEnergyCost;
        }

        public bool TryDash(Vector3 direction)
        {
            Vector3 dir = direction; dir.z = 0f;
            if (dir.sqrMagnitude <= 0f) return false;
            if (!CanDash()) return false;
            dir.Normalize();

            if (!SpendEnergy(_dashEnergyCost)) return false;

            // Stop assault boost when dashing
            if (_isAssaultBoosting)
            {
                _isAssaultBoosting = false;
            }

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

        #region Assault Boost API
        public bool CanAssaultBoost()
        {
            return !_isStaggered && !_isInStartLag && !_isDashing && _currentEnergy >= _assaultBoostActivationCost;
        }

        public bool CanAssaultBoostWithSustain(float minDuration = 1.0f)
        {
            if(_currentEnergy >= _maxEnergy) return true;
            float requiredEnergy = _assaultBoostActivationCost + (_assaultBoostDrainPerSec * minDuration);
            return !_isStaggered && !_isInStartLag && !_isDashing && _currentEnergy >= requiredEnergy;
        }

        public float CalculateRequiredAssaultBoostDuration(float distance)
        {
            float assaultBoostSpeed = _moveSpeed * _assaultBoostMaxSpeedMultiplier;
            return distance / assaultBoostSpeed;
        }

        public bool TryStartAssaultBoost()
        {
            if (!CanAssaultBoost()) return false;
            if (!SpendEnergy(_assaultBoostActivationCost)) return false;
            _isAssaultBoosting = true;
            // 가속 시작점을 현재 배율로 설정 (부드러운 전환)
            if (_assaultBoostCurrentMultiplier < 1.0f) 
                _assaultBoostCurrentMultiplier = 1.0f;
            return true;
        }

        public void StopAssaultBoost()
        {
            _isAssaultBoosting = false;
        }
        #endregion
        
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
            
            // StartLag 해제
            _isInStartLag = false;
            _startLagTimer = 0f;
            
            // Stop assault boost when staggered
            if (_isAssaultBoosting)
            {
                _isAssaultBoosting = false;
            }
            
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

        #region StartLag System
        public void StartStartLag(float duration)
        {
            if (duration <= 0f) return;
            _isInStartLag = true;
            _startLagTimer = Mathf.Max(_startLagTimer, duration); // 기존 타이머보다 긴 경우만 갱신
        }

        private void UpdateStartLagSystem(float deltaTime)
        {
            if (_isInStartLag)
            {
                _startLagTimer -= deltaTime;
                if (_startLagTimer <= 0f)
                {
                    _isInStartLag = false;
                    _startLagTimer = 0f;
                }
            }
        }
        #endregion
    }
}
