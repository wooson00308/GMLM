# 🛡️ SecureCodec 사용 예시

이 폴더는 SecureCodec 시스템을 실제 게임에서 어떻게 활용하는지 보여주는 예시 코드들입니다.

자세한 개념/튜토리얼은 `Assets/Scripts/SecureCodec/Docs/SecureCodec_Guide.md` 를 참고하세요.

## 📁 포함된 예시들

### 🎮 GameManagerExample.cs
**보호 대상**: 게임 진행도
- `SecureInt currentStage` - 현재 스테이지 
- `SecureInt maxUnlockedStage` - 해금된 최대 스테이지
- `SecureInt totalScore` - 총 점수
- `SecureBool isBossStage` - 보스 스테이지 여부

**핵심 기능**:
- 스테이지 진행 시 자동 해금 체크
- 메모리 조작으로 스테이지 건너뛰기 방지
- 치트 감지 시 게임 데이터 보호

### 👹 MonsterExample.cs  
**보호 대상**: 몬스터 스탯
- `SecureInt currentHp/maxHp` - 체력
- `SecureInt attack/defense` - 공격력/방어력
- `SecureInt expReward/goldReward` - 보상

**핵심 기능**:
- 몬스터 스탯 조작 방지
- 비정상적인 수치 감지 및 복구
- 보상 조작 차단

### 🧙 PlayerExample.cs
**보호 대상**: 플레이어 핵심 데이터
- `SecureInt level/currentExp` - 레벨/경험치
- `SecureInt currentHp/maxHp/currentMp/maxMp` - 체력/마나
- `SecureInt gold/gems/energy` - 재화 (골드/젬/에너지)
- `SecureInt strength/intelligence/agility` - 능력치

**핵심 기능**:
- 레벨/경험치 조작 방지
- 무한 골드/젬 치트 차단
- 능력치 해킹 감지

## 🚀 사용 방법

### 1. 기본 사용법
```csharp
// 기존 방식
int playerHp = 100;

// 보안 적용
SecureInt playerHp = 100;

// 사용법은 완전 동일
playerHp.Value += 50;        // 직접 값 변경
int currentHp = playerHp;    // 암시적 변환
```

### 2. 치트 감지 처리
```csharp
void Start()
{
    // 치트 감지 핸들러 등록
    AntiCheatSignals.RegisterCheatHandler(OnCheatDetected);
}

void OnCheatDetected(string reason)
{
    Debug.LogError($"치트 감지: {reason}");
    // 게임 종료, 경고 메시지, 서버 신고 등
}
```

### 3. 스탯 검증
```csharp
void ValidateStats()
{
    if (gold > 999999)
    {
        AntiCheatSignals.Flag("Player_InvalidGold");
        gold = 0; // 정상 수치로 복구
    }
}
```

## ⚡ 성능 특징

- **메모리 오버헤드**: 일반 int 대비 약 4배 (32바이트 vs 8바이트)
- **CPU 오버헤드**: 접근 시마다 XOR 연산 + 키 롤링
- **권장 사용처**: 
  - ✅ 핵심 게임 데이터 (레벨, 골드, 아이템 등)
  - ✅ 게임 밸런스에 영향을 주는 수치
  - ❌ 매 프레임 계산되는 임시 변수
  - ❌ UI 애니메이션 등 시각적 요소

## 🔒 보안 수준

1. **XOR 암호화**: 메모리에서 실제 값 숨김
2. **키 롤링**: 매 접근시마다 암호화 키 변경  
3. **무결성 검증**: 페이크 값으로 메모리 변조 감지
4. **치트 신호**: 감지 시 즉시 알림 및 대응

## 📝 주의사항

- 자주 접근하는 변수는 로컬 변수에 캐싱해서 사용
- 에디터에서는 경고 수준, 릴리스에서는 강력한 대응 설정 가능
- 네트워크 동기화가 필요한 데이터는 서버 검증도 함께 구현 권장
