using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using GMLM.Data;

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
        [SerializeField] private MechaStats _baseStats;
        private MechaStats _currentStats;
        private int _currentHp;
        private float _currentEnergy;

        public int TeamId => _teamId;
        public int MaxHp => _currentStats.maxHp;
        public int CurrentHp => _currentHp;
        public bool IsAlive => _currentHp > 0;
        public bool IsDead => !IsAlive;
        public float MoveSpeed => _currentStats.moveSpeed;
        public float Acceleration => _currentStats.acceleration;
        public float Deceleration => _currentStats.deceleration;
        public float TurnRateDeg => _currentStats.turnRateDeg;
        public float MaxEnergy => _currentStats.maxEnergy;
        public float CurrentEnergy => _currentEnergy;
        public float EnergyRegenPerSec => _currentStats.energyRegenPerSec;
        public float EnergyRegenDelay => _currentStats.energyRegenDelay;
        public IReadOnlyList<Weapon> WeaponsAll => _weaponsAll;
        public Pilot Pilot => _pilot;
        // Dash parameters for AI calculations
        public float DashDistance => _currentStats.dashDistance;
        public float DashSpeed => _currentStats.dashSpeed;
        public float DashCooldown => _currentStats.dashCooldown;
        public float DashEnergyCost => _currentStats.dashEnergyCost;
        public bool IsDashing => _isDashing;
        
        // Assault Boost parameters
        public float AssaultBoostSpeedMultiplier => _currentStats.assaultBoostSpeedMultiplier;
        public bool IsAssaultBoosting => _isAssaultBoosting;
        
        // Stagger system properties
        public int MaxStagger => _currentStats.maxStagger;
        public float CurrentStagger => _currentStagger;
        public bool IsStaggered => _isStaggered;
        public float StaggerProgress => _currentStagger / _currentStats.maxStagger;
        public float StaggerRecoveryCooldown => _staggerRecoveryCooldown;
        public float StaggerDuration => _currentStats.staggerDuration;
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
        private float _currentStagger = 0f;
        private bool _isStaggered = false;
        private float _staggerTimer = 0f;
        private float _staggerRecoveryCooldown = 0f; // 회복 지연 타이머

        // StartLag system state
        private bool _isInStartLag = false;
        private float _startLagTimer = 0f;

        private MechaModel _mechaModel;
        private readonly Dictionary<PartType, IPartData> _equippedParts = new Dictionary<PartType, IPartData>();

        private float _dashCooldownTimer = 0f;
        private bool _isDashing = false;
        private Vector3 _dashDir = Vector3.zero; // XY
        private float _dashRemainingDistance = 0f;

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
            if (_mechaModel == null)
            {
                _mechaModel = GetComponentInChildren<MechaModel>(true);
            }
            
            // 스탯 초기화
            RecalculateStats();
            _currentHp = _currentStats.maxHp;
            _currentEnergy = _currentStats.maxEnergy;
            
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
                    float dashProgress = 1f - (_dashRemainingDistance / Mathf.Max(0.01f, _currentStats.dashDistance));
                    float curveValue = _currentStats.dashSpeedCurve.Evaluate(dashProgress);
                    float currentDashSpeed = _currentStats.dashSpeed * _currentStats.dashPeakSpeedMultiplier * curveValue;
                    float step = Mathf.Min(currentDashSpeed * dt, _dashRemainingDistance);
                    
                    Vector3 selfPos = transform.position; selfPos.z = 0f;
                    Vector3 next = selfPos + _dashDir * step;
                    next.z = transform.position.z;
                    transform.position = next;
                    _dashRemainingDistance -= step;
					if (_mechaModel != null)
					{
						_mechaModel.UpdateDashThrusters();
					}
                }
                else
                {
                    _isDashing = false;
					if (_mechaModel != null)
					{
						_mechaModel.StopDashFx();
					}
					
					// 예약 시스템 제거: 대시 완료 후 자연스럽게 AttackAction에서 공격 처리
                }
            }

            // Assault Boost energy drain
            if (_isAssaultBoosting)
            {
                float drainAmount = _currentStats.assaultBoostDrainPerSec * dt;
                if (_currentEnergy >= drainAmount)
                {
                    _currentEnergy -= drainAmount;
                    _energyRegenCooldown = _currentStats.energyRegenDelay;
                    
                    // AC6 스타일: 점진적 가속
                    float accelRate = (_currentStats.assaultBoostMaxSpeedMultiplier - 1f) / Mathf.Max(0.01f, _currentStats.assaultBoostAccelTime);
                    _assaultBoostCurrentMultiplier = Mathf.Min(
                        _currentStats.assaultBoostMaxSpeedMultiplier,
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
                    float decelRate = (_currentStats.assaultBoostMaxSpeedMultiplier - 1f) / Mathf.Max(0.01f, _currentStats.assaultBoostDecelTime);
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
            if (_currentEnergy < _currentStats.maxEnergy)
            {
                _currentEnergy = Mathf.Min(_currentStats.maxEnergy, _currentEnergy + _currentStats.energyRegenPerSec * dt);
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
        
        public void AddPart(IPartData part)
        {
            if (part == null) return;
            
            // MechaModel 초기화
            if (_mechaModel == null)
            {
                _mechaModel = GetComponentInChildren<MechaModel>(true);
                if (_mechaModel == null)
                {
                    Debug.LogError("MechaModel not found!");
                    return;
                }
            }
            
            PartType type = part.PartType;
            
            // 기존 파츠가 있으면 먼저 탈착
            if (_equippedParts.TryGetValue(type, out var existing))
            {
                RemovePart(existing);
            }
            
            // 새 파츠 장착
            part.AttachPrefab(_mechaModel);
            _equippedParts[type] = part;
            
            // 스탯 재계산
            RecalculateStats();
        }

        public void RemovePart(IPartData part)
        {
            if (part == null) return;
            
            PartType type = part.PartType;
            
            // Dictionary에서 파츠 제거
            if (_equippedParts.ContainsKey(type) && _equippedParts[type] == part)
            {
                _equippedParts.Remove(type);
            }
            
            part.DetachPrefab(_mechaModel);
            
            // 스탯 재계산
            RecalculateStats();
        }
        
        private void RecalculateStats()
        {
            // 베이스 스탯에서 시작
            _currentStats = _baseStats.Clone();
            
            // 모든 장착된 파츠 순회하며 스탯 적용
            foreach (var part in _equippedParts.Values)
            {
                ApplyPartStats(_currentStats, part.Stats);
            }
        }
        
        private void ApplyPartStats(MechaStats target, MechaStats partStats)
        {
            // 가산 그룹: HP, Energy 계열 (절대값 더하기)
            target.maxHp += partStats.maxHp;
            target.maxEnergy += partStats.maxEnergy;
            target.maxStagger += partStats.maxStagger;
            
            // 승산 그룹: 속도, 가감속 계열 (배율로 처리)
            target.moveSpeed *= (1f + partStats.moveSpeed);
            target.acceleration *= (1f + partStats.acceleration);
            target.deceleration *= (1f + partStats.deceleration);
            target.turnRateDeg *= (1f + partStats.turnRateDeg);
            target.energyRegenPerSec *= (1f + partStats.energyRegenPerSec);
            target.staggerDecayRate *= (1f + partStats.staggerDecayRate);
            target.dashSpeed *= (1f + partStats.dashSpeed);
            target.dashPeakSpeedMultiplier *= (1f + partStats.dashPeakSpeedMultiplier);
            target.assaultBoostSpeedMultiplier *= (1f + partStats.assaultBoostSpeedMultiplier);
            target.assaultBoostMaxSpeedMultiplier *= (1f + partStats.assaultBoostMaxSpeedMultiplier);
            
            // 최소값 그룹: 지연시간 계열 (더 빠른 쪽 채택)
            target.energyRegenDelay = Mathf.Min(target.energyRegenDelay, partStats.energyRegenDelay);
            target.staggerRecoveryDelay = Mathf.Min(target.staggerRecoveryDelay, partStats.staggerRecoveryDelay);
            target.dashCooldown = Mathf.Min(target.dashCooldown, partStats.dashCooldown);
            target.assaultBoostActivationCost = Mathf.Min(target.assaultBoostActivationCost, partStats.assaultBoostActivationCost);
            target.assaultBoostDrainPerSec = Mathf.Min(target.assaultBoostDrainPerSec, partStats.assaultBoostDrainPerSec);
            target.assaultBoostAccelTime = Mathf.Min(target.assaultBoostAccelTime, partStats.assaultBoostAccelTime);
            target.assaultBoostDecelTime = Mathf.Min(target.assaultBoostDecelTime, partStats.assaultBoostDecelTime);
            
            // 덧셈 그룹: 거리, 비용, 지속시간
            target.dashDistance += partStats.dashDistance;
            target.dashEnergyCost += partStats.dashEnergyCost;
            target.staggerDuration += partStats.staggerDuration;
            
            // 곱셈 그룹: 데미지 배율
            target.staggerDamageMultiplier *= (1f + partStats.staggerDamageMultiplier);
        }

        public void RefreshWeapons()
        {
            _weaponsAll.Clear();
            GetComponentsInChildren(true, _weaponsAll);
			// Ensure MechaAnimation updates internal weapon presence caches (hands/external)
			if (_mechaModel == null)
			{
				_mechaModel = GetComponentInChildren<MechaModel>(true);
			}
			if (_mechaModel != null)
			{
				_mechaModel.RefreshWeaponPresence();
			}
        }
        
        public void TakeDamage(int damage, int impact)
        {
            if (!IsAlive) return;
            
            // Apply stagger impact
            if (impact > 0)
            {
                _currentStagger += impact;
                
                // Check for stagger threshold
                if (_currentStagger >= _currentStats.maxStagger && !_isStaggered)
                {
                    EnterStagger();
                }
            }
            
            // 스태거 회복 지연: 스태거 중이 아니고 게이지가 남아있다면 어떤 피해든 지연 타이머 리셋
            if (!_isStaggered && _currentStagger > 0f)
            {
                _staggerRecoveryCooldown = _currentStats.staggerRecoveryDelay;
            }
            
            // Apply damage with stagger multiplier
            int finalDamage = damage;
            if (_isStaggered)
            {
                finalDamage = Mathf.RoundToInt(damage * _currentStats.staggerDamageMultiplier);
            }
            
            _currentHp = Mathf.Clamp(_currentHp - Mathf.Max(0, finalDamage), 0, _currentStats.maxHp);
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
            float targetSpeed = Mathf.Max(0f, _currentStats.moveSpeed);
            float rate = (_currentSpeed < targetSpeed) ? _currentStats.acceleration : _currentStats.deceleration;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.deltaTime);
            Vector3 next = selfPos + dir * _currentSpeed * Time.deltaTime;
            next.z = transform.position.z;
            transform.position = next;
			if (_mechaModel != null)
			{
				_mechaModel.UpdateMoveThrusters(new Vector2(dir.x, dir.y) * _currentSpeed, _isAssaultBoosting);
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
            float targetSpeed = Mathf.Max(0f, _currentStats.moveSpeed);
            
            // AC6 스타일: 현재 가속 단계 적용
            if (_isAssaultBoosting || _assaultBoostCurrentMultiplier > 1.0f)
            {
                targetSpeed *= _assaultBoostCurrentMultiplier;
            }
            
            float rate = (_currentSpeed < targetSpeed) ? _currentStats.acceleration : _currentStats.deceleration;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.deltaTime);
            Vector3 next = selfPos + dir * _currentSpeed * Time.deltaTime;
            next.z = transform.position.z;
            transform.position = next;
			if (_mechaModel != null)
			{
				_mechaModel.UpdateMoveThrusters(new Vector2(dir.x, dir.y) * _currentSpeed, _isAssaultBoosting);
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
			if (_mechaModel != null)
			{
				_mechaModel.UpdateAiming(to);
				if (_mechaModel.EvaluateBodyAssist(to, out float residualYawDeg, out int sign))
				{
                // 2) 보조 회전: 잔여 각도만큼, 동체 회전 속도 제한
                float stepDeg = Mathf.Min(residualYawDeg, Mathf.Max(0f, _currentStats.turnRateDeg) * Time.deltaTime);
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
                float maxRad = Mathf.Deg2Rad * Mathf.Max(0f, _currentStats.turnRateDeg) * Time.deltaTime;
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
            _energyRegenCooldown = _currentStats.energyRegenDelay;
            return true;
        }

        public void AddEnergy(float amount)
        {
            _currentEnergy = Mathf.Clamp(_currentEnergy + Mathf.Max(0f, amount), 0f, _currentStats.maxEnergy);
        }

        public bool CanDash()
        {
            return !_isStaggered && !_isInStartLag && !_isDashing && _dashCooldownTimer <= 0f && _currentEnergy >= _currentStats.dashEnergyCost;
        }

        public bool TryDash(Vector3 direction)
        {
            Vector3 dir = direction; dir.z = 0f;
            if (dir.sqrMagnitude <= 0f) return false;
            if (!CanDash()) return false;
            dir.Normalize();

            if (!SpendEnergy(_currentStats.dashEnergyCost)) return false;

            // Stop assault boost when dashing
            if (_isAssaultBoosting)
            {
                _isAssaultBoosting = false;
            }

            _isDashing = true;
            _dashDir = dir;
            _dashRemainingDistance = Mathf.Max(0f, _currentStats.dashDistance);
            _dashCooldownTimer = _currentStats.dashCooldown;
			if (_mechaModel != null)
			{
                float dashDuration = (_currentStats.dashSpeed > 0f) ? (_currentStats.dashDistance / _currentStats.dashSpeed) : 0.15f;
				_mechaModel.PlayDashFx(new Vector2(dir.x, dir.y), dashDuration);
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
            return !_isStaggered && !_isInStartLag && !_isDashing && _currentEnergy >= _currentStats.assaultBoostActivationCost;
        }

        public bool CanAssaultBoostWithSustain(float minDuration = 1.0f)
        {
            if(_currentEnergy >= _currentStats.maxEnergy) return true;
            float requiredEnergy = _currentStats.assaultBoostActivationCost + (_currentStats.assaultBoostDrainPerSec * minDuration);
            return !_isStaggered && !_isInStartLag && !_isDashing && _currentEnergy >= requiredEnergy;
        }

        public float CalculateRequiredAssaultBoostDuration(float distance)
        {
            float assaultBoostSpeed = _currentStats.moveSpeed * _currentStats.assaultBoostMaxSpeedMultiplier;
            return distance / assaultBoostSpeed;
        }

        public bool TryStartAssaultBoost()
        {
            if (!CanAssaultBoost()) return false;
            if (!SpendEnergy(_currentStats.assaultBoostActivationCost)) return false;
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
                    _currentStagger = Mathf.Max(0f, _currentStagger - _currentStats.staggerDecayRate * deltaTime);
                }
            }
        }
        
        private void EnterStagger()
        {
            if (_isStaggered) return;
            _isStaggered = true;
            _staggerTimer = _currentStats.staggerDuration;
            _currentStagger = _currentStats.maxStagger; // Cap at max when staggered
            _staggerRecoveryCooldown = 0f; // 스태거 중에는 회복 지연 의미 없음
            
            // StartLag 해제
            _isInStartLag = false;
            _startLagTimer = 0f;
            
            // Stop assault boost when staggered
            if (_isAssaultBoosting)
            {
                _isAssaultBoosting = false;
            }
            
            if (_mechaModel != null)
            {
                _mechaModel.PlayStaggerEffect();
            }
        }
        
        private void ExitStagger()
        {
            _isStaggered = false;
            _staggerTimer = 0f;
            _currentStagger = 0f; // Reset stagger gauge after stagger ends
            _staggerRecoveryCooldown = _currentStats.staggerRecoveryDelay; // 회복 시작까지 지연
            
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
