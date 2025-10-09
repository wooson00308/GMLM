# AC6 스타일 투사체 프리셋 가이드

## 🎯 기본 개념

AC6의 투사체는 4단계 라이프사이클을 가집니다:
1. **발사 (0 ~ homingDelay)**: 직진 비행
2. **호밍 활성 (homingDelay ~ homingDuration)**: 제한된 선회로 추적
3. **연료 소진 (homingDuration ~ lifeTime)**: 직진
4. **소멸 (lifeTime)**

---

## 🚀 무기별 프리셋

### 1. 머신건 / 개틀링 (일반 탄환)
```
Speed: 20~30 m/s
LifeTime: 1.5초
IsHoming: false
IsHighThreat: false

용도: 지속 화력, 스트레이프로 회피 가능
```

### 2. 바주카 / 로켓 (저속 고폭탄)
```
Speed: 12~15 m/s
LifeTime: 3초
IsHoming: true
IsHighThreat: true  ⚠️ 대시 유발

Homing Settings:
├─ homingDelay: 0.3초 (발사 후 관성 비행)
├─ homingStrength: 0.3~0.5 (약한 궤도 보정)
├─ maxTurnRateDeg: 90 deg/sec
├─ homingDuration: 2.5초
└─ maxTrackingAngleDeg: 120도

특징: 느리지만 예측 어려움, 약한 호밍으로 회피 유도
```

### 3. 일반 미사일 (중속 유도탄)
```
Speed: 18~22 m/s
LifeTime: 4초
IsHoming: true
IsHighThreat: true  ⚠️ 대시 유발

Homing Settings:
├─ homingDelay: 0.2초
├─ homingStrength: 0.8 (강한 추적)
├─ maxTurnRateDeg: 180 deg/sec
├─ homingDuration: 3.5초
└─ maxTrackingAngleDeg: 150도

특징: AC6 표준 미사일, 회피 가능하나 압박 강함
```

### 4. 고성능 미사일 (고속 정밀 유도)
```
Speed: 25~30 m/s
LifeTime: 5초
IsHoming: true
IsHighThreat: true  ⚠️ 대시 유발

Homing Settings:
├─ homingDelay: 0.1초 (즉시 추적)
├─ homingStrength: 1.0 (최대 추적)
├─ maxTurnRateDeg: 360 deg/sec (급선회 가능)
├─ homingDuration: 4.5초
└─ maxTrackingAngleDeg: 170도

특징: 회피 극도로 어려움, 에너지 소모 대가
```

### 5. 플라즈마 / 레이저 빔 (직선 에너지탄)
```
Speed: 40~60 m/s
LifeTime: 0.8초
IsHoming: false
IsHighThreat: false

특징: 빠르지만 직진, 예측 회피 가능
```

### 6. 근거리 산탄 / 샷건
```
Speed: 15 m/s
LifeTime: 0.5초
IsHoming: false
IsHighThreat: false

특징: 짧은 수명, 여러 발 동시 발사 (Spread 활용)
```

---

## ⚙️ 세팅 가이드

### homingDelay (호밍 지연)
- **0.0초**: 발사 즉시 추적 (고성능 미사일)
- **0.2초**: 짧은 직진 후 추적 (표준)
- **0.3~0.5초**: 긴 관성 비행 (바주카, 로켓)

### homingStrength (호밍 강도)
- **0.0~0.3**: 약한 궤도 보정 (로켓, 바주카)
- **0.5~0.7**: 중간 추적 (저렴한 미사일)
- **0.8~1.0**: 강한 추적 (고급 미사일)

### maxTurnRateDeg (회전 속도)
- **60~90**: 느린 선회 (무거운 로켓)
- **120~180**: 보통 선회 (표준 미사일)
- **240~360**: 빠른 선회 (고기동 미사일)

### homingDuration (호밍 지속)
- **1.5~2.5초**: 짧은 추적 (에너지 절약형)
- **3.0~4.0초**: 표준 추적
- **4.5~5.0초**: 긴 추적 (고급형)

### maxTrackingAngleDeg (추적 시야각)
- **90~120도**: 좁은 시야 (회피 쉬움)
- **130~150도**: 보통 시야
- **160~180도**: 넓은 시야 (거의 전방향)

---

## 💡 밸런싱 팁

1. **IsHighThreat는 신중히**:
   - true로 설정 시 AI가 강제 대시 → 에너지 소모
   - 남발하면 방어 불가능

2. **속도 vs 호밍 트레이드오프**:
   - 빠른 탄: 호밍 약하게 (회피 가능)
   - 느린 탄: 호밍 강하게 (예측 어려움)

3. **에너지 경제**:
   - 고성능 미사일 = 높은 데미지 + 에너지 소모
   - 머신건 = 낮은 데미지 + 무제한 탄환

4. **레이어링**:
   - 머신건(압박) + 미사일(마무리)
   - 바주카(견제) + 플라즈마(피니시)

---

## 🔧 Unity Inspector 예시

### 표준 미사일 세팅:
```
[Projectile Component]
Speed: 20
Life Time: 4
Is Homing: ✓
Hit Effect: MissileHitFX
Destroy Effect: MissileExplosion

[Threat Level]
Is High Threat: ✓

[Homing Settings (AC6 Style)]
Homing Delay: 0.2
Homing Strength: 0.8
Max Turn Rate Deg: 180
Homing Duration: 3.5
Max Tracking Angle Deg: 150
```

---

## 📊 테스트 시나리오

1. **직진 회피 테스트**: 
   - homingStrength = 0
   - 스트레이프만으로 회피 가능한지

2. **대시 필수 테스트**:
   - IsHighThreat = true
   - TTI < 0.25초에 대시 발동 확인

3. **추적 한계 테스트**:
   - 180도 선회 시 호밍 해제 확인
   - maxTrackingAngleDeg 작동 여부

4. **연료 소진 테스트**:
   - homingDuration 이후 직진 확인
   - 회피 타이밍 연습

---

생성일: 2025-10-09
버전: AC6-Style v1.0

