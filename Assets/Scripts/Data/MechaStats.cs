using UnityEngine;
using Sirenix.OdinInspector;

namespace GMLM.Data
{
    [System.Serializable]
    public class MechaStats
    {
        [Header("HP")]
        [SerializeField, MinValue(1)] public int maxHp = 100;

        [Header("Movement")]
        [SerializeField, MinValue(0f)] public float moveSpeed = 3.5f; // units/sec (최고 속도)
        [SerializeField, MinValue(0f)] public float acceleration = 12f;   // units/sec^2
        [SerializeField, MinValue(0f)] public float deceleration = 16f;   // units/sec^2
        [SerializeField, MinValue(0f)] public float turnRateDeg = 360f;   // deg/sec, 우측=정면 기준 회전 속도

        [Header("Energy")]
        [SerializeField, MinValue(0f)] public float maxEnergy = 100f;
        [SerializeField, MinValue(0f)] public float energyRegenPerSec = 10f;
        [SerializeField, MinValue(0f)] public float energyRegenDelay = 1.0f;

        [Header("Stagger System (AC6 Style)")]
        [SerializeField, MinValue(1)] public int maxStagger = 1000; // 스태거 임계값
        [SerializeField, MinValue(0f)] public float staggerDecayRate = 200f; // 초당 감소량
        [SerializeField, MinValue(0.1f)] public float staggerDuration = 2.5f; // 스태거 지속시간
        [SerializeField, MinValue(1f)] public float staggerDamageMultiplier = 1.5f; // 스태거 상태 데미지 배율
        [SerializeField, MinValue(0f), Tooltip("스태거 회복 시작 지연 (피격 시 리셋)")] public float staggerRecoveryDelay = 0.6f;

        [Header("Dash")]
        [SerializeField, MinValue(0f)] public float dashDistance = 3.0f;
        [SerializeField, MinValue(0f)] public float dashSpeed = 20.0f;
        [SerializeField, MinValue(0f)] public float dashCooldown = 1.0f;
        [SerializeField, MinValue(0f)] public float dashEnergyCost = 20.0f;
        [SerializeField, Tooltip("대시 속도 커브 (0=시작, 1=끝)")] 
        public AnimationCurve dashSpeedCurve = AnimationCurve.EaseInOut(0, 0.3f, 1, 0.1f);
        [SerializeField, Tooltip("커브 최고점에서의 속도 배율")] 
        public float dashPeakSpeedMultiplier = 2.5f;

        [Header("Assault Boost")]
        [SerializeField, MinValue(0f)] public float assaultBoostSpeedMultiplier = 1.5f;
        [SerializeField, MinValue(0f)] public float assaultBoostActivationCost = 15.0f;
        [SerializeField, MinValue(0f)] public float assaultBoostDrainPerSec = 35.0f;
        [SerializeField, Tooltip("어썰트 부스트 가속 시간 (초)")] 
        public float assaultBoostAccelTime = 0.4f;
        [SerializeField, Tooltip("어썰트 부스트 감속 시간 (초)")] 
        public float assaultBoostDecelTime = 0.2f;
        [SerializeField, Tooltip("어썰트 부스트 최대 속도 배율")] 
        public float assaultBoostMaxSpeedMultiplier = 2.0f;

        public MechaStats Clone()
        {
            return new MechaStats
            {
                maxHp = this.maxHp,
                moveSpeed = this.moveSpeed,
                acceleration = this.acceleration,
                deceleration = this.deceleration,
                turnRateDeg = this.turnRateDeg,
                maxEnergy = this.maxEnergy,
                energyRegenPerSec = this.energyRegenPerSec,
                energyRegenDelay = this.energyRegenDelay,
                maxStagger = this.maxStagger,
                staggerDecayRate = this.staggerDecayRate,
                staggerDuration = this.staggerDuration,
                staggerDamageMultiplier = this.staggerDamageMultiplier,
                staggerRecoveryDelay = this.staggerRecoveryDelay,
                dashDistance = this.dashDistance,
                dashSpeed = this.dashSpeed,
                dashCooldown = this.dashCooldown,
                dashEnergyCost = this.dashEnergyCost,
                dashSpeedCurve = this.dashSpeedCurve,
                dashPeakSpeedMultiplier = this.dashPeakSpeedMultiplier,
                assaultBoostSpeedMultiplier = this.assaultBoostSpeedMultiplier,
                assaultBoostActivationCost = this.assaultBoostActivationCost,
                assaultBoostDrainPerSec = this.assaultBoostDrainPerSec,
                assaultBoostAccelTime = this.assaultBoostAccelTime,
                assaultBoostDecelTime = this.assaultBoostDecelTime,
                assaultBoostMaxSpeedMultiplier = this.assaultBoostMaxSpeedMultiplier
            };
        }
    }
}
