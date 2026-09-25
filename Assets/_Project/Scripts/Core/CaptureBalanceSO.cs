using UnityEngine;
namespace Palsoul.Core
{
    [CreateAssetMenu(menuName = "Palsoul/Capture Balance")]
    public class CaptureBalanceSO : ScriptableObject
    {
        [Range(0.01f, 1)] public float hpThreshold = 0.5f;
        [Min(0)] public float healthyModifier = 0.5f;
        [Min(0)] public float criticalModifier = 2f;
        [Min(0)] public float stealthModifier = 1.5f;
        [Min(0)] public float stunnedModifier = 1.4f;
        [Min(0)] public float burningModifier = 1.2f;
        [Min(0)] public float poisonedModifier = 1.15f;
        [Min(0)] public float frozenModifier = 1.4f;
        [Range(0, 1)] public float rareHealthyPenalty = 0.1f;
    }
}
