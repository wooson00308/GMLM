using System.Collections.Generic;
using UnityEngine;

namespace GMLM.AI
{
    public class Blackboard : IBlackboard
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        // 기존 API (호환성 유지)
        public T GetValue<T>(string key)
        {
            if (_values.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        public void SetValue<T>(string key, T value)
        {
            _values[key] = value;
        }

        // 새로운 타입 안전 API
        public T GetValue<T>(BlackboardKey<T> key)
        {
            return GetValue<T>(key.Key);
        }

        public void SetValue<T>(BlackboardKey<T> key, T value)
        {
            SetValue(key.Key, value);
        }

        // 기존 편의 메서드 (호환성 유지)
        public GameObject GetGameObject(string key) => GetValue<GameObject>(key);
        public Transform GetTransform(string key) => GetValue<Transform>(key);
        public Vector3 GetVector3(string key) => GetValue<Vector3>(key);

        public void SetGameObject(string key, GameObject value) => SetValue(key, value);
        public void SetTransform(string key, Transform value) => SetValue(key, value);
        public void SetVector3(string key, Vector3 value) => SetValue(key, value);

        // 타입 안전한 편의 메서드
        public GameObject GetGameObject(BlackboardKey<GameObject> key) => GetValue<GameObject>(key);
        public Transform GetTransform(BlackboardKey<Transform> key) => GetValue<Transform>(key);
        public Vector3 GetVector3(BlackboardKey<Vector3> key) => GetValue<Vector3>(key);

        public void SetGameObject(BlackboardKey<GameObject> key, GameObject value) => SetValue(key, value);
        public void SetTransform(BlackboardKey<Transform> key, Transform value) => SetValue(key, value);
        public void SetVector3(BlackboardKey<Vector3> key, Vector3 value) => SetValue(key, value);
    }
} 