using System;
using System.Collections.Generic;
using UnityEngine;

namespace GMLM.Service
{
    /// <summary>
    /// 서비스 로케이터 패턴을 구현한 클래스
    /// 다양한 서비스에 대한 중앙 액세스 포인트를 제공합니다.
    /// </summary>
    public class ServiceLocator
    {
        private static ServiceLocator _instance;
        
        /// <summary>
        /// 싱글톤 인스턴스에 접근하기 위한 프로퍼티
        /// </summary>
        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServiceLocator();
                }
                return _instance;
            }
        }
        
        // 서비스 저장소
        private readonly Dictionary<Type, IService> _services = new Dictionary<Type, IService>();
        
        // 외부에서 생성 방지
        private ServiceLocator() { }
        
        /// <summary>
        /// 서비스를 등록합니다.
        /// </summary>
        /// <typeparam name="T">등록할 서비스 타입 (IService 구현체)</typeparam>
        /// <param name="service">등록할 서비스 인스턴스</param>
        public void RegisterService<T>(T service) where T : IService
        {
            Type serviceType = typeof(T);
            
            if (_services.ContainsKey(serviceType))
            {
                Debug.LogWarning($"서비스 타입 {serviceType.Name}은(는) 이미 등록되어 있습니다. 기존 서비스를 정리하고 대체합니다.");
                // 기존 서비스 종료 처리
                try
                {
                    _services[serviceType].Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"서비스 {serviceType.Name} 종료 중 오류 발생: {ex.Message}");
                }
                _services[serviceType] = service;
            }
            else
            {
                _services.Add(serviceType, service);
            }
            
            try
            {
                service.Initialize();
                Debug.Log($"서비스 {serviceType.Name} 등록 및 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"서비스 {serviceType.Name} 초기화 중 오류 발생: {ex.Message}");
                _services.Remove(serviceType);
                throw;
            }
        }
        
        /// <summary>
        /// 등록된 서비스를 가져옵니다.
        /// </summary>
        /// <typeparam name="T">가져올 서비스 타입</typeparam>
        /// <returns>요청한 타입의 서비스 인스턴스</returns>
        /// <exception cref="InvalidOperationException">서비스가 등록되어 있지 않은 경우 예외 발생</exception>
        public T GetService<T>() where T : IService
        {
            Type serviceType = typeof(T);
            
            if (!_services.TryGetValue(serviceType, out var service))
            {
                throw new InvalidOperationException($"서비스 {serviceType.Name}이(가) 등록되어 있지 않습니다.");
            }
            
            return (T)service;
        }
        
        /// <summary>
        /// 특정 서비스가 등록되어 있는지 확인합니다.
        /// </summary>
        /// <typeparam name="T">확인할 서비스 타입</typeparam>
        /// <returns>등록 여부</returns>
        public bool HasService<T>() where T : IService
        {
            return _services.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// 등록된 서비스를 제거합니다.
        /// </summary>
        /// <typeparam name="T">제거할 서비스 타입</typeparam>
        /// <returns>제거 성공 여부</returns>
        public bool RemoveService<T>() where T : IService
        {
            Type serviceType = typeof(T);
            
            if (_services.TryGetValue(serviceType, out var service))
            {
                try
                {
                    service.Shutdown();
                    Debug.Log($"서비스 {serviceType.Name} 종료 완료");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"서비스 {serviceType.Name} 종료 중 오류 발생: {ex.Message}");
                }
                
                _services.Remove(serviceType);
                Debug.Log($"서비스 {serviceType.Name} 제거 완료");
                return true;
            }
            
            Debug.LogWarning($"서비스 {serviceType.Name}을(를) 제거할 수 없습니다. 등록되어 있지 않습니다.");
            return false;
        }
        
        /// <summary>
        /// 모든 서비스를 제거합니다.
        /// </summary>
        public void ClearAllServices()
        {
            foreach (var service in _services.Values)
            {
                try
                {
                    service.Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"서비스 종료 중 오류 발생: {ex.Message}");
                }
            }
            
            _services.Clear();
            Debug.Log("모든 서비스가 종료되고 제거되었습니다.");
        }
    }
} 