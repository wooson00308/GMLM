using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor.Animations;

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
        [SerializeField, MinValue(0)] private int _impactValue = 6; // AC6 style stagger impact
        [SerializeField, Tooltip("사거리 유지 대상 무기 (주력 무기는 true, 보조 화력은 false)")] private bool _isRangeKeeping = true;
        [SerializeField] private bool _isRotateToTarget = false;
        [SerializeField] private AnimatorOverrideController _animatorOverrideController;

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
		[ShowIf("IsRanged")]
		[SerializeField, Tooltip("호밍 탄환에도 방사 적용")] private bool _applySpreadToHoming = false;

        [Header("Pre-Delay")]
        [SerializeField, Tooltip("공격 전 선딜레이 (초)"), MinValue(0f)] private float _preDelay = 0f;

        [Header("Burst Fire")]
        [SerializeField, Tooltip("연사(버스트) 모드 활성화")] private bool _burstMode = false;
		[ShowIf("IsBurstEnabled")]
		[SerializeField, Tooltip("한 번 발사 시 나가는 탄환 수"), MinValue(1)] private int _burstCount = 3;
		[ShowIf("IsBurstEnabled")]
		[SerializeField, Tooltip("버스트 내 발사 간격 (초)"), MinValue(0.01f)] private float _burstInterval = 0.1f;
		[ShowIf("IsBurstEnabled")]
		[SerializeField, Tooltip("버스트 완료 후 다음 버스트까지 추가 대기시간 (초)"), MinValue(0f)] private float _burstCooldown = 0.5f;

        // Optional legacy-like fields (not used directly by logic yet)
        public WeaponType Type { get { return _type; } }
        public int Damage { get; private set; }
        public int Range { get { return Mathf.RoundToInt(_range); } }
        public int FireRate { get; private set; }
        public int Accuracy { get; private set; }
        
        // Reload system fields
        [SerializeField] private int _magazineSize = 30;
        [SerializeField] private float _reloadTime = 2.0f;
        private int _currentAmmo;
        private bool _isReloading = false;
        private float _reloadTimer = 0f;

        private float _cooldownTimer;
        private float _preDelayTimer = 0f;
        private bool _isPreDelayActive = false;
        private Mecha _preDelaySelf;
        private Mecha _preDelayTarget;
        
        // Burst fire state management
        private bool _isBurstInProgress = false;
        private int _currentBurstShot = 0;
        private float _burstTimer = 0f;
        private Mecha _burstSelf;
        private Mecha _burstTarget;
        private MechaAnimation _mechaAnimation;
        // Odin helper methods
        private bool IsRanged => _type == WeaponType.Ranged;
        private bool IsBurstEnabled => _burstMode && IsRanged;

        public bool IsRotateToTarget => _isRotateToTarget;
        public bool IsRangeKeeping => _isRangeKeeping;
        public float AttackRange => _range;
        public float RemainingCooldown => Mathf.Max(0f, _cooldownTimer);
        public int ImpactValue => _impactValue;

		// Exposed descriptors for aiming logic (read-only)
		public float ProjectileSpeed => _projectilePrefab != null ? _projectilePrefab.Speed : 0f;
		public bool IsProjectileHoming => _projectilePrefab != null && _projectilePrefab.IsHoming;
		
		// Burst mode properties (read-only)
		public bool IsBurstModeEnabled => _burstMode;
		public bool IsBurstInProgress => _isBurstInProgress;
		public int BurstCount => _burstCount;
		public float BurstInterval => _burstInterval;
		
		// Reload system properties (read-only)
		public int MagazineSize => _magazineSize;
		public float ReloadTime => _reloadTime;
		public int CurrentAmmo => _currentAmmo;
		public bool IsReloading => _isReloading;
		public bool CanFire => !_isReloading && _currentAmmo > 0 && _cooldownTimer <= 0f && !_isBurstInProgress && !_isPreDelayActive;

        private void Awake() {
            _cooldownTimer = 0f;
            _currentAmmo = _magazineSize; // 초기 탄약 수 설정
            _mechaAnimation = GetComponentInParent<MechaAnimation>();
        }

		private void Update()
		{
			if (_cooldownTimer > 0f)
			{
				_cooldownTimer -= Time.deltaTime;
			}
			
			// Reload system update
			if (_isReloading)
			{
				_reloadTimer -= Time.deltaTime;
				if (_reloadTimer <= 0f)
				{
					_currentAmmo = _magazineSize;
					_isReloading = false;
				}
			}
			
			// 선딜레이 처리
			if (_isPreDelayActive)
			{
				HandlePreDelay();
			}
			
			// 버스트 모드 자동 진행 처리
			if (_isBurstInProgress)
			{
				HandleBurstAutoProgress();
			}
		}
		
		private void HandlePreDelay()
		{
			if (_preDelayTimer > 0f)
			{
				_preDelayTimer -= Time.deltaTime;
				return;
			}
			
			// 선딜레이 완료 - 타겟과 발사자 유효성만 재검증 (사거리는 체크하지 않음)
			if (_preDelaySelf == null || _preDelayTarget == null || !_preDelayTarget.IsAlive || 
				_preDelaySelf.TeamId == _preDelayTarget.TeamId)
			{
				CancelPreDelay();
				return;
			}
			
			// 선딜레이 완료 후 실제 공격 실행 (사거리 무시)
			var self = _preDelaySelf;
			var target = _preDelayTarget;
			CancelPreDelay();
			
			// 버스트 모드와 단발 모드 구분 처리
			if (IsBurstEnabled)
			{
				StartBurstFire(self, target);
			}
			else
			{
				FireSingleShot(self, target);
			}
		}
		
		private void CancelPreDelay()
		{
			_isPreDelayActive = false;
			_preDelayTimer = 0f;
			_preDelaySelf = null;
			_preDelayTarget = null;
		}
		
		private void HandleBurstAutoProgress()
		{
			if (_burstTimer > 0f)
			{
				_burstTimer -= Time.deltaTime;
				return;
			}
			
			// 다음 발사할 탄이 있는지 확인
			if (_currentBurstShot < _burstCount)
			{
				// 탄약 체크 - 버스트 중에도 탄약 소진 시 재장전 필요
				if (_currentAmmo <= 0)
				{
					CompleteBurst();
					StartReload();
					return;
				}
				
				// 타겟과 발사자 유효성 검증 (사거리 체크 제거 - 버스트는 완료까지 진행)
				if (_burstSelf == null || _burstTarget == null || !_burstTarget.IsAlive || 
					_burstSelf.TeamId == _burstTarget.TeamId)
				{
					CompleteBurst();
					return;
				}
				
				// 다음 발사 실행
				FireProjectile(_burstSelf, _burstTarget);
				_currentAmmo--; // 탄약 소모
				_currentBurstShot++;
				
				// 다음 발사까지의 간격 설정 또는 버스트 완료
				if (_currentBurstShot < _burstCount)
				{
					_burstTimer = _burstInterval;
				}
				else
				{
					CompleteBurst();
				}
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
            if (self.IsStaggered) return false; // 스태거 상태에서는 공격 불가
            if (!CanFire) return false; // 재장전 중이거나 탄약이 없으면 공격 불가
            if (self.TeamId == target.TeamId) return false;

            float distance = Vector3.Distance(self.transform.position, target.transform.position);
            if (distance > _range) return false;

            if (_mechaAnimation != null && _animatorOverrideController != null)
            {
                _mechaAnimation.SetAnimator(_animatorOverrideController);
                _mechaAnimation.PlayAnimation();
            }

            // 선딜레이가 설정되어 있으면 선딜레이 시작
            if (_preDelay > 0f)
            {
                StartPreDelay(self, target);
                return true;
            }

            // 선딜레이 없이 즉시 공격 실행
            // 버스트 모드와 단발 모드 구분 처리
            if (IsBurstEnabled)
            {
                // 버스트 발사 시작
                StartBurstFire(self, target);
                return true;
            }
            else
            {
                // 단발 발사
                return FireSingleShot(self, target);
            }
        }
        
        private void StartPreDelay(Mecha self, Mecha target)
        {
            _isPreDelayActive = true;
            _preDelayTimer = _preDelay;
            _preDelaySelf = self;
            _preDelayTarget = target;
        }
        
        private void StartBurstFire(Mecha self, Mecha target)
        {
            _isBurstInProgress = true;
            _currentBurstShot = 0;
            _burstTimer = 0f;
            _burstSelf = self;
            _burstTarget = target;
            
            // 첫 번째 발사 즉시 실행
            FireProjectile(self, target);
            _currentAmmo--; // 탄약 소모
            _currentBurstShot++;
            
            // 다음 발사까지의 간격 설정
            if (_currentBurstShot < _burstCount)
            {
                _burstTimer = _burstInterval;
            }
            else
            {
                // 버스트 완료 (단발 버스트인 경우)
                CompleteBurst();
            }
        }
        
        private void CompleteBurst()
        {
            _isBurstInProgress = false;
            _currentBurstShot = 0;
            _burstTimer = 0f;
            _burstSelf = null;
            _burstTarget = null;
            
            // 탄약 소진 시 재장전 타이머 우선, 아니면 버스트 쿨다운
            if (_currentAmmo <= 0)
            {
                StartReload();
                _cooldownTimer = _reloadTime;
            }
            else
            {
                // 버스트 완료 후 쿨다운 설정 (기본 공격속도 + 버스트 쿨다운)
                float atkSpd = Mathf.Max(0.01f, _attackSpeed);
                _cooldownTimer = (1f / atkSpd) + _burstCooldown;
            }
        }
        
        private bool FireSingleShot(Mecha self, Mecha target)
        {
            FireProjectile(self, target);
            
            // 탄약 소모
            _currentAmmo--;
            
            // 탄약 소진 시 자동 재장전 및 재장전 타임으로 쿨다운 설정
            if (_currentAmmo <= 0)
            {
                StartReload();
                _cooldownTimer = _reloadTime;
            }
            else
            {
                // 단발 쿨다운 설정
                float atkSpd = Mathf.Max(0.01f, _attackSpeed);
                _cooldownTimer = 1f / atkSpd;
            }
            
            return true;
        }
        
        public void StartReload()
        {
            if (!_isReloading)
            {
                _isReloading = true;
                _reloadTimer = _reloadTime;
            }
        }
        
        private void FireProjectile(Mecha self, Mecha target)
        {
            int damage = Mathf.Max(0, _attackPower);
            int impact = Mathf.Max(0, _impactValue);
            
            // 발사 이펙트
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
                target.TakeDamage(damage, impact);
            }
            else
            {
                if (_projectilePrefab == null)
                {
                    // Fallback to hitscan if no projectile prefab assigned
                    target.TakeDamage(damage, impact);
                }
                else
                {
                    // Fire strictly along current muzzle orientation. Predictive aiming is handled upstream (AttackAction)
                    var spawnPos = (_muzzle != null ? _muzzle.position : self.transform.position);
                    var spawnRot = (_muzzle != null ? _muzzle.rotation : self.transform.rotation);
                    var proj = Instantiate(_projectilePrefab, spawnPos, spawnRot);
                    if (proj.IsHoming && !_applySpreadToHoming)
                    {
                        proj.Initialize(target.transform, self.TeamId, damage, impact);
                    }
                    else
                    {
                        // derive direction from current muzzle, then apply fan-shaped spread
                        Vector3 dir = spawnRot * Vector3.up; dir.z = 0f;
                        if (dir.sqrMagnitude <= 1e-6f) dir = Vector3.right;
                        dir.Normalize();
                        
                        // 부채꼴 패턴: 중앙 기준으로 균등 분산
                        float baseAngle = Random.Range(-_spreadDeg * 0.5f, _spreadDeg * 0.5f);
                        float fineOffset = Random.Range(-_spreadDeg * 0.1f, _spreadDeg * 0.1f); // 미세 조정
                        float yaw = baseAngle + fineOffset;
                        
                        Vector3 spreadDir = (Quaternion.Euler(0f, 0f, yaw) * dir).normalized;
                        
                        if (proj.IsHoming && _applySpreadToHoming)
                        {
                            // 호밍 탄환, 방사 적용: 방사로 뿌리되 타겟 정보는 전달
                            proj.InitializeWithDirectionAndTarget(new Vector2(spreadDir.x, spreadDir.y), target.transform, self.TeamId, damage, impact);
                        }
                        else
                        {
                            // 비호밍 탄환: 방사로 발사
                            proj.InitializeWithDirection(new Vector2(spreadDir.x, spreadDir.y), self.TeamId, damage, impact);
                        }
                    }
                }
            }
        }
    }
}