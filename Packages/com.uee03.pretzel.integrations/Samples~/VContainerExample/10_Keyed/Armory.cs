using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Picks one of several IWeapon registrations by key.</summary>
    public sealed class Armory : IStartable
    {
        private readonly ISampleLog _log;
        private readonly IWeapon _melee;
        private readonly IWeapon _ranged;

        public Armory(ISampleLog log, [Key(WeaponSlot.Melee)] IWeapon melee, [Key(WeaponSlot.Ranged)] IWeapon ranged)
        {
            _log = log;
            _melee = melee;
            _ranged = ranged;
        }

        public void Start() => _log.Write("10 Keyed", $"Melee slot: {_melee.Name}. Ranged slot: {_ranged.Name}");
    }
}
