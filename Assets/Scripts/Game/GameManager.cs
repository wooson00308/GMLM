using UnityEngine;
using GMLM.Service;
using GMLM.Data;

namespace GMLM.Game
{
    public class GameManager : MonoBehaviour, IService
    {
        [SerializeField]
        private PilotDataTable _pilotDataTable;
        
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