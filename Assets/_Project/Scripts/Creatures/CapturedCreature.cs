using System;
using Palsoul.Core;
using UnityEngine;

namespace Palsoul.Creatures
{
    [Serializable]
    public sealed class CapturedCreature
    {
        public string id = Guid.NewGuid().ToString("N");
        public CreatureDefinitionSO definition;
        [Range(0, 1)] public float healthRatio = 1f;
        public CapturedCreature(CreatureDefinitionSO species, float ratio = 1f)
        {
            definition = species;
            healthRatio = Mathf.Clamp01(ratio);
        }
    }
}
