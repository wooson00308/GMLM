using UnityEngine;

/// <summary>
/// 메모리 해킹 및 치트 시도를 감지했을 때 신호를 발생시키는 시스템
/// </summary>
public static class AntiCheatSignals
{
    /// <summary>
    /// 치트 감지 시 호출되는 이벤트
    /// </summary>
    public static System.Action<string> OnCheatDetected;
    
    /// <summary>
    /// 치트 시도를 플래그로 표시하고 로그를 남김
    /// </summary>
    /// <param name="reason">치트 감지 사유</param>
    public static void Flag(string reason)
    {
        // 개발 중에는 경고로 표시
        Debug.LogWarning($"[ANTI-CHEAT] 메모리 변조 감지: {reason}");
        
        // 릴리스에서는 더 강력한 대응 가능 (게임 종료, 서버 신고 등)
        #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        Debug.LogError($"[SECURITY VIOLATION] {reason} - 게임을 종료합니다.");
        #endif
        
        // 외부 시스템에 알림
        OnCheatDetected?.Invoke(reason);
        
        // TODO: 필요시 추가 대응 로직 (통계 수집, 서버 신고 등)
    }
    
    /// <summary>
    /// 치트 감지 이벤트 리스너 등록
    /// </summary>
    public static void RegisterCheatHandler(System.Action<string> handler)
    {
        OnCheatDetected += handler;
    }
    
    /// <summary>
    /// 치트 감지 이벤트 리스너 제거
    /// </summary>
    public static void UnregisterCheatHandler(System.Action<string> handler)
    {
        OnCheatDetected -= handler;
    }
}
