using Michsky.MUIP;
using UnityEngine;
using TMPro;

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
        [SerializeField] private bool _isFollowTarget = false;
        [SerializeField] private Vector2 _offset = new Vector2(0, 0);
        [SerializeField] private Camera _camera; // null이면 Camera.main 사용
        private RectTransform _rectTransform;

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
            UpdateUI();
        }

        private void UpdateUI() {
            _apBar.currentPercent = (float)_mecha.CurrentHp / _mecha.MaxHp * 100f;
            _enBar.currentPercent = _mecha.CurrentEnergy / _mecha.MaxEnergy * 100f;
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
    }
}

