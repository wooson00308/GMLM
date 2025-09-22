using UnityEngine;

namespace GMLM.AI
{
    public interface IBlackboard
    {
        T GetValue<T>(string key);
        void SetValue<T>(string key, T value);
        
        // 유니티 GameObject나 Component를 쉽게 참조하기 위한 편의 메서드
        GameObject GetGameObject(string key);
        Transform GetTransform(string key);
        Vector3 GetVector3(string key);
        
        void SetGameObject(string key, GameObject value);
        void SetTransform(string key, Transform value);
        void SetVector3(string key, Vector3 value);
    }
} 