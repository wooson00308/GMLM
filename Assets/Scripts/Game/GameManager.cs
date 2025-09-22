using UnityEngine;
using GMLM.Service;

namespace GMLM.Game
{
    public class GameManager : MonoBehaviour, IService
    {
        private void Awake()
        {
            ServiceManager.Instance.RegisterSceneService(this);
        }

        public void Initialize()
        {
            throw new System.NotImplementedException();
        }

        public void Shutdown()
        {
            throw new System.NotImplementedException();
        }
    }
}