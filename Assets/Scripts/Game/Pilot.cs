using UnityEngine;

namespace GMLM.Game {
    public class Pilot : MonoBehaviour
    {
        [SerializeField] private CombatStyle _combatStyle = CombatStyle.Ranged;
        
        public string Id { get; private set; }
        [SerializeField] private string _name;
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Image { get; private set; }
        public CombatStyle CombatStyle => _combatStyle;

        private void Awake()
        {
            if(!string.IsNullOrEmpty(_name)) {
                Name = _name;
            }
        }
    }
}
