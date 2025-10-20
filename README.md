# GMLM 프로젝트 안내

## 프로젝트 개요
GMLM은 탑다운 시점에서 거대한 메카를 조종하며 전투를 벌이는 액션 게임 프로토타입입니다. 플레이어와 AI 파일럿이 같은 전장을 공유하며, 각 메카는 무기 구성과 파일럿의 전투 성향(Combat Style)에 따라 다른 움직임과 전술을 보입니다. 프로젝트는 빠른 전투 전개, 아머드 코어(AC) 스타일의 스태거 시스템, 그리고 폭발적인 이펙트를 강조하는 방향으로 제작되고 있습니다.

- **플랫폼**: PC (키보드/마우스 & 게임패드 입력 지원)
- **엔진 버전**: Unity 6000.2.6f2 (Unity 6)
- **렌더링 파이프라인**: Universal Render Pipeline(URP)

## 현재 구현된 주요 기능
### 전투 & 메카 시스템
- **메카 스탯**: 체력, 이동/회전 속도, 대시 거리, 에너지(부스트) 리소스 관리 등 세부 스탯을 노출하여 인스펙터에서 조정 가능.【F:Assets/Scripts/Game/Mecha.cs†L38-L103】
- **스태거(정지) 시스템**: 피격 시 스태거 수치가 증가하며, 임계점 돌파 시 일정 시간 동안 스태거 상태가 되어 추가 피해를 받습니다. 자연 회복 지연과 회복 배율 설정을 지원합니다.【F:Assets/Scripts/Game/Mecha.cs†L44-L69】【F:Assets/Scripts/Game/Mecha.cs†L111-L132】
- **대시(회피) 메커닉**: 일정 거리/속도로 순간 이동하며 애니메이션 및 파티클 업데이트가 연동됩니다.【F:Assets/Scripts/Game/Mecha.cs†L135-L206】
- **무기 시스템**: 사거리 유지, 목표 회전, 선딜레이, 버스트 발사, 탄창/재장전, 호밍·산탄 옵션을 포함한 다양한 무기 파라미터를 제공하며 Projectile 프리팹과 연동됩니다.【F:Assets/Scripts/Game/Weapon/Weapon.cs†L13-L141】

### AI & 데이터 드리븐 구조
- **파일럿 AI**: Behavior Tree 기반 러너로 구축되어 타겟 탐색, 거리 유지, 회피 대시, 공격 행동을 병렬로 수행합니다.【F:Assets/Scripts/Game/AI/PilotAI.cs†L8-L44】
- **Behavior Tree 유틸리티**: `BehaviorTreeBuilder`, `Parallel`, `Sequence` 등 커스텀 노드를 이용해 손쉽게 행동 패턴을 조합할 수 있습니다.【F:Assets/Scripts/AI/BehaviorTreeBuilder.cs†L1-L120】
- **파일럿 데이터 테이블**: JSON 기반 데이터(`PilotDataTable`)로 파일럿의 이름, 설명, 전투 성향을 로드하여 게임 오브젝트에 주입합니다.【F:Assets/Scripts/Data/PilotData.cs†L8-L33】【F:Assets/Scripts/Data/PilotDataTable.cs†L12-L70】

### 서비스 & 인프라
- **Service Locator 패턴**: `ServiceManager`가 씬 전환과 함께 서비스 생명주기를 관리하며, 이벤트 버스 등 핵심 서비스를 자동 등록합니다.【F:Assets/Scripts/Service/ServiceManager.cs†L9-L121】【F:Assets/Scripts/Service/ServiceManager.cs†L123-L175】
- **이벤트 시스템**: `EventService`와 `IEventData` 인터페이스를 통해 느슨한 결합의 메시징을 지원합니다.【F:Assets/Scripts/Event/EventService.cs†L6-L107】
- **보안형 값 래퍼**: `SecureCodec` 모듈이 수치값을 암호화해 메모리 변조(치트)에 대비합니다.【F:Assets/Scripts/SecureCodec/Secure.cs†L6-L162】

### 입력 & 씬 구성
- **신형 Input System**: 이동, 공격, 대시, 상호작용 등 PC 및 게임패드에 대응하는 액션 맵을 제공하며 런타임 재바인딩이 가능하도록 설계되어 있습니다.【F:Assets/InputSystem_Actions.inputactions†L1-L120】
- **씬 플로우**: `0. bootstrap` → `1. lobby` → `2. game` 씬으로 이어지는 구조로, 부팅 씬에서 서비스 초기화 후 로비/전투 씬을 로드하는 방식입니다.【F:Assets/Scenes/0. bootstrap.unity.meta†L1-L16】

## 폴더 구조 하이라이트
```
GMLM/
├─ Assets/
│  ├─ Scenes/                 # Bootstrap, Lobby, Game 씬
│  ├─ Scripts/
│  │  ├─ Game/                # 메카, 파일럿, 무기, 투사체 로직
│  │  ├─ AI/                  # Behavior Tree 런타임 및 노드
│  │  ├─ Data/                # 데이터 테이블 & JSON 로더
│  │  ├─ Event/Service        # 이벤트 버스, 서비스 로케이터
│  │  └─ SecureCodec/         # 보안 타입 래퍼 (Anti-Cheat)
│  ├─ Prefabs/                # 메카, 무기, FX 등 프리팹
│  ├─ Resources/              # 데이터 테이블 리소스
│  └─ Plugins & Packages/     # Odin Inspector, Modern UI Pack 등 서드파티
├─ Docs/                      # 기획/디자인 문서 (작성 예정)
├─ Packages/                  # Unity 패키지 매니페스트
└─ ProjectSettings/           # Unity 프로젝트 설정
```

## 개발 환경 & 빌드
1. **필수 요구사항**
   - Unity Hub와 Unity 6000.2.6f2 설치
   - Git LFS 사용 (대형 에셋 포함 가능성)
2. **프로젝트 열기**
   - `GMLM` 폴더를 Unity Hub에 추가 후 열기
   - 최초 열람 시 URP 리소스가 자동 설정되며, 필요 시 `ProjectSettings/Graphics`에서 확인
3. **입력 시스템 세팅**
   - `InputSystem_Actions.inputactions` 파일을 더블 클릭 후, 필요한 액션을 인풋 모듈에 바인딩
4. **플레이**
   - `0. bootstrap` 씬을 열고 Play 버튼 실행 → 서비스 초기화 후 로비 씬이 로드됩니다.

## 향후 작업 아이디어
- 메카/무기 밸런스 데이터 테이블 확장 및 UI 연동
- 로비 UI 완성도 향상 및 파일럿 선택 화면 구현
- PVE 웨이브 또는 PVP 시뮬레이션 게임모드 프로토타입 추가
- 네트워크 플레이(예: Netcode for GameObjects) 검토

## 라이선스 및 서드파티 에셋
- 프로젝트 내부에는 Modern UI Pack, TextMeshPro, URP 샘플 등 서드파티 리소스가 포함되어 있습니다. 상용 배포 전 각 에셋의 라이선스를 확인하세요.
- 코드와 데이터에 대한 최종 라이선스 정책은 아직 정의되지 않았습니다.

## 문의
이 문서는 초기 프로덕션 스냅샷을 기반으로 작성되었습니다. 추가 설명이나 업데이트가 필요하면 이슈 트래커 또는 팀 커뮤니케이션 채널을 통해 문의해 주세요.
