using UnityEngine;
using GMLM.Data;

namespace GMLM.Game {
    public class Pilot : MonoBehaviour
    {
        [SerializeField] private CombatStyle _combatStyle = CombatStyle.Ranged;
        
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Image { get; private set; }
        public CombatStyle CombatStyle => _combatStyle;

        public void Initialize(PilotData data)
        {
            Id = data.id;
            Name = data.name;
            Description = data.description;
            Image = data.image;
            _combatStyle = data.combatStyle;
        }
    }
}
