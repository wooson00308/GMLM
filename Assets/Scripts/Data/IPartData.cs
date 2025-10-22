using GMLM.Game;

namespace GMLM.Data
{
    public interface IPartData
    {
        PartType PartType { get; }
        MechaStats Stats { get; }

        void AttachPrefab(MechaModel model);
        void DetachPrefab(MechaModel model);
    }
}