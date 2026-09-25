namespace Palsoul.Core
{
    public static class CaptureFormula
    {
        public static float Calculate(CaptureSphereDataSO sphere, float hpNormalized,
            bool isStealth, CaptureStatus status, CreatureDefinitionSO species = null)
        {
            if (sphere == null || sphere.balance == null) return 0;
            var b = sphere.balance;
            var parameters = new CaptureParameters(b.hpThreshold, b.healthyModifier, b.criticalModifier,
                b.stealthModifier, b.stunnedModifier, b.burningModifier, b.poisonedModifier,
                b.frozenModifier, b.rareHealthyPenalty);
            return CaptureRules.Calculate(sphere.baseChance, hpNormalized, isStealth, status, parameters,
                species != null && species.rarity >= CreatureRarity.Rare, species != null ? species.captureHPThreshold : 0);
        }
    }
}
