using System.Collections.Generic;
using UnityEngine;

namespace GMLM.AI
{
    public class Blackboard : IBlackboard
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

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

        public GameObject GetGameObject(string key) => GetValue<GameObject>(key);
        public Transform GetTransform(string key) => GetValue<Transform>(key);
        public Vector3 GetVector3(string key) => GetValue<Vector3>(key);

        public void SetGameObject(string key, GameObject value) => SetValue(key, value);
        public void SetTransform(string key, Transform value) => SetValue(key, value);
        public void SetVector3(string key, Vector3 value) => SetValue(key, value);
    }
} 