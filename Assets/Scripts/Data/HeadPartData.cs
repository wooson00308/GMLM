using UnityEngine;
using GMLM.Game;

namespace GMLM.Data
{
    [CreateAssetMenu(menuName = "GMLM/PartData/Head")]
    public class HeadPartData : ScriptableObject, IPartData
    {
        [SerializeField] private Part _headPrefab;
        [field: SerializeField] public MechaStats Stats { get; private set; }
        
        public PartType PartType => PartType.Head;

        public void AttachPrefab(MechaModel model)
        {
            model.AttachPart(PartType.Head, _headPrefab);
        }

        public void DetachPrefab(MechaModel model)
        {
            model.DetachPart(PartType.Head);
        }
    }
}