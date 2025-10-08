using UnityEngine;
using System.Collections.Generic;

namespace GMLM.Game
{
	public class MechaAnimation : MonoBehaviour
	{
		[Header("Rig Transforms")]
		[SerializeField] private Transform _head;
		[SerializeField] private Transform _leftHand;
		[SerializeField] private Transform _rightHand;

		[Header("Yaw Limits (±deg)")]
		[SerializeField] private float _maxHeadYaw = 70f;
		[SerializeField] private float _maxLeftHandYaw = 95f;
		[SerializeField] private float _maxRightHandYaw = 95f;

		[Header("Turn Speeds (deg/sec)")]
		[SerializeField] private float _headTurnSpeed = 720f;
		[SerializeField] private float _leftHandTurnSpeed = 900f;
		[SerializeField] private float _rightHandTurnSpeed = 900f;

		[Header("Hand Rotation Gating")]
		[SerializeField, Tooltip("자식에 Weapon이 있을 때만 해당 손 회전 허용")] private bool _rotateLeftIfWeaponOnly = true;
		[SerializeField, Tooltip("자식에 Weapon이 있을 때만 해당 손 회전 허용")] private bool _rotateRightIfWeaponOnly = true;

		[Header("Body Assist Rotation")]
		[SerializeField, Tooltip("헤드/손이 한계에 가까워지면 동체가 보조 회전")] private bool _enableBodyAssist = true;
		[SerializeField, Range(0.5f, 0.99f), Tooltip("진입 비율(예: 0.85 → 85%)")] private float _assistEnterFrac = 0.85f;
		[SerializeField, Range(0.3f, 0.95f), Tooltip("이탈 비율(예: 0.65 → 65%)")] private float _assistExitFrac = 0.65f;
		[SerializeField, Tooltip("보조 회전 진입 지연(초)")] private float _assistDelaySec = 0.05f;

        [SerializeField] private GameObject _destroyEffect;

		// Cached base local Z for preserving authored rest pose
		private float _headBaseLocalZ;
		private float _leftBaseLocalZ;
		private float _rightBaseLocalZ;

		// Cached presence of weapons under hands
		private bool _leftHasWeapon;
		private bool _rightHasWeapon;
		private bool _assistActive;
		private float _assistTimer;

		[Header("Thrusters")]
		[SerializeField, Tooltip("버니어 위치 리스트")] private List<Transform> _thrusterPoints;
		[SerializeField, Tooltip("대시 FX 프리팹(선택)")] private GameObject _dashFxPrefab;
		[SerializeField, Tooltip("스러스터 최대 크기(스케일)")] private float _thrusterMaxScale = 0.85f;
		[SerializeField, Tooltip("이동 FX 비활성화 지연(초)")] private float _moveFxTimeoutSec = 0.12f;
		[SerializeField, Tooltip("이동 FX 페이드아웃 시간(초)")] private float _moveFxFadeOutSec = 0.2f;
		private readonly List<GameObject> _dashFxInstances = new List<GameObject>();
		private bool _isDashFxActive = false;
		private Vector2 _dashOppDir = Vector2.right;
		private float _moveFxTimer = 0f;
		private bool _isMoveFading = false;
		private float _moveFxFadeT = 0f;
		private readonly List<float> _moveFxFadeStartScales = new List<float>();

		private void Awake()
		{
			CacheBaseLocalRotations();
			RefreshWeaponPresence();
			// Ensure thruster FX instances exist before first movement
			if (_dashFxPrefab != null && _dashFxInstances.Count == 0)
			{
				SetupDashFxInstances(_dashFxPrefab);
			}
		}

		private void Update()
		{
			// 이동 FX 자동 소거 및 페이드아웃
			if (_isDashFxActive) return; // 대시 중에는 이동 FX 관리하지 않음
			if (_dashFxInstances.Count == 0) return;
			if (_moveFxTimer > 0f)
			{
				_moveFxTimer -= Time.deltaTime;
				if (_moveFxTimer > 0f)
				{
					_isMoveFading = false; // 이동 갱신 중에는 페이드 리셋
					return;
				}
			}
			// 타이머 만료 → 페이드 시작/진행
			if (!_isMoveFading)
			{
				_isMoveFading = true;
				_moveFxFadeT = 0f;
				// 시작 스케일 기록
				if (_moveFxFadeStartScales.Count != _dashFxInstances.Count)
				{
					_moveFxFadeStartScales.Clear();
					for (int i = 0; i < _dashFxInstances.Count; i++) _moveFxFadeStartScales.Add(0f);
				}
				for (int i = 0; i < _dashFxInstances.Count; i++)
				{
					var fx = _dashFxInstances[i];
					_moveFxFadeStartScales[i] = (fx != null) ? fx.transform.localScale.x : 0f;
				}
			}
			float dur = Mathf.Max(0.01f, _moveFxFadeOutSec);
			_moveFxFadeT += Time.deltaTime;
			float t = Mathf.Clamp01(_moveFxFadeT / dur);
			for (int i = 0; i < _dashFxInstances.Count; i++)
			{
				var fx = _dashFxInstances[i];
				if (fx == null) continue;
				float s0 = (i < _moveFxFadeStartScales.Count) ? _moveFxFadeStartScales[i] : 0f;
				float s = Mathf.Lerp(s0, 0f, t);
				fx.transform.localScale = Vector3.one * s;
				if (t >= 1f) fx.SetActive(false);
			}
			if (_moveFxFadeT >= dur) _isMoveFading = false;
		}

        private void OnDisable()
        {
            if (_destroyEffect != null)
            {
                Instantiate(_destroyEffect, transform.position, transform.rotation);
            }
        }

		/// <summary>
		/// 현재 로컬 Z를 기준 자세로 캐시한다.
		/// </summary>
		public void CacheBaseLocalRotations()
		{
			if (_head != null) _headBaseLocalZ = _head.localEulerAngles.z;
			if (_leftHand != null) _leftBaseLocalZ = _leftHand.localEulerAngles.z;
			if (_rightHand != null) _rightBaseLocalZ = _rightHand.localEulerAngles.z;
		}

		/// <summary>
		/// Hand 하위에 Weapon 존재 여부를 갱신한다.
		/// </summary>
		public void RefreshWeaponPresence()
		{
			_leftHasWeapon = HasWeaponUnder(_leftHand);
			_rightHasWeapon = HasWeaponUnder(_rightHand);
		}

		/// <summary>
		/// 헤드/손 Yaw 여유가 부족할 때 동체 보조 회전 필요 여부와 잔여 각도를 계산한다.
		/// </summary>
		public bool EvaluateBodyAssist(Vector2 desiredWorldDirection, out float residualYawDeg, out int sign)
		{
			residualYawDeg = 0f;
			sign = 0;
			if (!_enableBodyAssist) return false;

			Vector2 bodyDir = (Vector2)transform.right;
			Vector2 targetDir = (desiredWorldDirection.sqrMagnitude > 0.0001f && IsFinite(desiredWorldDirection))
				? desiredWorldDirection.normalized : bodyDir;
			float deltaDeg = Vector2.SignedAngle(bodyDir, targetDir);
			float absDelta = Mathf.Abs(deltaDeg);
			sign = (deltaDeg >= 0f) ? 1 : -1;

			// 유효 구성요소의 최소 허용각 사용(가장 먼저 포화되는 축 기준)
			float minLimit = 0f;
			if (_head != null && _maxHeadYaw > 0f) minLimit = (minLimit <= 0f) ? _maxHeadYaw : Mathf.Min(minLimit, _maxHeadYaw);
			bool leftAllowed = (_leftHand != null && _maxLeftHandYaw > 0f && (!_rotateLeftIfWeaponOnly || _leftHasWeapon));
			bool rightAllowed = (_rightHand != null && _maxRightHandYaw > 0f && (!_rotateRightIfWeaponOnly || _rightHasWeapon));
			if (leftAllowed) minLimit = (minLimit <= 0f) ? _maxLeftHandYaw : Mathf.Min(minLimit, _maxLeftHandYaw);
			if (rightAllowed) minLimit = (minLimit <= 0f) ? _maxRightHandYaw : Mathf.Min(minLimit, _maxRightHandYaw);

			if (minLimit <= 0f)
			{
				// 조준 축이 없으면 보조 회전 불필요
				return false;
			}

			float enterDeg = Mathf.Clamp01(_assistEnterFrac) * minLimit;
			float exitDeg = Mathf.Clamp01(_assistExitFrac) * minLimit;
			if (exitDeg > enterDeg) exitDeg = enterDeg * 0.8f;

			// 진입/이탈 히스테리시스 + 소량 지연
			if (!_assistActive)
			{
				if (absDelta > enterDeg)
				{
					_assistTimer -= Time.deltaTime;
					if (_assistTimer <= 0f)
					{
						_assistActive = true;
					}
				}
				else
				{
					_assistTimer = _assistDelaySec;
				}
			}
			else
			{
				if (absDelta < exitDeg)
				{
					_assistActive = false;
					_assistTimer = _assistDelaySec;
				}
			}

			if (_assistActive)
			{
				// 잔여 각도: 목표를 exitDeg까지 끌어내리는 양
				residualYawDeg = Mathf.Max(0f, absDelta - exitDeg);
			}
			return _assistActive;
		}

		public void PlayDashFx(Vector2 direction, float duration)
		{
			if (_dashFxPrefab == null) return;
			_isDashFxActive = true;
			_dashOppDir = (-direction).sqrMagnitude > 1e-6f ? (-direction).normalized : Vector2.right;
			if (_dashFxInstances.Count == 0)
			{
				SetupDashFxInstances(_dashFxPrefab);
			}
			if (_dashFxInstances.Count == 0) return;
			Vector2 dirNorm = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector2.right;
			Vector2 oppDir = -dirNorm;
			const float minIntensity = 0.05f;
			for (int i = 0; i < _dashFxInstances.Count; i++)
			{
				var fx = _dashFxInstances[i];
				if (fx == null) continue;
				Vector2 offsetNorm;
				if (_thrusterPoints != null && _thrusterPoints.Count == _dashFxInstances.Count)
				{
					var tp = _thrusterPoints[i];
					if (tp == null) { fx.SetActive(false); continue; }
					Vector2 offset = (Vector2)tp.position - (Vector2)transform.position;
					if (offset.sqrMagnitude < 1e-6f) { fx.SetActive(false); continue; }
					offsetNorm = offset.normalized;
				}
				else { offsetNorm = Vector2.right; }
				float intensity = Mathf.Max(0f, Vector2.Dot(oppDir, offsetNorm));
				if (intensity < minIntensity) { fx.SetActive(false); continue; }
				Vector2 flameDir = -offsetNorm;
				fx.transform.right = flameDir;
				fx.transform.Rotate(0f, 0f, 90f);
				float scaleFactor = Mathf.Lerp(0.15f, _thrusterMaxScale, intensity * intensity);
				fx.transform.localScale = Vector3.one * scaleFactor;
				AdjustParticleDuration(fx, duration);
				fx.SetActive(true);
			}
		}

		public void UpdateDashThrusters()
		{
			if (!_isDashFxActive) return;
			const float minIntensity = 0.05f;
			for (int i = 0; i < _dashFxInstances.Count; i++)
			{
				var fx = _dashFxInstances[i];
				if (fx == null) continue;
				Vector2 offsetNorm;
				if (_thrusterPoints != null && _thrusterPoints.Count == _dashFxInstances.Count)
				{
					var tp = _thrusterPoints[i];
					if (tp == null) { fx.SetActive(false); continue; }
					Vector2 offset = (Vector2)tp.position - (Vector2)transform.position;
					if (offset.sqrMagnitude < 1e-6f) { fx.SetActive(false); continue; }
					offsetNorm = offset.normalized;
				}
				else { offsetNorm = Vector2.right; }
				float intensity = Mathf.Max(0f, Vector2.Dot(_dashOppDir, offsetNorm));
				if (intensity < minIntensity) { fx.SetActive(false); continue; }
				Vector2 flameDir = -offsetNorm;
				Quaternion rot = Quaternion.FromToRotation(Vector3.right, flameDir) * Quaternion.Euler(0f, 0f, 90f);
				fx.transform.rotation = rot;
				float scale = Mathf.Lerp(0.15f, _thrusterMaxScale, intensity * intensity);
				fx.transform.localScale = Vector3.one * scale;
				if (!fx.activeSelf) fx.SetActive(true);
			}
		}

		public void StopDashFx()
		{
			_isDashFxActive = false;
			for (int i = 0; i < _dashFxInstances.Count; i++)
			{
				var fx = _dashFxInstances[i];
				if (fx != null) fx.SetActive(false);
			}
		}

		public void UpdateMoveThrusters(Vector2 velocity)
		{
			if (_isDashFxActive) return;
			if (_dashFxInstances.Count == 0)
			{
				if (_dashFxPrefab != null) SetupDashFxInstances(_dashFxPrefab);
				if (_dashFxInstances.Count == 0) return;
			}
			if (float.IsNaN(velocity.x) || float.IsInfinity(velocity.x) || float.IsNaN(velocity.y) || float.IsInfinity(velocity.y))
			{
				for (int i = 0; i < _dashFxInstances.Count; i++) { var fx = _dashFxInstances[i]; if (fx != null) fx.SetActive(false); }
				return;
			}
			float speed = velocity.magnitude;
			if (speed < 0.01f)
			{
				for (int i = 0; i < _dashFxInstances.Count; i++) { var fx = _dashFxInstances[i]; if (fx != null) fx.SetActive(false); }
				return;
			}
			Vector2 dirNorm = velocity.normalized;
			Vector2 oppDir = -dirNorm;
			const float minIntensity = 0.05f;
			for (int i = 0; i < _dashFxInstances.Count; i++)
			{
				var fx = _dashFxInstances[i];
				if (fx == null) continue;
				Vector2 offsetNorm;
				if (_thrusterPoints != null && _thrusterPoints.Count == _dashFxInstances.Count)
				{
					var tp = _thrusterPoints[i];
					if (tp == null) { fx.SetActive(false); continue; }
					Vector2 offset = (Vector2)tp.position - (Vector2)transform.position;
					if (offset.sqrMagnitude < 1e-6f) { fx.SetActive(false); continue; }
					offsetNorm = offset.normalized;
				}
				else { offsetNorm = Vector2.right; }
				float intensity = Mathf.Max(0f, Vector2.Dot(oppDir, offsetNorm));
				if (intensity < minIntensity) { fx.SetActive(false); continue; }
				Vector2 flameDir = -offsetNorm;
				Quaternion rot = Quaternion.FromToRotation(Vector3.right, flameDir) * Quaternion.Euler(0f, 0f, 90f);
				fx.transform.rotation = rot;
				float scale = Mathf.Lerp(0.08f, _thrusterMaxScale, intensity);
				fx.transform.localScale = Vector3.one * scale;
				if (!fx.activeSelf) fx.SetActive(true);
			}
			// 이동이 갱신되었으므로 타이머 갱신
			_moveFxTimer = _moveFxTimeoutSec;
			_isMoveFading = false;
		}

		public void SetupDashFxInstances(GameObject prefab)
		{
			if (prefab == null) return;
			if (_dashFxInstances.Count > 0) return;
			if (_thrusterPoints == null || _thrusterPoints.Count == 0)
			{
				CreateFxInstance(prefab, transform);
				return;
			}
			for (int i = 0; i < _thrusterPoints.Count; i++)
			{
				var pt = _thrusterPoints[i];
				if (pt == null) continue;
				CreateFxInstance(prefab, pt);
			}
		}

		private void CreateFxInstance(GameObject prefab, Transform parent)
		{
			var fx = Object.Instantiate(prefab, parent);
			fx.transform.localPosition = Vector3.zero;
			fx.transform.localRotation = Quaternion.identity;
			fx.SetActive(false);
			_dashFxInstances.Add(fx);
		}

		private void AdjustParticleDuration(GameObject fx, float duration)
		{
			if (duration <= 0f || fx == null) return;
			var particles = fx.GetComponentsInChildren<ParticleSystem>(true);
			for (int i = 0; i < particles.Length; i++)
			{
				var ps = particles[i];
				ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
				var main = ps.main;
				main.duration = duration;
				main.startLifetime = new ParticleSystem.MinMaxCurve(duration);
				ps.Play();
			}
		}

		private static bool HasWeaponUnder(Transform root)
		{
			if (root == null) return false;
			return root.GetComponentInChildren<Weapon>(true) != null;
		}

		/// <summary>
		/// 몸체 기준 desiredWorldDirection으로 헤드/양손을 부드럽게 조준한다.
		/// </summary>
		public void UpdateAiming(Vector2 desiredWorldDirection)
		{
			Vector2 bodyDir = transform.right;
			Vector2 targetDir = (desiredWorldDirection.sqrMagnitude > 0.0001f && IsFinite(desiredWorldDirection))
				? desiredWorldDirection.normalized : bodyDir;
			float deltaDeg = Vector2.SignedAngle(bodyDir, targetDir);

			// Head
			if (_head != null && _maxHeadYaw > 0f)
			{
				float clamped = Mathf.Clamp(deltaDeg, -_maxHeadYaw, _maxHeadYaw);
				float currentZ = _head.localEulerAngles.z;
				float targetZ = _headBaseLocalZ + clamped;
				float nextZ = Mathf.MoveTowardsAngle(currentZ, targetZ, _headTurnSpeed * Time.deltaTime);
				_head.localRotation = Quaternion.Euler(0f, 0f, nextZ);
			}

			// Left Hand (gated by weapon presence if enabled)
			if (_leftHand != null && _maxLeftHandYaw > 0f && (!_rotateLeftIfWeaponOnly || _leftHasWeapon))
			{
				float clamped = Mathf.Clamp(deltaDeg, -_maxLeftHandYaw, _maxLeftHandYaw);
				float currentZ = _leftHand.localEulerAngles.z;
				float targetZ = _leftBaseLocalZ + clamped;
				float nextZ = Mathf.MoveTowardsAngle(currentZ, targetZ, _leftHandTurnSpeed * Time.deltaTime);
				_leftHand.localRotation = Quaternion.Euler(0f, 0f, nextZ);
			}

			// Right Hand (gated by weapon presence if enabled)
			if (_rightHand != null && _maxRightHandYaw > 0f && (!_rotateRightIfWeaponOnly || _rightHasWeapon))
			{
				float clamped = Mathf.Clamp(deltaDeg, -_maxRightHandYaw, _maxRightHandYaw);
				float currentZ = _rightHand.localEulerAngles.z;
				float targetZ = _rightBaseLocalZ + clamped;
				float nextZ = Mathf.MoveTowardsAngle(currentZ, targetZ, _rightHandTurnSpeed * Time.deltaTime);
				_rightHand.localRotation = Quaternion.Euler(0f, 0f, nextZ);
			}
		}

		/// <summary>
		/// 좌측 손만 조준(무기 게이트 적용).
		/// </summary>
		public void UpdateLeftHandAim(Vector2 desiredWorldDirection)
		{
			if (_leftHand == null || _maxLeftHandYaw <= 0f) return;
			if (_rotateLeftIfWeaponOnly && !_leftHasWeapon) return;
			Vector2 bodyDir = transform.right;
			Vector2 targetDir = (desiredWorldDirection.sqrMagnitude > 0.0001f && IsFinite(desiredWorldDirection))
				? desiredWorldDirection.normalized : bodyDir;
			float deltaDeg = Vector2.SignedAngle(bodyDir, targetDir);
			float clamped = Mathf.Clamp(deltaDeg, -_maxLeftHandYaw, _maxLeftHandYaw);
			float currentZ = _leftHand.localEulerAngles.z;
			float targetZ = _leftBaseLocalZ + clamped;
			float nextZ = Mathf.MoveTowardsAngle(currentZ, targetZ, _leftHandTurnSpeed * Time.deltaTime);
			_leftHand.localRotation = Quaternion.Euler(0f, 0f, nextZ);
		}

		/// <summary>
		/// 우측 손만 조준(무기 게이트 적용).
		/// </summary>
		public void UpdateRightHandAim(Vector2 desiredWorldDirection)
		{
			if (_rightHand == null || _maxRightHandYaw <= 0f) return;
			if (_rotateRightIfWeaponOnly && !_rightHasWeapon) return;
			Vector2 bodyDir = transform.right;
			Vector2 targetDir = (desiredWorldDirection.sqrMagnitude > 0.0001f && IsFinite(desiredWorldDirection))
				? desiredWorldDirection.normalized : bodyDir;
			float deltaDeg = Vector2.SignedAngle(bodyDir, targetDir);
			float clamped = Mathf.Clamp(deltaDeg, -_maxRightHandYaw, _maxRightHandYaw);
			float currentZ = _rightHand.localEulerAngles.z;
			float targetZ = _rightBaseLocalZ + clamped;
			float nextZ = Mathf.MoveTowardsAngle(currentZ, targetZ, _rightHandTurnSpeed * Time.deltaTime);
			_rightHand.localRotation = Quaternion.Euler(0f, 0f, nextZ);
		}

		private static bool IsFinite(Vector2 v)
		{
			return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.x) || float.IsInfinity(v.y));
		}
	}
}


