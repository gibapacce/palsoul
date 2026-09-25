using System;

namespace Palsoul.Core
{
    public readonly struct CaptureParameters
    {
        public readonly float HPThreshold, Healthy, Critical, Stealth, Stunned, Burning, Poisoned, Frozen, RareHealthyPenalty;
        public CaptureParameters(float hpThreshold, float healthy, float critical, float stealth,
            float stunned, float burning, float poisoned, float frozen, float rareHealthyPenalty)
        {
            HPThreshold = hpThreshold; Healthy = healthy; Critical = critical; Stealth = stealth;
            Stunned = stunned; Burning = burning; Poisoned = poisoned; Frozen = frozen;
            RareHealthyPenalty = rareHealthyPenalty;
        }
    }

    /// <summary>Pure probability rules, executable without scenes or Unity native code.</summary>
    public static class CaptureRules
    {
        public static float Calculate(float baseChance, float health, bool stealth, CaptureStatus status,
            CaptureParameters parameters, bool rare = false, float speciesThreshold = 0)
        {
            float hp = Math.Clamp(health, 0, 1);
            float t = 1 - Math.Clamp(hp / Math.Max(.001f, parameters.HPThreshold), 0, 1);
            float result = baseChance * (parameters.Healthy + (parameters.Critical - parameters.Healthy) * t);
            if (stealth) result *= parameters.Stealth;
            if ((status & CaptureStatus.Stunned) != 0) result *= parameters.Stunned;
            if ((status & CaptureStatus.Burning) != 0) result *= parameters.Burning;
            if ((status & CaptureStatus.Poisoned) != 0) result *= parameters.Poisoned;
            if ((status & CaptureStatus.Frozen) != 0) result *= parameters.Frozen;
            if (rare && hp > speciesThreshold) result *= parameters.RareHealthyPenalty;
            return Math.Clamp(result, 0, 1);
        }
    }
    [Flags]
    public enum CaptureStatus { None = 0, Stunned = 1, Burning = 2, Poisoned = 4, Frozen = 8 }
}
