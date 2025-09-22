using UnityEngine;

/// <summary>
/// 게임 매니저 예시 - 스테이지 진행도를 메모리 해킹으로부터 보호
/// </summary>
public class GameManagerExample : MonoBehaviour
{
    [Header("스테이지 정보 (메모리 보호됨)")]
    [SerializeField] private SecureInt currentStage = 1;
    [SerializeField] private SecureInt maxUnlockedStage = 1;
    [SerializeField] private SecureBool isBossStage = false;
    
    [Header("점수 정보")]
    [SerializeField] private SecureInt totalScore = 0;
    [SerializeField] private SecureFloat stageTimeBonus = 1.0f;

    [Header("오브젝트 프리팹/레퍼런스")]
    [SerializeField] private PlayerExample playerPrefab;
    [SerializeField] private MonsterExample monsterPrefab;
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private Transform monsterSpawn;

    private PlayerExample player;
    private MonsterExample monster;

    void Start()
    {
        Debug.Log($"게임 시작 - 현재 스테이지: {currentStage}");
        
        // 치트 감지 핸들러 등록
        AntiCheatSignals.RegisterCheatHandler(OnCheatDetected);

        // 자동 초기화: 플레이어/몬스터 스폰 및 연결
        AutoBootstrap();
    }

    public void CompleteStage()
    {
        Debug.Log($"스테이지 {currentStage} 클리어!");
        
        // 스테이지 진행
        currentStage.Value += 1;
        
        // 최대 해금 스테이지 업데이트
        if (currentStage > maxUnlockedStage)
        {
            maxUnlockedStage = currentStage;
        }
        
        // 보스 스테이지 체크 (5의 배수)
        isBossStage = (currentStage % 5 == 0);
        
        // 점수 계산 (시간 보너스 적용)
        int stageScore = 1000 + (int)(500 * stageTimeBonus);
        totalScore.Value += stageScore;
        
        Debug.Log($"다음 스테이지: {currentStage} (보스: {isBossStage})");
        Debug.Log($"총 점수: {totalScore}");
    }

    void AutoBootstrap()
    {
        // 플레이어 생성 또는 찾기
        if (player == null)
        {
            player = FindObjectOfType<PlayerExample>();
            if (player == null && playerPrefab != null)
            {
                var pos = playerSpawn ? playerSpawn.position : Vector3.zero;
                player = Instantiate(playerPrefab, pos, Quaternion.identity);
                player.name = "PlayerExample(Auto)";
            }
        }

        // 몬스터 생성
        SpawnMonsterForStage(currentStage);
    }

    void SpawnMonsterForStage(int stage)
    {
        // 기존 몬스터 정리
        if (monster != null)
        {
            Destroy(monster.gameObject);
            monster = null;
        }

        if (monsterPrefab == null) return;
        var pos = monsterSpawn ? monsterSpawn.position : new Vector3(3, 0, 0);
        monster = Instantiate(monsterPrefab, pos, Quaternion.identity);
        monster.name = $"MonsterExample(Stage {stage})";
        monster.Initialize(Mathf.Max(1, (int)stage));
        monster.OnKilled += OnMonsterKilled;
    }

    void OnMonsterKilled(int exp, int gold)
    {
        if (player != null)
        {
            player.GainExp(exp);
            player.GainGold(gold);
        }
        // 다음 몬스터 스폰(간단 루프)
        SpawnMonsterForStage(currentStage);
    }
    
    public void SetStageDirectly(int stage)
    {
        // 일반적인 방법으로는 해금된 스테이지까지만 이동 가능
        if (stage <= maxUnlockedStage)
        {
            currentStage = stage;
            isBossStage = (stage % 5 == 0);
            Debug.Log($"스테이지 {stage}로 이동");
        }
        else
        {
            Debug.LogWarning("아직 해금되지 않은 스테이지입니다!");
        }
    }
    
    // 디버그용 - 에디터에서만 사용
    [ContextMenu("스테이지 강제 진행")]
    void DebugAdvanceStage()
    {
        #if UNITY_EDITOR
        CompleteStage();
        #endif
    }
    
    private void OnCheatDetected(string reason)
    {
        Debug.LogError($"치트 감지됨: {reason}");
        
        // 게임 데이터 리셋이나 경고 메시지 표시 등
        if (reason.Contains("GameManager"))
        {
            Debug.LogError("스테이지 데이터 조작 시도 감지! 게임을 재시작합니다.");
        }
    }
    
    void OnDestroy()
    {
        AntiCheatSignals.UnregisterCheatHandler(OnCheatDetected);
    }
    
    // 인스펙터에서 확인용 (실제 값은 암호화되어 있음)
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"현재 스테이지: {currentStage}");
        GUILayout.Label($"최대 해금: {maxUnlockedStage}");
        GUILayout.Label($"보스 스테이지: {isBossStage}");
        GUILayout.Label($"총 점수: {totalScore}");
        GUILayout.Label($"시간 보너스: {stageTimeBonus:F1}x");
        GUILayout.EndArea();

        // 간단 조작 UI
        GUILayout.BeginArea(new Rect(10, 220, 260, 120));
        if (GUILayout.Button("스테이지 클리어")) CompleteStage();
        if (GUILayout.Button("몬스터 리스폰")) SpawnMonsterForStage(currentStage);
        GUILayout.EndArea();
    }
}
