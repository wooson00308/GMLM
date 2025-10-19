using UnityEngine;
using GMLM.AI;

namespace GMLM.Game
{
    /// <summary>
    /// 대시 방향 계산을 위한 유틸리티 클래스
    /// EvadeDashAction의 로직을 재사용 가능하도록 분리
    /// </summary>
    public static class DashUtils
    {
        /// <summary>
        /// 타겟 접근용 대시 방향 계산 (투사체 회피 고려)
        /// </summary>
        /// <param name="selfPos">자신의 위치</param>
        /// <param name="targetPos">타겟 위치</param>
        /// <param name="mecha">메카 컴포넌트</param>
        /// <param name="sensor">투사체 센서 (null 가능)</param>
        /// <param name="blackboard">AI 블랙보드</param>
        /// <returns>대시 방향 (정규화됨)</returns>
        public static Vector3 CalculateApproachDirection(
            Vector3 selfPos, Vector3 targetPos, 
            Mecha mecha, MechaProjectileSensor sensor, 
            IBlackboard blackboard)
        {
            // 사거리 유지용 대시는 항상 타겟 직선 방향
            // 투사체 회피는 EvadeDashAction에서 담당
            Vector2 toTarget = ((Vector2)targetPos - (Vector2)selfPos).normalized;
            return toTarget;
        }
        
        /// <summary>
        /// 대시 거리와 속도를 고려한 접근 가능성 체크
        /// </summary>
        /// <param name="currentDistance">현재 거리</param>
        /// <param name="targetDistance">목표 거리</param>
        /// <param name="dashDistance">대시 거리</param>
        /// <returns>대시로 목표 거리에 도달 가능한지</returns>
        public static bool CanReachWithDash(float currentDistance, float targetDistance, float dashDistance)
        {
            float gap = currentDistance - targetDistance;
            return gap > dashDistance * 0.3f && gap <= dashDistance * 1.5f; // 너무 가깝거나 너무 멀면 대시 불필요
        }
    }
}
