# SecureCodec Guide

이 문서는 Unity에서 메모리 변조(치트) 저항성을 높이기 위한 SecureCodec 시스템의 배경지식과 사용법, 내부 동작을 설명합니다.

## 1) 왜 필요한가
- 일반 int/float/Vector 값은 메모리에서 그대로 노출됩니다.
- 치트 툴(CE 등)은 값을 검색/고정/조작하여 게임 밸런스를 붕괴시킵니다.
- 네트워크 검증이 없는 솔로/로컬 게임에서도 최소한의 방어막이 필요합니다.

SecureCodec은 값을 암호화하여 저장하고, 접근 시에만 복호화하는 방식으로 메모리에서의 직접 검색과 변조를 어렵게 만듭니다.

## 2) 핵심 컨셉
- Secure<T, TCodec>: 제네릭 보안 컨테이너. T 값을 32비트 정수 레인들로 직렬화/역직렬화하는 ISecureCodec<T>를 이용.
- ISecureCodec<T>: 타입 T를 32비트 정수 레인(int 슬롯)으로 변환하는 직렬화 규약. int/float/bool/Vector2/3/4, double 지원.
- 무결성 미러(fake): 복호화된 원시 값에 간단한 변환(XOR + salt)을 적용해 보관. 재복호 결과와 불일치 시 변조로 간주하고 플래그 발생.
- 키 롤링(key rolling): 값을 읽을 때마다 랜덤 키를 재생성하고 재암호화. 메모리 스캐너가 값을 특정하기 어렵습니다.

## 3) 내부 동작 흐름
1. 설정(생성/대입)
   - Codec으로 T → 레인(int[]) 직렬화
   - 레인 값을 임의 키와 XOR하여 encN에 저장
   - 동일 원시값에 간단한 함수(XOR + salt)를 적용해 fakeN 저장(무결성 미러)
2. 읽기(get)
   - encN ^ keyN으로 원시값 복호
   - (원시값 ^ mask) + salt == fakeN 검증. 실패 시 AntiCheatSignals.Flag() 호출
   - 즉시 새로운 키를 생성하여 encN 재암호화(키 롤링)
3. 쓰기(set)
   - 새 값을 직렬화 → 암호화 → 미러 갱신

## 4) 제공 타입
- 기본: SecureInt, SecureFloat, SecureDouble, SecureBool
- 벡터: SecureVector2, SecureVector3, SecureVector4
- 직접 제네릭 사용도 가능: Secure<MyType, MyCodec>

## 5) 인스펙터/에디터 통합
- Editor/SecureTypeDrawers.cs에 PropertyDrawer 제공 → 에디터에서 일반 필드처럼 편집 가능
- 런타임 빌드에 포함되지 않음(#if UNITY_EDITOR)

## 6) 사용법 요약
```csharp
[SerializeField] private SecureInt health = 100;

// 읽기
int hp = health;

// 쓰기
health.Value += 50;

// 벡터 타입
[SerializeField] private SecureVector3 spawn = new Vector3(0,1,0);
```

## 7) 커스텀 타입 확장하기
1) Codec 구현
```csharp
public struct QuaternionCodec : ISecureCodec<Quaternion>
{
    public int Lanes => 4;
    public void ToLanes(in Quaternion v, Span<int> lanes)
    {
        lanes[0] = System.BitConverter.SingleToInt32Bits(v.x);
        lanes[1] = System.BitConverter.SingleToInt32Bits(v.y);
        lanes[2] = System.BitConverter.SingleToInt32Bits(v.z);
        lanes[3] = System.BitConverter.SingleToInt32Bits(v.w);
    }
    public Quaternion FromLanes(ReadOnlySpan<int> lanes)
    {
        return new Quaternion(
            System.BitConverter.Int32BitsToSingle(lanes[0]),
            System.BitConverter.Int32BitsToSingle(lanes[1]),
            System.BitConverter.Int32BitsToSingle(lanes[2]),
            System.BitConverter.Int32BitsToSingle(lanes[3]));
    }
}
```
2) 래퍼 타입(선택)
```csharp
[System.Serializable]
public struct SecureQuaternion
{
    private Secure<UnityEngine.Quaternion, QuaternionCodec> _value;
    public SecureQuaternion(UnityEngine.Quaternion v){ _value = new Secure<UnityEngine.Quaternion, QuaternionCodec>(v);}    
    public UnityEngine.Quaternion Value { get=>_value.Value; set=>_value.Value = value; }
    public static implicit operator UnityEngine.Quaternion(SecureQuaternion s)=>s._value.Value;
    public static implicit operator SecureQuaternion(UnityEngine.Quaternion v)=> new SecureQuaternion(v);
}
```

## 8) 보안 모델과 한계
- 목적: 메모리 조작 난이도 증가 및 간단한 변조 탐지
- 한계: 게임 프로세스를 완벽히 보호하지는 못함(디버깅 툴, 커널 드라이버, 코드 인젝션 등은 별도 대응 필요)
- 권장: 서버 검증(멀티), 스냅샷 해시, 파일 무결성 검사, 안티 치트 솔루션과 병행

## 9) 성능 고려
- 오버헤드: 각 필드당 4~6개의 int 저장 + 접근 시 XOR/검증/키 재생성
- 권장 적용 범위: 핵심 수치(레벨, 재화, 보상, 밸런스 결정 스탯). 프레임당 다량 접근되는 임시 값에는 비권장.
- 최적화 팁: 잦은 읽기 구간은 지역 변수로 캐싱 후 연산

## 10) FAQ
- Editor에서 값 바꾸면 안전한가요?
  - 드로어가 Value를 통해 정상 경로로 설정하므로, 미러와 키가 함께 갱신됩니다.
- 값이 가끔 초기화되는 것 같아요
  - 구조체 초기화/복사 타이밍에 주의하세요. 인스펙터 수정은 드로어가 안전하게 처리합니다.
- 왜 XOR + salt 검증인가요?
  - 가볍고 빠릅니다. 필요시 더 강한 해시(예: xxHash, BLAKE2s)를 추가 가능.

---

## Hands-on 튜토리얼
1) 필드 선언
```csharp
[SerializeField] private SecureInt gold = 1000;
```
2) 보상 지급
```csharp
gold.Value += rewardAmount;
```
3) 부정 수치 검증(선택)
```csharp
if (gold > 999999) AntiCheatSignals.Flag("Player_InvalidGold");
```
4) 치트 감지 후 처리
```csharp
AntiCheatSignals.RegisterCheatHandler(reason => {
    Debug.LogError($"치트 감지: {reason}");
    // 저장 차단/세이브 삭제/게임 종료 등
});
```
