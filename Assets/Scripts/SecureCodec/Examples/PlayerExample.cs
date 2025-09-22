using UnityEngine;

/// <summary>
/// 플레이어 예시 - 주요 스탯과 재화를 메모리 해킹으로부터 보호
/// </summary>
public class PlayerExample : MonoBehaviour
{
    [Header("기본 스탯 (메모리 보호됨)")]
    [SerializeField] private SecureInt level = 1;
    [SerializeField] private SecureInt maxHp = 100;
    [SerializeField] private SecureInt currentHp = 100;
    [SerializeField] private SecureInt maxMp = 50;
    [SerializeField] private SecureInt currentMp = 50;
    
    [Header("능력치")]
    [SerializeField] private SecureInt strength = 10;
    [SerializeField] private SecureInt intelligence = 10;
    [SerializeField] private SecureInt agility = 10;
    [SerializeField] private SecureInt luck = 5;
    
    [Header("진행도")]
    [SerializeField] private SecureInt currentExp = 0;
    [SerializeField] private SecureInt expToNextLevel = 100;
    [SerializeField] private SecureInt skillPoints = 0;
    
    [Header("재화 (핵심 보호 대상)")]
    [SerializeField] private SecureInt gold = 1000;
    [SerializeField] private SecureInt gems = 10;
    [SerializeField] private SecureInt energy = 100;
    
    [Header("상태")]
    [SerializeField] private SecureBool isAlive = true;
    [SerializeField] private SecureFloat hpRegenRate = 0.5f; // 초당 회복량
    [SerializeField] private SecureFloat mpRegenRate = 1.0f;

    void Start()
    {
        Debug.Log($"플레이어 생성 - Lv.{level} HP:{currentHp}/{maxHp} MP:{currentMp}/{maxMp}");
        Debug.Log($"초기 자산 - 골드:{gold} 젬:{gems}");
    }

    // 외부에서 초기 체력/마나/레벨 설정 등 간단 초기화 지원
    public void Initialize(int startLevel = 1)
    {
        level = Mathf.Max(1, startLevel);
        maxHp = 80 + (level * 20);
        maxMp = 40 + (level * 10);
        currentHp = maxHp;
        currentMp = maxMp;
        expToNextLevel = level * 100 + 50;
    }

    void Update()
    {
        if (!isAlive) return;
        
        // 자동 회복
        if (currentHp < maxHp)
        {
            float healAmount = hpRegenRate * Time.deltaTime;
            currentHp.Value = Mathf.Min(maxHp, currentHp + Mathf.FloorToInt(healAmount));
        }
        
        if (currentMp < maxMp)
        {
            float manaAmount = mpRegenRate * Time.deltaTime;
            currentMp.Value = Mathf.Min(maxMp, currentMp + Mathf.FloorToInt(manaAmount));
        }
    }

    public void TakeDamage(int damage)
    {
        if (!isAlive) return;
        
        currentHp.Value -= damage;
        Debug.Log($"플레이어가 {damage} 데미지를 받았다! (HP: {currentHp}/{maxHp})");
        
        if (currentHp <= 0)
        {
            Die();
        }
    }
    
    public void ConsumeMp(int cost)
    {
        if (currentMp >= cost)
        {
            currentMp.Value -= cost;
            Debug.Log($"마나 {cost} 소모 (MP: {currentMp}/{maxMp})");
        }
        else
        {
            Debug.Log("마나가 부족합니다!");
        }
    }
    
    public void GainExp(int amount)
    {
        currentExp.Value += amount;
        Debug.Log($"경험치 +{amount} (현재: {currentExp}/{expToNextLevel})");
        
        // 레벨업 체크
        while (currentExp >= expToNextLevel)
        {
            LevelUp();
        }
    }
    
    void LevelUp()
    {
        currentExp.Value -= expToNextLevel;
        level.Value += 1;
        
        // 스탯 증가
        maxHp.Value += 20;
        maxMp.Value += 10;
        strength.Value += 2;
        intelligence.Value += 2;
        agility.Value += 1;
        skillPoints.Value += 3;
        
        // 체력/마나 전체 회복
        currentHp = maxHp;
        currentMp = maxMp;
        
        // 다음 레벨 필요 경험치 증가
        expToNextLevel.Value = level * 100 + 50;
        
        Debug.Log($"🎉 레벨업! Lv.{level} (스킬포인트 +3)");
    }
    
    public bool SpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold.Value -= amount;
            Debug.Log($"골드 {amount} 소모 (잔액: {gold})");
            return true;
        }
        else
        {
            Debug.Log("골드가 부족합니다!");
            return false;
        }
    }
    
    public void GainGold(int amount)
    {
        gold.Value += amount;
        Debug.Log($"골드 +{amount} (총: {gold})");
    }
    
    public bool SpendGems(int amount)
    {
        if (gems >= amount)
        {
            gems.Value -= amount;
            Debug.Log($"젬 {amount} 소모 (잔액: {gems})");
            return true;
        }
        else
        {
            Debug.Log("젬이 부족합니다!");
            return false;
        }
    }
    
    public void GainGems(int amount)
    {
        gems.Value += amount;
        Debug.Log($"젬 +{amount} (총: {gems})");
    }
    
    void Die()
    {
        isAlive = false;
        Debug.Log("💀 플레이어 사망!");
        
        // 사망 페널티 (골드 일부 손실)
        int goldLoss = gold / 10;
        gold.Value -= goldLoss;
        Debug.Log($"사망 페널티: 골드 -{goldLoss}");
    }
    
    public void Revive()
    {
        if (isAlive) return;
        
        isAlive = true;
        currentHp = maxHp / 2;
        currentMp = maxMp / 2;
        Debug.Log("플레이어 부활!");
    }
    
    // 치트 방지 검증
    void ValidatePlayerData()
    {
        // 비정상적인 수치 감지
        if (gold > 999999)
        {
            AntiCheatSignals.Flag("Player_InvalidGold");
        }
        
        if (gems > 9999)
        {
            AntiCheatSignals.Flag("Player_InvalidGems");
        }
        
        if (level > 100)
        {
            AntiCheatSignals.Flag("Player_InvalidLevel");
        }
        
        // HP가 최대치를 초과하는지 확인
        if (currentHp > maxHp)
        {
            AntiCheatSignals.Flag("Player_InvalidHP");
            currentHp = maxHp;
        }
    }
    
    // UI 표시용
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 220, 350, 300));
        
        GUILayout.Label($"=== 플레이어 정보 ===");
        GUILayout.Label($"레벨: {level} (경험치: {currentExp}/{expToNextLevel})");
        GUILayout.Label($"HP: {currentHp}/{maxHp}  MP: {currentMp}/{maxMp}");
        GUILayout.Label($"STR: {strength}  INT: {intelligence}  AGI: {agility}  LUK: {luck}");
        GUILayout.Label($"스킬포인트: {skillPoints}");
        GUILayout.Space(10);
        
        GUILayout.Label($"=== 재화 ===");
        GUILayout.Label($"💰 골드: {gold:N0}");
        GUILayout.Label($"💎 젬: {gems}");
        GUILayout.Label($"⚡ 에너지: {energy}");
        GUILayout.Label($"상태: {(isAlive ? "생존" : "💀 사망")}");
        
        GUILayout.EndArea();
    }
    
    // 디버그용 테스트 함수들
    [ContextMenu("경험치 획득 테스트")]
    void DebugGainExp() => GainExp(150);
    
    [ContextMenu("골드 획득 테스트")]
    void DebugGainGold() => GainGold(500);
    
    [ContextMenu("데미지 테스트")]
    void DebugTakeDamage() => TakeDamage(30);
}
