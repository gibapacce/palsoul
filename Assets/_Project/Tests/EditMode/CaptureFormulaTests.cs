using NUnit.Framework;
using UnityEngine;
using Palsoul.Core;

namespace Palsoul.Tests
{
    public class CaptureFormulaTests
    {
        private CaptureBalanceSO balance;
        private CaptureSphereDataSO sphere;
        [SetUp] public void Setup()
        {
            balance = ScriptableObject.CreateInstance<CaptureBalanceSO>();
            sphere = ScriptableObject.CreateInstance<CaptureSphereDataSO>();
            sphere.baseChance = .3f;
            sphere.balance = balance;
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(sphere);
            Object.DestroyImmediate(balance);
        }
        [TestCase(1f, false, CaptureStatus.None, .15f)]
        [TestCase(.5f, false, CaptureStatus.None, .15f)]
        [TestCase(.2f, false, CaptureStatus.None, .42f)]
        [TestCase(0f, false, CaptureStatus.None, .6f)]
        [TestCase(.2f, true, CaptureStatus.None, .63f)]
        [TestCase(.2f, true, CaptureStatus.Stunned, .882f)]
        [TestCase(1f, false, CaptureStatus.Burning, .18f)]
        [TestCase(1f, false, CaptureStatus.Frozen, .21f)]
        [TestCase(0f, true, CaptureStatus.Stunned | CaptureStatus.Burning, 1f)]
        public void KnownProbabilities(float hp, bool stealth, CaptureStatus status, float expected)
            => Assert.That(CaptureFormula.Calculate(sphere, hp, stealth, status), Is.EqualTo(expected).Within(.0001));

        [Test] public void SphereTierChangesChanceThroughItsConfiguredBaseChance()
        {
            sphere.baseChance = .6f;
            Assert.That(CaptureFormula.Calculate(sphere, .2f, false, CaptureStatus.None), Is.EqualTo(.84f).Within(.0001));
        }
        [Test] public void RareCreatureRequiresLowHealthForViableChance()
        {
            var species = ScriptableObject.CreateInstance<CreatureDefinitionSO>();
            try
            {
                species.rarity = CreatureRarity.Rare;
                Assert.That(CaptureFormula.Calculate(sphere, 1, false, CaptureStatus.None, species), Is.EqualTo(.015f).Within(.0001));
                Assert.That(CaptureFormula.Calculate(sphere, .2f, false, CaptureStatus.None, species), Is.EqualTo(.42f).Within(.0001));
            }
            finally { Object.DestroyImmediate(species); }
        }
        [Test] public void MissingConfigurationDoesNotAllowCapture()
        {
            sphere.balance = null;
            Assert.That(CaptureFormula.Calculate(sphere, 0, true, CaptureStatus.Stunned), Is.Zero);
        }
    }
}
