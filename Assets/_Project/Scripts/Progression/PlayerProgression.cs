using System.Collections.Generic;
using Palsoul.Core;
using Palsoul.Creatures;
using UnityEngine;

namespace Palsoul.Progression
{
    [RequireComponent(typeof(HealthSystem), typeof(EtherWallet))]
    public class PlayerProgression : MonoBehaviour
    {
        private readonly Dictionary<AttributeType, int> levels = new();
        public float HealthBonus { get; private set; }
        public int GetLevel(AttributeType type) => levels.TryGetValue(type, out int level) ? level : 0;
        public bool TryUpgrade(AttributeUpgradeSO upgrade)
        {
            // MVP 7: only Vigor is implemented; unsupported upgrades never charge money.
            if (upgrade == null || upgrade.attributeType != AttributeType.Vigor) return false;
            int level = GetLevel(upgrade.attributeType);
            float cost = upgrade.GetCostForLevel(level);
            if (level >= upgrade.maxLevel || cost < 0 || !float.IsFinite(cost)) return false;
            if (!GetComponent<EtherWallet>().TrySpend(cost)) return false;
            levels[upgrade.attributeType] = level + 1;
            HealthBonus = upgrade.GetTotalValueAtLevel(level + 1);
            GetComponent<TransformationSystem>()?.RefreshHealthMaximum();
            GetComponent<SquadController>()?.RefreshPassiveHealth();
            return true;
        }
    }
}
