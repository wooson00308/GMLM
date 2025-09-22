using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using GMLM.Event;

namespace GMLM.Service
{
    /// <summary>
    /// 유니티 게임 오브젝트로 존재하는 서비스 관리자
    /// 게임 실행 시 필요한 서비스들을 자동으로 등록하고, 씬별 서비스 생명주기를 관리
    /// </summary>
    public class ServiceManager : MonoBehaviour
    {
        private static ServiceManager _instance;

        /// <summary>
        /// 전역 접근용 싱글톤 인스턴스 (Bootstrap에서 생성되는 것을 전제로 함)
        /// </summary>
        public static ServiceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ServiceManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("[ServiceManager] Instance 요청됨 but 씬에 ServiceManager가 존재하지 않습니다. Bootstrap 구성 확인 필요.");
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private bool _dontDestroyOnLoad = true;
        
        // 서비스 로케이터 인스턴스 참조
        private ServiceLocator _serviceLocator;
        
        // 등록할 서비스 인스턴스들 (하이어라키에서 추가 및 설정 가능)
        [SerializeField] private List<MonoBehaviour> _preServiceObjects = new List<MonoBehaviour>();
        [SerializeField] private List<MonoBehaviour> _postServiceObjects = new List<MonoBehaviour>();
        
        // 종료 순서 관리를 위한 우선순위 (낮은 수가 먼저 종료됨)
        [SerializeField] private bool _useShutdownOrder = false;
        [SerializeField] private List<ShutdownOrderItem> _shutdownOrder = new List<ShutdownOrderItem>();
        
        // 씬별 서비스 관리 (씬 이름 -> 서비스 목록)
        private readonly Dictionary<string, List<IService>> _sceneServices = new Dictionary<string, List<IService>>();
        
        // 디버그용 - 현재 등록된 씬별 서비스 정보
        [SerializeField, TextArea(3, 10)] private string _debugSceneServices = "씬별 서비스 정보가 여기에 표시됩니다.";
        
        // 씬 서비스 자동 해제 활성화 여부
        [SerializeField] private bool _enableSceneServiceAutoCleanup = true;
        
        private void Awake()
        {
            // 싱글톤 보장
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            _serviceLocator = ServiceLocator.Instance;
            
            // 씬 이벤트 구독
            if (_enableSceneServiceAutoCleanup)
            {
                SceneManager.sceneUnloaded += OnSceneUnloaded;
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            
            RegisterServices();
        }
        
        /// <summary>
        /// 인스펙터에 등록된 서비스 오브젝트들을 서비스 로케이터에 등록
        /// </summary>
        private void RegisterServices()
        {
            RegisterPreServices();
            RegisterCoreServices();
            RegisterPostServices();
        }

        private void RegisterPreServices()
        {
            foreach (var serviceObject in _preServiceObjects)
            {
                if (serviceObject is IService service)
                {
                    RegisterServiceWithInterface(service, serviceObject.GetType());
                }
            }
        }

        private void RegisterPostServices()
        {
            foreach (var serviceObject in _postServiceObjects)
            {
                if (serviceObject is IService service)
                {
                    RegisterServiceWithInterface(service, serviceObject.GetType());
                }
            }
        }
        
        /// <summary>
        /// 서비스 객체를 가장 적합한 인터페이스 타입으로 등록
        /// </summary>
        private void RegisterServiceWithInterface(IService service, System.Type concreteType)
        {
            // 모든 인터페이스를 가져와서 IService를 구현한 것 찾기
            var interfaces = concreteType.GetInterfaces();
            
            foreach (var interfaceType in interfaces)
            {
                if (interfaceType != typeof(IService) && typeof(IService).IsAssignableFrom(interfaceType))
                {
                    // 리플렉션을 사용하여 RegisterService<T>를 올바른 타입으로 호출
                    var method = typeof(ServiceLocator).GetMethod("RegisterService");
                    var genericMethod = method.MakeGenericMethod(interfaceType);
                    genericMethod.Invoke(_serviceLocator, new object[] { service });
                    
                    return;
                }
            }
            
            // 적절한 인터페이스를 찾지 못했을 경우 구체 타입으로 등록
            var registerMethod = typeof(ServiceLocator).GetMethod("RegisterService");
            var genericRegisterMethod = registerMethod.MakeGenericMethod(concreteType);
            genericRegisterMethod.Invoke(_serviceLocator, new object[] { service });
        }
        
        /// <summary>
        /// 기본 서비스들을 등록합니다.
        /// </summary>
        private void RegisterCoreServices()
        {
            // 이벤트 버스 서비스 등록
            ServiceLocator.Instance.RegisterService(new EventService());
        }
        
        #region 씬별 서비스 관리
        
        /// <summary>
        /// 현재 활성 씬에 서비스를 등록합니다. 씬이 언로드될 때 자동으로 해제됩니다.
        /// </summary>
        /// <typeparam name="T">등록할 서비스 타입</typeparam>
        /// <param name="service">등록할 서비스 인스턴스</param>
        public void RegisterSceneService<T>(T service) where T : IService
        {
            string sceneName = SceneManager.GetActiveScene().name;
            RegisterSceneService(service, sceneName);
        }
        
        /// <summary>
        /// 지정된 씬에 서비스를 등록합니다. 해당 씬이 언로드될 때 자동으로 해제됩니다.
        /// </summary>
        /// <typeparam name="T">등록할 서비스 타입</typeparam>
        /// <param name="service">등록할 서비스 인스턴스</param>
        /// <param name="sceneName">씬 이름</param>
        public void RegisterSceneService<T>(T service, string sceneName) where T : IService
        {
            // 서비스 로케이터에 등록
            _serviceLocator.RegisterService(service);
            
            // 씬별 서비스 목록에 추가
            if (!_sceneServices.ContainsKey(sceneName))
            {
                _sceneServices[sceneName] = new List<IService>();
            }
            
            _sceneServices[sceneName].Add(service);
            
            Debug.Log($"[ServiceManager] 씬 서비스 등록: {typeof(T).Name} -> {sceneName}");
            UpdateDebugInfo();
        }
        
        /// <summary>
        /// 씬이 로드될 때 호출되는 이벤트 핸들러
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[ServiceManager] 씬 로드됨: {scene.name}");
            UpdateDebugInfo();
        }
        
        /// <summary>
        /// 씬이 언로드될 때 호출되는 이벤트 핸들러 - 해당 씬의 서비스들을 자동으로 해제
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            string sceneName = scene.name;
            
            if (_sceneServices.ContainsKey(sceneName))
            {
                var services = _sceneServices[sceneName];
                Debug.Log($"[ServiceManager] 씬 서비스 해제 시작: {sceneName} ({services.Count}개 서비스)");
                
                // 등록된 순서의 역순으로 해제 (의존성 고려)
                for (int i = services.Count - 1; i >= 0; i--)
                {
                    var service = services[i];
                    
                    try
                    {
                        // 서비스 로케이터에서 제거
                        var serviceType = service.GetType();
                        var removeMethod = typeof(ServiceLocator).GetMethod("RemoveService");
                        var genericMethod = removeMethod.MakeGenericMethod(serviceType);
                        genericMethod.Invoke(_serviceLocator, null);
                        
                        Debug.Log($"[ServiceManager] 씬 서비스 해제: {serviceType.Name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ServiceManager] 씬 서비스 해제 중 오류: {service.GetType().Name} - {ex.Message}");
                    }
                }
                
                // 씬별 서비스 목록에서 제거
                _sceneServices.Remove(sceneName);
                Debug.Log($"[ServiceManager] 씬 서비스 해제 완료: {sceneName}");
                
                UpdateDebugInfo();
            }
        }
        
        /// <summary>
        /// 인스펙터용 디버그 정보 업데이트
        /// </summary>
        private void UpdateDebugInfo()
        {
            if (_sceneServices.Count == 0)
            {
                _debugSceneServices = "등록된 씬별 서비스가 없습니다.";
                return;
            }
            
            var debugInfo = new System.Text.StringBuilder();
            debugInfo.AppendLine("=== 씬별 서비스 현황 ===");
            
            foreach (var kvp in _sceneServices)
            {
                debugInfo.AppendLine($"\n[{kvp.Key}] ({kvp.Value.Count}개):");
                foreach (var service in kvp.Value)
                {
                    debugInfo.AppendLine($"  - {service.GetType().Name}");
                }
            }
            
            _debugSceneServices = debugInfo.ToString();
        }
        
        /// <summary>
        /// 특정 씬의 모든 서비스를 강제로 해제합니다.
        /// </summary>
        /// <param name="sceneName">해제할 씬 이름</param>
        public void ForceUnloadSceneServices(string sceneName)
        {
            if (_sceneServices.ContainsKey(sceneName))
            {
                var fakeScene = new Scene();
                // Scene 구조체는 불변이라 리플렉션으로 name 설정
                var nameField = typeof(Scene).GetField("m_Name", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                nameField?.SetValue(fakeScene, sceneName);
                
                OnSceneUnloaded(fakeScene);
            }
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // 씬 이벤트 구독 해제
            if (_enableSceneServiceAutoCleanup)
            {
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
            
            if (_useShutdownOrder && _shutdownOrder.Count > 0)
            {
                // 종료 우선순위에 따라 서비스 순차 종료
                _shutdownOrder.Sort((a, b) => a.shutdownPriority.CompareTo(b.shutdownPriority));
                
                foreach (var item in _shutdownOrder)
                {
                    if (item.serviceType != null)
                    {
                        // 리플렉션을 사용하여 RemoveService<T>를 올바른 타입으로 호출
                        try
                        {
                            var method = typeof(ServiceLocator).GetMethod("RemoveService");
                            var genericMethod = method.MakeGenericMethod(item.serviceType);
                            genericMethod.Invoke(_serviceLocator, null);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"서비스 {item.serviceType.Name} 종료 중 오류 발생: {ex.Message}");
                        }
                    }
                }
                
                // 남은 서비스 정리
                _serviceLocator.ClearAllServices();
            }
            else
            {
                // 우선순위가 없는 경우 모든 서비스 한 번에 정리
                _serviceLocator.ClearAllServices();
            }
        }
        
        #if UNITY_EDITOR
        // 에디터에서 사용할 유틸리티 메서드 (선택적)
        /// <summary>
        /// 에디터 전용: 서비스 오브젝트 목록에 새 서비스를 추가
        /// </summary>
        public void AddService(MonoBehaviour service)
        {
            if (service is IService)
            {
                _postServiceObjects.Add(service);
            }
            else
            {
                Debug.LogError($"{service.name}은(는) IService를 구현하지 않았습니다.");
            }
        }
        
        /// <summary>
        /// 에디터 전용: 현재 씬에 서비스를 즉시 등록 (테스트용)
        /// </summary>
        public void AddSceneService(MonoBehaviour service)
        {
            if (service is IService iService)
            {
                RegisterSceneService(iService);
            }
            else
            {
                Debug.LogError($"{service.name}은(는) IService를 구현하지 않았습니다.");
            }
        }
        
        /// <summary>
        /// 에디터 전용: 씬별 서비스 정보를 강제로 업데이트
        /// </summary>
        [UnityEditor.MenuItem("GMLM/Update Scene Service Debug Info")]
        public static void UpdateSceneServiceDebugInfo()
        {
            var manager = FindFirstObjectByType<ServiceManager>();
            if (manager != null)
            {
                manager.UpdateDebugInfo();
                Debug.Log("씬 서비스 디버그 정보가 업데이트되었습니다.");
            }
            else
            {
                Debug.LogWarning("ServiceManager를 찾을 수 없습니다.");
            }
        }
        #endif
    }
} 