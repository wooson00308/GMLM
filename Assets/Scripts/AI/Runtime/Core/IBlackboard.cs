using UnityEngine;

namespace GMLM.AI
{
    public interface IBlackboard
    {
        // 기존 API (호환성 유지)
        T GetValue<T>(string key);
        void SetValue<T>(string key, T value);
        
        // 새로운 타입 안전 API
        T GetValue<T>(BlackboardKey<T> key);
        void SetValue<T>(BlackboardKey<T> key, T value);
        
        // 유니티 GameObject나 Component를 쉽게 참조하기 위한 편의 메서드
        GameObject GetGameObject(string key);
        Transform GetTransform(string key);
        Vector3 GetVector3(string key);
        
        void SetGameObject(string key, GameObject value);
        void SetTransform(string key, Transform value);
        void SetVector3(string key, Vector3 value);
        
        // 타입 안전한 편의 메서드
        GameObject GetGameObject(BlackboardKey<GameObject> key);
        Transform GetTransform(BlackboardKey<Transform> key);
        Vector3 GetVector3(BlackboardKey<Vector3> key);
        
        void SetGameObject(BlackboardKey<GameObject> key, GameObject value);
        void SetTransform(BlackboardKey<Transform> key, Transform value);
        void SetVector3(BlackboardKey<Vector3> key, Vector3 value);
    }
} 