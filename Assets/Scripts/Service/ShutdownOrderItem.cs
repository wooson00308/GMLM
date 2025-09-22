using System;

namespace GMLM.Service
{
    /// <summary>
    /// 서비스 종료 우선순위 지정을 위한 직렬화 가능 클래스
    /// </summary>
    [System.Serializable]
    public class ShutdownOrderItem
    {
        public System.Type serviceType;
        public int shutdownPriority;
    }
} 