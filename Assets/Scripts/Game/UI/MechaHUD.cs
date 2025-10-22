using Michsky.MUIP;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace GMLM.Game
{
    public enum WeaponSlot
    {
        RightHand,
        LeftHand,
        LeftShoulder,
        RightShoulder
    }

    /// <summary>
    /// 메카 인게임 HUD UI 클래스
    /// </summary>
    public class MechaHUD : MonoBehaviour
    {
        [SerializeField] private Mecha _mecha;
        [SerializeField] private TextMeshProUGUI _nameText; // mecha name text
        [SerializeField] private ProgressBar _apBar; // armor point bar
        [SerializeField] private ProgressBar _enBar; // energy bar
        [SerializeField] private Image _staggerBar; // stagger bar

        [ShowIf("_isFollowTarget")]
        [SerializeField] private Image _apBarBackground;
        [ShowIf("_isFollowTarget")]
        [SerializeField] private Image _apBarFill;
        [ShowIf("_isFollowTarget")]
        [SerializeField] private Image _enBarBackground;
        [ShowIf("_isFollowTarget")]
        [SerializeField] private Image _enBarFill;

        //여기 밑에는 무기들 잔탄 수 표시 (오른손, 왼손, 왼쪽 어깨, 오른쪽 어깨) 백그라운드 + 필 조합
        [ShowIf("_isFollowTarget")]
        [SerializeField] private Image _rightHandAmmoBackground;
        [ShowIf("_isFollowTarget")]
        [SerializeField] private Image _rightHandAmmoFill;
        [ShowIf("_isFollowTarget")]
        [SerializeField] private Image _leftHandAmmoBackground;
        [ShowIf("_isFollowTarget")]
        [SerializeField] private Image _leftHandAmmoFill;

        [ShowIf("_isFollowTarget")]
        [SerializeField] private Image _leftShoulderAmmoBackground;
        [ShowIf("_isFollowTarget")]
        [SerializeField] private Image _leftShoulderAmmoFill;
        [ShowIf("_isFollowTarget")]
        [SerializeField] private Image _rightShoulderAmmoBackground;
        [ShowIf("_isFollowTarget")]
        [SerializeField] private Image _rightShoulderAmmoFill;

        [SerializeField] private bool _isFollowTarget = false;
        [SerializeField] private Vector2 _offset = new Vector2(0, 0);
        [SerializeField] private Camera _camera; // null이면 Camera.main 사용
        [SerializeField] private float _staggerFlashSpeed = 10.0f; // 깜빡임 속도
        [SerializeField] private Color _hpDangerColor = Color.red; // HP 위험 색상
        [SerializeField] private Color _energyLowColor = Color.red; // 에너지 부족 색상
        [SerializeField] private float _hpDangerThreshold = 0.3f; // HP 위험 임계값 (30%)
        [SerializeField] private float _energyLowThreshold = 0.2f; // 에너지 부족 임계값 (20%)

        private RectTransform _rectTransform;
        private float _lastStaggerValue = 0f;
        private bool _isStaggerFlashing = false;
        private bool _prevIsStaggered = false;
        private Tween _staggerTween;
        private Color _originalHpColor;
        private Color _originalEnergyColor;
        [SerializeField] private float _hpPulseDuration = 0.25f; // AP 감소 시 단발성 펄스 시간
        private Tween _hpPulseTween;
        private int _lastHpValue = -1;
        [SerializeField] private Color _energyPulseColor = Color.cyan; // 에너지 사용 시 펄스 색상
        [SerializeField] private float _energyPulseDuration = 0.25f; // 에너지 사용 시 단발성 펄스 시간
        private Tween _energyPulseTween;
        private float _lastEnergyValue = -1f;

        // Background fillAmount 초기값 캐싱 (프리팹 설정값이 최대값)
        private float _apBarBackgroundMaxFill;
        private float _enBarBackgroundMaxFill;
        private float _rightHandAmmoBackgroundMaxFill;
        private float _leftHandAmmoBackgroundMaxFill;
        private float _leftShoulderAmmoBackgroundMaxFill;
        private float _rightShoulderAmmoBackgroundMaxFill;

        // AP/EN Fill 색상 연출용
        private Color _originalApFillColor;
        private Color _originalEnFillColor;
        private Tween _apFillPulseTween;
        private Tween _enFillPulseTween;

        // 무기 잔탄 장전 효과용
        [SerializeField] private Color _reloadingColor = Color.red; // 장전 중 색상
        [SerializeField] private Color _reloadedColor = Color.white; // 장전 완료 색상
        [SerializeField] private float _reloadPulseSpeed = 2.0f; // 장전 중 펄스 속도
        private Dictionary<WeaponSlot, Tween> _ammoPulseTweens = new Dictionary<WeaponSlot, Tween>();
        private Dictionary<WeaponSlot, bool> _wasReloading = new Dictionary<WeaponSlot, bool>();

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            if(_mecha != null) {
                Initialize(_mecha);
            }
        }

        public void Initialize(Mecha mecha)
        {
            _mecha = mecha;
            _nameText.text = mecha.Pilot.Name;
            _lastStaggerValue = _mecha.CurrentStagger;
            _prevIsStaggered = _mecha.IsStaggered;
            
            // 원본 색상 저장
            if (_apBar.loadingBar != null) {
                _originalHpColor = _apBar.loadingBar.color;
            }
            if (_enBar.loadingBar != null) {
                _originalEnergyColor = _enBar.loadingBar.color;
            }
            _lastHpValue = _mecha.CurrentHp;
            _lastEnergyValue = _mecha.CurrentEnergy;
            
            // Background fillAmount 초기값 캐싱 (프리팹 설정값이 최대값)
            if (_apBarBackground != null) _apBarBackgroundMaxFill = _apBarBackground.fillAmount;
            if (_enBarBackground != null) _enBarBackgroundMaxFill = _enBarBackground.fillAmount;
            if (_rightHandAmmoBackground != null) _rightHandAmmoBackgroundMaxFill = _rightHandAmmoBackground.fillAmount;
            if (_leftHandAmmoBackground != null) _leftHandAmmoBackgroundMaxFill = _leftHandAmmoBackground.fillAmount;
            if (_leftShoulderAmmoBackground != null) _leftShoulderAmmoBackgroundMaxFill = _leftShoulderAmmoBackground.fillAmount;
            if (_rightShoulderAmmoBackground != null) _rightShoulderAmmoBackgroundMaxFill = _rightShoulderAmmoBackground.fillAmount;

            // Fill 색상 초기값 캐싱
            if (_apBarFill != null) _originalApFillColor = _apBarFill.color;
            if (_enBarFill != null) _originalEnFillColor = _enBarFill.color;
            
            UpdateUI();
        }

        private void UpdateUI() {
            _apBar.currentPercent = (float)_mecha.CurrentHp / _mecha.MaxHp * 100f;
            _enBar.currentPercent = _mecha.CurrentEnergy / _mecha.MaxEnergy * 100f;

            // AP(HP) 감소 시 단발성 붉은 펄스 (Follow 모드가 아닐 때만)
            if (_apBar.loadingBar != null && !_isFollowTarget) {
                int currentHp = _mecha.CurrentHp;
                if (_lastHpValue >= 0 && currentHp < _lastHpValue) {
                    StartHpPulse();
                }
                _lastHpValue = currentHp;

                // HP 색상 변경 (펄스 중에는 트윈이 색상 제어)
                if (_hpPulseTween == null || !_hpPulseTween.IsActive()) {
                    float hpRatio = (float)_mecha.CurrentHp / _mecha.MaxHp;
                    if (hpRatio <= _hpDangerThreshold) {
                        _apBar.loadingBar.color = _hpDangerColor; // 30% 이하: 빨강 고정
                    } else {
                        _apBar.loadingBar.color = _originalHpColor; // 30% 초과: 원본색
                    }
                }
            }

            // 에너지 부족 색상 변경 (20% 이하) 및 사용 시 펄스
            if (_enBar.loadingBar != null) {
                float currentEnergy = _mecha.CurrentEnergy;
                
                // 에너지 사용 감지 및 펄스
                if (_lastEnergyValue >= 0 && currentEnergy < _lastEnergyValue) {
                    StartEnergyPulse();
                }
                _lastEnergyValue = currentEnergy;
                
                // 에너지 색상 변경 (펄스 중에는 트윈이 색상 제어)
                if (_energyPulseTween == null || !_energyPulseTween.IsActive()) {
                    float energyRatio = currentEnergy / _mecha.MaxEnergy;
                    if (energyRatio <= _energyLowThreshold) {
                        _enBar.loadingBar.color = _energyLowColor; // 20% 이하: 빨강 고정
                    } else {
                        _enBar.loadingBar.color = _originalEnergyColor; // 20% 초과: 원본색
                    }
                }
            }

            float staggerRatio = 0f;
            if (_mecha.MaxStagger > 0f) {
                staggerRatio = Mathf.Clamp01(_mecha.CurrentStagger / _mecha.MaxStagger);
            }

            _staggerBar.rectTransform.localScale = new Vector3(staggerRatio, 1, 1);

            // 스태거 '터짐' 상태 전환 감지: false -> true 시 깜빡임 시작
            bool isStaggeredNow = _mecha != null && _mecha.IsStaggered;
            if (isStaggeredNow && !_prevIsStaggered && !_isStaggerFlashing) {
                StartStaggerFlash();
            }
            // 스태거 종료 시 트윈 정리 및 복원
            if (!isStaggeredNow && _prevIsStaggered && _isStaggerFlashing) {
                if (_staggerTween != null && _staggerTween.IsActive()) {
                    _staggerTween.Kill(true);
                }
                _isStaggerFlashing = false;
                Color c = _staggerBar.color;
                _staggerBar.color = new Color(c.r, c.g, c.b, 1f);
            }
            _prevIsStaggered = isStaggeredNow;
            _lastStaggerValue = _mecha.CurrentStagger;

            // 0 -> White, 0.5 -> Yellow, 1 -> Red
            float alpha = _staggerBar.color.a;
            if (staggerRatio <= 0.5f) {
                float t = staggerRatio / 0.5f; // 0..1
                _staggerBar.color = new Color(1f, 1f, 1f - t, alpha); // White -> Yellow
            }
            else {
                float t = (staggerRatio - 0.5f) / 0.5f; // 0..1
                _staggerBar.color = new Color(1f, 1f - t, 0f, alpha); // Yellow -> Red
            }

            // Follow 모드일 때 AP/EN/무기 잔탄 UI 업데이트
            if (_isFollowTarget)
            {
                UpdateFollowModeUI();
            }
        }

        /// <summary>
        /// Follow 모드에서의 AP/EN/무기 잔탄 UI 업데이트
        /// </summary>
        private void UpdateFollowModeUI()
        {
            // AP Bar 업데이트
            if (_apBarFill != null && _apBarBackground != null)
            {
                float apRatio = (float)_mecha.CurrentHp / _mecha.MaxHp;
                _apBarFill.fillAmount = _apBarBackgroundMaxFill * apRatio;
                
                // AP 감소 시 붉은 펄스 (기존 ProgressBar와 동일한 로직)
                int currentHp = _mecha.CurrentHp;
                if (_lastHpValue >= 0 && currentHp < _lastHpValue)
                {
                    StartApFillPulse();
                }
                _lastHpValue = currentHp; // Follow 모드에서도 HP 값 업데이트
                
                // AP 색상 연출 (기존 ProgressBar와 동일한 로직)
                UpdateApFillColor(apRatio);
            }

            // EN Bar 업데이트
            if (_enBarFill != null && _enBarBackground != null)
            {
                float enRatio = _mecha.CurrentEnergy / _mecha.MaxEnergy;
                _enBarFill.fillAmount = _enBarBackgroundMaxFill * enRatio;
                
                // EN 색상 연출 (기존 ProgressBar와 동일한 로직)
                UpdateEnFillColor(enRatio);
            }

            // 무기 잔탄 업데이트
            UpdateWeaponAmmoUI(WeaponSlot.RightHand, _rightHandAmmoFill, _rightHandAmmoBackgroundMaxFill);
            UpdateWeaponAmmoUI(WeaponSlot.LeftHand, _leftHandAmmoFill, _leftHandAmmoBackgroundMaxFill);
            UpdateWeaponAmmoUI(WeaponSlot.LeftShoulder, _leftShoulderAmmoFill, _leftShoulderAmmoBackgroundMaxFill);
            UpdateWeaponAmmoUI(WeaponSlot.RightShoulder, _rightShoulderAmmoFill, _rightShoulderAmmoBackgroundMaxFill);
        }

        /// <summary>
        /// 특정 슬롯의 무기 잔탄 UI 업데이트
        /// </summary>
        private void UpdateWeaponAmmoUI(WeaponSlot slot, Image fillImage, float backgroundMaxFill)
        {
            if (fillImage == null) return;

            var weapon = GetWeaponInSlot(slot);
            bool wasReloadingBefore = _wasReloading.ContainsKey(slot) && _wasReloading[slot];
            
            if (weapon != null)
            {
                if (weapon.IsReloading)
                {
                    // 장전 중: 진행률에 따라 fillAmount 채우면서 빨간색 펄스
                    float progress = weapon.ReloadProgress;
                    fillImage.fillAmount = backgroundMaxFill * progress;
                    
                    // 펄스가 실행 중이 아니면 시작
                    if (!_ammoPulseTweens.ContainsKey(slot) || _ammoPulseTweens[slot] == null || !_ammoPulseTweens[slot].IsActive())
                    {
                        StartAmmoReloadPulse(slot, fillImage);
                    }
                    
                    _wasReloading[slot] = true;
                }
                else
                {
                    // 장전 완료 순간 감지 → 흰색 플래시
                    if (wasReloadingBefore)
                    {
                        StopAmmoReloadPulse(slot);
                        StartAmmoReloadCompleteFlash(slot, fillImage);
                    }
                    
                    // 정상 상태: 실제 잔탄 수 표시
                    float ratio = (float)weapon.CurrentAmmo / weapon.MagazineSize;
                    fillImage.fillAmount = backgroundMaxFill * ratio;
                    
                    // 플래시 중이 아니면 흰색으로 설정
                    if (!_ammoPulseTweens.ContainsKey(slot) || _ammoPulseTweens[slot] == null || !_ammoPulseTweens[slot].IsActive())
                    {
                        fillImage.color = _reloadedColor;
                    }
                    
                    _wasReloading[slot] = false;
                }
            }
            else
            {
                // 무기 없음
                StopAmmoReloadPulse(slot);
                fillImage.fillAmount = 0f;
                _wasReloading[slot] = false;
            }
        }

        /// <summary>
        /// AP Fill 색상 업데이트 (기존 ProgressBar와 동일한 로직)
        /// </summary>
        private void UpdateApFillColor(float apRatio)
        {
            if (_apFillPulseTween != null && _apFillPulseTween.IsActive()) return;

            if (apRatio <= _hpDangerThreshold)
            {
                _apBarFill.color = _hpDangerColor; // 30% 이하: 빨강 고정
            }
            else
            {
                _apBarFill.color = _originalApFillColor; // 30% 초과: 원본색
            }
        }

        /// <summary>
        /// AP Fill 피격 시 붉은 펄스 효과 (기존 ProgressBar와 동일한 로직)
        /// </summary>
        private void StartApFillPulse()
        {
            if (_apBarFill == null) return;

            // 기존 펄스가 있으면 재시작
            if (_apFillPulseTween != null && _apFillPulseTween.IsActive())
            {
                _apFillPulseTween.Kill(true);
            }

            Color startColor = _apBarFill.color;
            _apFillPulseTween = DOTween.Sequence()
                .Append(_apBarFill.DOColor(_hpDangerColor, _hpPulseDuration * 0.5f))
                .Append(_apBarFill.DOColor(startColor, _hpPulseDuration * 0.5f))
                .OnComplete(() => {
                    _apFillPulseTween = null;
                });
        }

        /// <summary>
        /// EN Fill 색상 업데이트 (기존 ProgressBar와 동일한 로직)
        /// </summary>
        private void UpdateEnFillColor(float enRatio)
        {
            if (_enFillPulseTween != null && _enFillPulseTween.IsActive()) return;

            if (enRatio <= _energyLowThreshold)
            {
                _enBarFill.color = _energyLowColor; // 20% 이하: 빨강 고정
            }
            else
            {
                _enBarFill.color = _originalEnFillColor; // 20% 초과: 원본색
            }
        }

        /// <summary>
        /// 무기 잔탄 장전 중 펄스 효과 시작
        /// </summary>
        private void StartAmmoReloadPulse(WeaponSlot slot, Image fillImage)
        {
            // 기존 펄스가 있으면 정리
            StopAmmoReloadPulse(slot);

            // 빨간색으로 시작
            fillImage.color = _reloadingColor;
            
            // 빨간색 → 거의 투명 → 빨간색으로 펄스 효과
            Color pulseColor = new Color(_reloadingColor.r, _reloadingColor.g, _reloadingColor.b, 0.1f); // 거의 투명
            _ammoPulseTweens[slot] = fillImage.DOColor(pulseColor, 1f / _reloadPulseSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine); // 부드러운 펄스
        }

        /// <summary>
        /// 무기 잔탄 장전 펄스 효과 정지
        /// </summary>
        private void StopAmmoReloadPulse(WeaponSlot slot)
        {
            if (_ammoPulseTweens.ContainsKey(slot) && _ammoPulseTweens[slot] != null)
            {
                _ammoPulseTweens[slot].Kill();
                _ammoPulseTweens.Remove(slot);
            }
        }

        /// <summary>
        /// 장전 완료 시 흰색 플래시 효과
        /// </summary>
        private void StartAmmoReloadCompleteFlash(WeaponSlot slot, Image fillImage)
        {
            if (fillImage == null) return;
            
            // 기존 펄스 정리
            StopAmmoReloadPulse(slot);
            
            // 밝은 흰색으로 순간 플래시 후 원래 색으로 복귀
            Color brightWhite = new Color(1f, 1f, 1f, 1f);
            _ammoPulseTweens[slot] = DOTween.Sequence()
                .Append(fillImage.DOColor(brightWhite, 0.1f))
                .Append(fillImage.DOColor(_reloadedColor, 0.2f))
                .OnComplete(() => {
                    if (_ammoPulseTweens.ContainsKey(slot))
                    {
                        _ammoPulseTweens.Remove(slot);
                    }
                });
        }

        private void Update() {
            // 메카가 비활성화되면 UI도 비활성화
            if (_mecha != null && !_mecha.gameObject.activeInHierarchy) {
                gameObject.SetActive(false);
                return;
            }
            
            UpdateUI();
            
            if (_isFollowTarget && _mecha != null && _rectTransform != null) {
                FollowTarget();
            }
        }

        private void FollowTarget() {
            Camera cam = _camera != null ? _camera : Camera.main;
            if (cam == null) return;
            
            // 월드 좌표 → 스크린 좌표 변환
            Vector3 worldPos = _mecha.transform.position;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            
            // 오프셋 적용 (스크린 픽셀 단위)
            screenPos.x += _offset.x;
            screenPos.y += _offset.y;
            
            // RectTransform 위치 설정
            _rectTransform.position = screenPos;
        }

        private void StartStaggerFlash() {
            if (_isStaggerFlashing) return;

            _isStaggerFlashing = true;

            float staggerDuration = _mecha != null ? _mecha.StaggerDuration : 2.5f;

            // 기존 트윈 정리
            if (_staggerTween != null && _staggerTween.IsActive()) {
                _staggerTween.Kill(true);
            }

            // 깜빡임 효과: 알파값을 빠르게 변화 (스태거 지속시간 동안만)
            _staggerTween = _staggerBar.DOFade(0.3f, 1f / _staggerFlashSpeed)
                .SetLoops(Mathf.Max(1, Mathf.RoundToInt(staggerDuration * _staggerFlashSpeed)), LoopType.Yoyo)
                .OnComplete(() => {
                    _isStaggerFlashing = false;
                    Color currentColor = _staggerBar.color;
                    _staggerBar.color = new Color(currentColor.r, currentColor.g, currentColor.b, 1f);
                });
        }

        private void StartHpPulse() {
            if (_apBar.loadingBar == null) return;

            // 기존 펄스가 있으면 재시작
            if (_hpPulseTween != null && _hpPulseTween.IsActive()) {
                _hpPulseTween.Kill(true);
            }

            Color startColor = _apBar.loadingBar.color;
            _hpPulseTween = DOTween.Sequence()
                .Append(_apBar.loadingBar.DOColor(_hpDangerColor, _hpPulseDuration * 0.5f))
                .Append(_apBar.loadingBar.DOColor(startColor, _hpPulseDuration * 0.5f))
                .OnComplete(() => {
                    _hpPulseTween = null;
                });
        }

        private void StartEnergyPulse() {
            if (_enBar.loadingBar == null) return;

            // 기존 펄스가 있으면 재시작
            if (_energyPulseTween != null && _energyPulseTween.IsActive()) {
                _energyPulseTween.Kill(true);
            }

            Color startColor = _enBar.loadingBar.color;
            _energyPulseTween = DOTween.Sequence()
                .Append(_enBar.loadingBar.DOColor(_energyPulseColor, _energyPulseDuration * 0.5f))
                .Append(_enBar.loadingBar.DOColor(startColor, _energyPulseDuration * 0.5f))
                .OnComplete(() => {
                    _energyPulseTween = null;
                });
        }

        #region Weapon Slot Mapping
        /// <summary>
        /// 특정 슬롯에 있는 무기를 찾아 반환
        /// </summary>
        private Weapon GetWeaponInSlot(WeaponSlot slot)
        {
            if (_mecha == null || _mecha.WeaponsAll == null) return null;

            var mechaAnimation = _mecha.GetComponentInChildren<MechaAnimation>();
            if (mechaAnimation == null) return null;

            Transform targetTransform = null;
            switch (slot)
            {
                case WeaponSlot.RightHand:
                    targetTransform = mechaAnimation.RightHand;
                    break;
                case WeaponSlot.LeftHand:
                    targetTransform = mechaAnimation.LeftHand;
                    break;
                case WeaponSlot.LeftShoulder:
                    targetTransform = mechaAnimation.LeftShoulder;
                    break;
                case WeaponSlot.RightShoulder:
                    targetTransform = mechaAnimation.RightShoulder;
                    break;
            }

            if (targetTransform == null) return null;

            // 해당 Transform 하위에 있는 무기 찾기
            foreach (var weapon in _mecha.WeaponsAll)
            {
                if (weapon != null && weapon.transform.IsChildOf(targetTransform))
                {
                    return weapon;
                }
            }

            return null;
        }

        #endregion

    }
}

