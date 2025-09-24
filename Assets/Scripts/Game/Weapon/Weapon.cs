using UnityEngine;

namespace GMLM.Game
{
    public class Weapon : MonoBehaviour
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Image { get; private set; }
        public WeaponType Type { get; private set; }
        public int Damage { get; private set; }
        public int Range { get; private set; }
        public int FireRate { get; private set; }
        public int MagazineSize { get; private set; }
        public int ReloadTime { get; private set; }
        public int Accuracy { get; private set; }
    }
}