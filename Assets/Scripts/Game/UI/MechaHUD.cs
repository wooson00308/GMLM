using Michsky.MUIP;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

namespace GMLM.Game
{
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
        [SerializeField] private bool _isFollowTarget = false;
        [SerializeField] private Vector2 _offset = new Vector2(0, 0);
        [SerializeField] private Camera _camera; // null이면 Camera.main 사용
        [SerializeField] private float _staggerFlashSpeed = 10.0f; // 깜빡임 속도
        [SerializeField] private Color _hpDangerColor = Color.red; // HP 위험 색상
        [SerializeField] private Color _energyLowColor = Color.cyan; // 에너지 부족 색상
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

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            
            if(_mecha != null) {
                Initialize(_mecha);
            }
        }

        public void Initialize(Mecha mecha)
        {
            _mecha = mecha;
            //_nameText.text = mecha.Pilot.Name;
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
            
            UpdateUI();
        }

        private void UpdateUI() {
            _apBar.currentPercent = (float)_mecha.CurrentHp / _mecha.MaxHp * 100f;
            _enBar.currentPercent = _mecha.CurrentEnergy / _mecha.MaxEnergy * 100f;

            // AP(HP) 감소 시 단발성 붉은 펄스
            if (_apBar.loadingBar != null) {
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

            // 에너지 부족 색상 변경 (20% 이하)
            if (_enBar.loadingBar != null) {
                float energyRatio = _mecha.CurrentEnergy / _mecha.MaxEnergy;
                if (energyRatio <= _energyLowThreshold) {
                    _enBar.loadingBar.color = _energyLowColor;
                } else {
                    _enBar.loadingBar.color = _originalEnergyColor;
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

    }
}

