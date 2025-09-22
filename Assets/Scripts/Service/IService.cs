using UnityEngine;

namespace GMLM.Service
{
    /// <summary>
    /// 서비스 로케이터 패턴에서 사용되는 모든 서비스의 기본 인터페이스
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// 서비스 초기화 메서드
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 서비스 종료 메서드
        /// </summary>
        void Shutdown();
    }
} 