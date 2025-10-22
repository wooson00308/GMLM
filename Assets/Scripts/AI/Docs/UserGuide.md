# AI - 사용자 가이드

## 1. 개요 (Introduction)

이 문서는 AI 라이브러리를 사용하여 AI를 조립하는 방법을 안내하는 가이드입니다. 이 라이브러리는 비헤이비어 트리(Behavior Tree)의 구조적인 장점과 유틸리티 AI(Utility AI)의 유연한 의사결정을 결합한 하이브리드 AI 시스템을 순수 C# 코드로 작성할 수 있도록 설계되었습니다.

**설계 철학:**
- **Code-First:** 모든 AI 로직은 코드로 정의됩니다. 복잡한 에디터나 ScriptableObject 설정 없이, 코드의 명확성과 재사용성에 집중합니다.
- **Lightweight:** 핵심 기능만 담아 가볍고 빠릅니다. 어떤 Unity 프로젝트에도 쉽게 이식하고 확장할 수 있습니다.
- **Versatile:** 범용적으로 설계되어, 특정 게임 장르에 구애받지 않고 다양한 종류의 AI를 제작할 수 있습니다.

---

## 2. 부품 목록 (Component Overview)

AI를 조립하기 전에, 우리가 사용할 주요 부품(클래스)들에 대해 알아봅시다.

| 부품명 (클래스) | 역할 | 비유 |
|---|---|---|
| `BehaviorTreeRunner` | AI의 생명과 심장. GameObject에 부착되어 AI를 실행시킵니다. | 파워 유닛 & 조종석 |
| `IBlackboard` | AI의 모든 데이터(상태, 타겟 등)를 저장하는 중앙 데이터 저장소. | 중앙처리장치(CPU) |
| `Node` | 모든 행동, 판단, 흐름의 가장 기본적인 추상 단위. | 기본 블록 |
| `ActionNode` | AI가 수행하는 실제 행동. (예: 이동, 공격) | 팔, 다리, 무기 |
| `CompositeNode` | 다른 노드들을 자식으로 묶어 구조를 만드는 노드. | 골격 프레임 |
| ┣ `Sequence` | 자식들을 순서대로 실행. (AND) | 순차 동작 기어 |
| ┣ `Selector` | 자식들 중 하나가 성공할 때까지 실행. (OR) | 조건 분기 기어 |
| ┗ `UtilitySelector` | **(핵심 부품)** 자식들의 가치를 판단해 가장 좋은 것을 실행. | AI 두뇌 |
| `DecoratorNode` | 자식 노드 하나를 감싸 부가 기능을 추가하는 노드. | 보조 장비, 장식 |
| ┗ `UtilityScorer` | **(핵심 부품)** 행동(Action)의 가치를 점수로 계산. `UtilitySelector`의 판단 근거. | 타겟팅 컴퓨터 |
| `Consideration` | **(핵심 부품)** 점수 계산을 위한 '상황 판단' 기준. (예: 거리, 체력) | 센서 |

---

## 3. 조립 순서 (Assembly Guide)

이제 이 부품들을 조립하여 실제 작동하는 AI를 만들어 보겠습니다.

### Step 1. 동력원 장착: `BehaviorTreeRunner` 생성

AI를 탑재할 GameObject에 새로운 C# 스크립트를 생성하고, `BehaviorTreeRunner`를 상속받게 하십시오.

```csharp
// 예시: MyGuardAI.cs
using UnityEngine;
using GMLM.AI;
using GMLM.AI.Core;
using GMLM.AI.Abstractions;

public class MyGuardAI : BehaviorTreeRunner
{
    // 이 메서드 안에서 AI의 모든 부품을 조립하게 됩니다.
    protected override Node InitializeTree(IBlackboard blackboard)
    {
        // 여기에 조립 코드를 작성합니다.
        return null; // 임시
    }
}
```

### Step 2. 센서 설계: `Consideration` 커스텀 제작

AI가 주변 상황을 인지할 수 있도록 `Consideration`을 만듭니다. `Consideration`은 특정 상황을 0과 1 사이의 값으로 반환해야 합니다.

```csharp
// 예시: TargetDistanceConsideration.cs
// 목표와의 거리를 판단하는 센서
public class TargetDistanceConsideration : Consideration
{
    private string _targetKey;
    private float _maxDistance;

    public TargetDistanceConsideration(IBlackboard blackboard, AnimationCurve responseCurve, string targetKey, float maxDistance) 
        : base(blackboard, responseCurve)
    {
        _targetKey = targetKey;
        _maxDistance = maxDistance;
    }

    protected override float GetRawValue()
    {
        var target = Blackboard.GetTransform(_targetKey);
        var self = Blackboard.GetTransform("self"); // 자신은 "self" 키로 등록되었다고 가정
        if (target == null || self == null) return 0f;

        float distance = Vector3.Distance(self.position, target.position);
        // 거리가 멀수록 0, 가까울수록 1에 가까운 값으로 정규화
        return Mathf.Clamp01(1 - (distance / _maxDistance));
    }
}
```

### Step 3. 행동 모듈 조립: `ActionNode` 커스텀 제작

AI가 수행할 구체적인 행동을 `ActionNode`를 상속받아 만듭니다.

```csharp
// 예시: AttackTargetAction.cs
// 대상을 공격하는 행동
public class AttackTargetAction : ActionNode
{
    private string _targetKey;

    public AttackTargetAction(IBlackboard blackboard, string targetKey) : base(blackboard)
    {
        _targetKey = targetKey;
    }

    public override async UniTask<NodeStatus> Execute()
    {
        var target = Blackboard.GetGameObject(_targetKey);
        if (target == null)
        {
            Debug.Log("대상이 없어 공격에 실패했습니다.");
            return NodeStatus.Failure;
        }

        Debug.Log($"{target.name}을(를) 공격합니다!");
        // 여기에 실제 공격 로직 (애니메이션, 발사체 생성 등) 추가
        // 지금은 즉시 성공을 반환
        return NodeStatus.Success;
    }
}
```

### Step 4. 최종 조립: `InitializeTree`에서 모든 부품 결합

이제 `MyGuardAI.cs`의 `InitializeTree` 메서드 안에서 모든 부품을 조립합니다.

```csharp
// MyGuardAI.cs의 일부
protected override Node InitializeTree(IBlackboard blackboard)
{
    // 0. AI 자신과 목표물 등록 (외부에서 설정해준다고 가정)
    // 이 예제에서는 임시로 FindObjectOfType을 사용합니다.
    var player = FindObjectOfType<Player>(); // Player 클래스가 있다고 가정
    blackboard.SetTransform("self", this.transform);
    if(player != null) blackboard.SetGameObject("player", player.gameObject);

    // 1. 행동(Action) 제작
    var attackAction = new AttackTargetAction(blackboard, "player");
    // (대기, 순찰 등 다른 행동들도 마찬가지로 제작...)
    var idleAction = new IdleAction(blackboard); // IdleAction이 있다고 가정

    // 2. 센서(Consideration) 및 반응 곡선(ResponseCurve) 제작
    // 거리가 가까울수록 점수가 급격히 오르는 곡선
    var distanceCurve = new AnimationCurve(
        new Keyframe(0, 0.1f), // 거리가 멀면(raw=0) 점수가 낮음
        new Keyframe(0.8f, 0.8f),
        new Keyframe(1, 1f)  // 거리가 가까우면(raw=1) 점수가 높음
    );
    var distanceConsideration = new TargetDistanceConsideration(blackboard, distanceCurve, "player", 20f);
    
    // (시야, 체력 등 다른 센서들도 마찬가지로 제작...)

    // 3. 타겟팅 컴퓨터(UtilityScorer)에 행동과 센서 장착
    var attackScorer = new UtilityScorer(
        attackAction,
        new List<Consideration> { distanceConsideration }
    );
    // 대기 행동은 기본 점수만 갖도록 설정
    var idleScorer = new UtilityScorer(idleAction, new List<Consideration>());

    // 4. 두뇌(UtilitySelector)에 타겟팅 컴퓨터들 연결
    var rootNode = new UtilitySelector(
        new List<UtilityScorer>
        {
            attackScorer,
            idleScorer
        }
    );

    // 5. 완성된 최상위 노드 반환
    return rootNode;
}
```

---

## 4. 작동 및 튜닝 (Operation & Tuning)

- 위와 같이 조립을 마치고 Unity 씬을 실행하면, `MyGuardAI`는 매 프레임 `UtilitySelector`를 통해 '공격'과 '대기' 행동의 가치를 비교합니다.
- 플레이어와의 거리가 가까워지면 `distanceConsideration`의 점수가 `distanceCurve`에 따라 높아지고, `attackScorer`의 최종 점수도 따라 올라갑니다.
- `attackScorer`의 점수가 `idleScorer`의 점수보다 높아지는 순간, AI는 '공격' 행동을 선택하게 됩니다.
- AI의 반응성을 바꾸고 싶다면, `MyGuardAI.cs` 코드에서 `AnimationCurve`의 키프레임 값들을 수정하여 쉽게 튜닝할 수 있습니다.

---

## 5. 고급 기능 (Advanced Features)

### 5.1 타입 안전한 Blackboard 사용

기존 string 기반 API와 함께 타입 안전한 API도 제공됩니다:

```csharp
// 기존 방식 (여전히 지원)
blackboard.SetGameObject("player", player.gameObject);
var player = blackboard.GetGameObject("player");

// 새로운 타입 안전 방식
var playerKey = new BlackboardKey<GameObject>("player");
blackboard.SetGameObject(playerKey, player.gameObject);
var player = blackboard.GetGameObject(playerKey);
```

### 5.2 커스텀 점수 집계 전략

기존 enum 방식 외에 커스텀 집계 전략을 사용할 수 있습니다:

```csharp
// 기존 방식 (여전히 지원)
var attackScorer = new UtilityScorer(
    attackAction,
    new List<Consideration> { distanceConsideration },
    ScoreAggregationType.Average
);

// 새로운 전략 패턴 방식
var customStrategy = new WeightedAverageStrategy(); // 커스텀 전략
var attackScorer = new UtilityScorer(
    attackAction,
    new List<Consideration> { distanceConsideration },
    customStrategy
);
```

### 5.3 개선된 비동기 실행

예외 처리가 가능한 새로운 비동기 API:

```csharp
// 기존 방식 (여전히 지원)
tree.Tick(); // async void

// 새로운 방식 (예외 처리 가능)
try
{
    await tree.TickAsync(); // UniTask 반환
}
catch (Exception ex)
{
    Debug.LogError($"AI 실행 중 오류: {ex.Message}");
}
``` 