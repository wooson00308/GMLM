using System;
using System.Collections.Generic;
using UnityEngine;
using GMLM.Service;

namespace GMLM.Event
{
    /// <summary>
    /// 이벤트 버스 시스템의 핵심 클래스입니다.
    /// 이벤트 구독, 발행, 해제 기능을 제공합니다.
    /// </summary>
    public class EventService : IService
    {
        // 각 이벤트 타입별 리스너 목록을 관리하는 딕셔너리
        private readonly Dictionary<Type, List<Delegate>> _eventListeners = new Dictionary<Type, List<Delegate>>();
        
        private readonly Dictionary<Type, string> _eventTypeNames = new Dictionary<Type, string>();
        private bool _enableLogging = false;

        public void Initialize()
        {

        }

        public void Shutdown()
        {
        }

        #region 이벤트 구독 및 해제

        /// <summary>
        /// 지정된 이벤트 타입에 대한 리스너를 등록합니다.
        /// </summary>
        /// <typeparam name="T">구독할 이벤트 타입 (IEvent를 구현해야 함)</typeparam>
        /// <param name="listener">이벤트 발생 시 호출될 콜백 메서드</param>
        public void Subscribe<T>(Action<T> listener) where T : IEvent
        {
            Type eventType = typeof(T);
            
            if (!_eventListeners.ContainsKey(eventType))
            {
                _eventListeners[eventType] = new List<Delegate>();
                _eventTypeNames[eventType] = eventType.Name;
            }
            
            if (!_eventListeners[eventType].Contains(listener))
            {
                _eventListeners[eventType].Add(listener);
                
                if (_enableLogging)
                    Debug.Log($"[EventBus] 구독 추가: {eventType.Name}, 총 리스너: {_eventListeners[eventType].Count}");
            }
        }

        /// <summary>
        /// 지정된 이벤트 타입에 대한 리스너 등록을 해제합니다.
        /// </summary>
        /// <typeparam name="T">구독 해제할 이벤트 타입</typeparam>
        /// <param name="listener">해제할 리스너</param>
        public void Unsubscribe<T>(Action<T> listener) where T : IEvent
        {
            Type eventType = typeof(T);
            
            if (_eventListeners.ContainsKey(eventType))
            {
                _eventListeners[eventType].Remove(listener);
                
                if (_enableLogging)
                    Debug.Log($"[EventBus] 구독 해제: {eventType.Name}, 남은 리스너: {_eventListeners[eventType].Count}");
                
                // 리스너가 없으면 딕셔너리에서 해당 이벤트 타입 제거
                if (_eventListeners[eventType].Count == 0)
                {
                    _eventListeners.Remove(eventType);
                    _eventTypeNames.Remove(eventType);
                }
            }
        }

        #endregion

        #region 이벤트 발행

        /// <summary>
        /// 지정된 이벤트를 발행합니다.
        /// </summary>
        /// <typeparam name="T">발행할 이벤트 타입</typeparam>
        /// <param name="eventData">이벤트 데이터</param>
        public void Publish<T>(T eventData) where T : IEvent
        {
            Type eventType = typeof(T);
            
            if (!_eventListeners.ContainsKey(eventType))
            {
                if (_enableLogging)
                    Debug.LogWarning($"[EventBus] 이벤트 발행: {eventType.Name} - 리스너가 없습니다.");
                return;
            }
            
            if (_enableLogging)
                Debug.Log($"[EventBus] 이벤트 발행: {eventType.Name}, 리스너 수: {_eventListeners[eventType].Count}");
            
            // 리스너 목록을 복사하여 순회 중 변경 방지
            var listeners = new List<Delegate>(_eventListeners[eventType]);
            
            foreach (var listener in listeners)
            {
                try
                {
                    var callback = listener as Action<T>;
                    callback?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] 이벤트 처리 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        #endregion

        #region 유틸리티 메서드

        /// <summary>
        /// 모든 이벤트 구독을 제거합니다.
        /// </summary>
        public void Clear()
        {
            _eventListeners.Clear();
            _eventTypeNames.Clear();
            
            if (_enableLogging)
                Debug.Log("[EventBus] 모든 이벤트 리스너가 제거되었습니다.");
        }

        /// <summary>
        /// 로깅 활성화 여부를 설정합니다.
        /// </summary>
        /// <param name="enable">로깅 활성화 여부</param>
        public void SetLogging(bool enable)
        {
            _enableLogging = enable;
            Debug.Log($"[EventBus] 이벤트 로깅 {(enable ? "활성화" : "비활성화")}");
        }

        /// <summary>
        /// 현재 등록된 모든 이벤트 타입과 리스너 수를 반환합니다.
        /// </summary>
        /// <returns>이벤트 타입별 리스너 수</returns>
        public Dictionary<string, int> GetRegisteredEvents()
        {
            var result = new Dictionary<string, int>();
            
            foreach (var pair in _eventListeners)
            {
                string eventName = _eventTypeNames[pair.Key];
                result[eventName] = pair.Value.Count;
            }
            
            return result;
        }

        #endregion
    }
} 