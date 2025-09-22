using UnityEngine;

namespace GMLM.Event
{
    /// <summary>
    /// 이벤트 버스 시스템에서 사용되는 모든 이벤트의 기본 인터페이스입니다.
    /// 모든 커스텀 이벤트는 이 인터페이스를 구현해야 합니다.
    /// </summary>
    public interface IEvent
    {
        // 마커 인터페이스 - 이벤트의 식별을 위한 기본 타입으로 사용됩니다.
        // 필요한 경우 공통 메서드나 속성을 여기에 추가할 수 있습니다.
    }
} 