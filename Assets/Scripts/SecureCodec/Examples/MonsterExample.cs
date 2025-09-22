using UnityEngine;

/// <summary>
/// 몬스터 예시 - 스탯을 메모리 해킹으로부터 보호
/// </summary>
public class MonsterExample : MonoBehaviour
{
    [Header("기본 스탯 (메모리 보호됨)")]
    [SerializeField] private SecureInt maxHp;
    [SerializeField] private SecureInt currentHp;
    [SerializeField] private SecureInt attack;
    [SerializeField] private SecureInt defense;
    [SerializeField] private SecureFloat moveSpeed = 2.0f;
    
    [Header("상태 정보")]
    [SerializeField] private SecureBool isDead = false;
    [SerializeField] private SecureBool isStunned = false;
    [SerializeField] private SecureFloat stunDuration = 0f;
    
    [Header("보상 정보")]  
    [SerializeField] private SecureInt expReward;
    [SerializeField] private SecureInt goldReward;

    // 처치 시 보상 알림 (exp, gold)
    public event System.Action<int, int> OnKilled;

    void Start()
    {
        // 외부에서 초기화하지 않았다면 기본 초기화
        if (maxHp <= 0)
            InitializeStats();
        Debug.Log($"몬스터 생성 - HP: {currentHp}/{maxHp}, 공격: {attack}, 방어: {defense}");
    }
    
    void InitializeStats()
    {
        // 랜덤 기본값
        ApplyLevel(Random.Range(1, 10));
    }

    // 외부에서 레벨을 지정해 초기화할 수 있게 제공
    public void Initialize(int level)
    {
        ApplyLevel(Mathf.Max(1, level));
    }

    void ApplyLevel(int level)
    {
        maxHp = 50 + (level * 20);
        currentHp = maxHp;
        attack = 10 + (level * 3);
        defense = 5 + (level * 2);
        expReward = level * 10;
        goldReward = level * 5;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        // 방어력 적용
        int actualDamage = Mathf.Max(1, damage - defense);
        currentHp.Value -= actualDamage;
        
        Debug.Log($"몬스터가 {actualDamage} 데미지를 받았다! (남은 HP: {currentHp}/{maxHp})");
        
        // 체력 확인
        if (currentHp <= 0)
        {
            Die();
        }
    }
    
    public int GetAttackDamage()
    {
        if (isDead || isStunned) return 0;
        
        // 공격력에 약간의 랜덤성 추가
        float variance = Random.Range(0.8f, 1.2f);
        return Mathf.RoundToInt(attack * variance);
    }
    
    public void Heal(int amount)
    {
        if (isDead) return;
        
        currentHp.Value = Mathf.Min(maxHp, currentHp + amount);
        Debug.Log($"몬스터 회복! HP: {currentHp}/{maxHp}");
    }
    
    public void Stun(float duration)
    {
        if (isDead) return;
        
        isStunned = true;
        stunDuration = duration;
        Debug.Log($"몬스터 기절! ({duration}초)");
    }
    
    void Update()
    {
        // 기절 시간 처리
        if (isStunned && stunDuration > 0)
        {
            stunDuration.Value -= Time.deltaTime;
            if (stunDuration <= 0)
            {
                isStunned = false;
                Debug.Log("몬스터 기절 해제!");
            }
        }
    }
    
    void Die()
    {
        isDead = true;
        Debug.Log($"몬스터 처치! 보상 - 경험치: {expReward}, 골드: {goldReward}");
        
        // 실제 게임에서는 여기서 보상 지급
        // PlayerExample의 GainExp, GainGold 호출
        OnKilled?.Invoke(expReward, goldReward);
        
        gameObject.SetActive(false);
    }
    
    // 치트 방지를 위한 스탯 검증
    void ValidateStats()
    {
        // 비정상적인 스탯 감지
        if (currentHp > maxHp * 2) // HP가 최대치의 2배를 넘으면
        {
            AntiCheatSignals.Flag("Monster_InvalidHP");
            currentHp = maxHp; // 정상 수치로 복구
        }
        
        if (attack > 1000) // 공격력이 비정상적으로 높으면
        {
            AntiCheatSignals.Flag("Monster_InvalidAttack");
        }
    }
    
    // 인스펙터 확인용
    void OnGUI()
    {
        if (!gameObject.activeInHierarchy) return;
        
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2);
        if (screenPos.z > 0)
        {
            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 60, 100, 60),
                $"HP: {currentHp}/{maxHp}\n" +
                $"ATK: {attack} DEF: {defense}\n" +
                $"{(isStunned ? "기절!" : "")}");
        }
    }
    
    // 디버그용
    [ContextMenu("데미지 테스트")]
    void DebugTakeDamage()
    {
        TakeDamage(25);
    }
    
    [ContextMenu("기절 테스트")]  
    void DebugStun()
    {
        Stun(3f);
    }
}
