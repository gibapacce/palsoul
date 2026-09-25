using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using Palsoul.Editor;
using Palsoul.Core;
using Palsoul.Combat;
using Palsoul.Creatures;
using Palsoul.Progression;
using Palsoul.Base;

namespace Palsoul.Tests
{
    // Editor-hosted integration tests enter actual Play Mode after generating the scene.
    public class PrototypeIntegrationTests
    {
        private PlayerController player;
        [UnitySetUp] public IEnumerator Setup()
        {
            PrototypeBuilder.Build();
            EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath);
            yield return new EnterPlayMode();
            yield return null;
            player = Object.FindAnyObjectByType<PlayerController>();
        }
        [UnityTearDown] public IEnumerator Cleanup() { yield return new ExitPlayMode(); }
        // Editor-hosted tests must advance frames explicitly: WaitForSeconds is
        // a coroutine scheduler instruction, not an EditMode runner instruction.
        private static IEnumerator WaitForGameTime(float seconds)
        {
            float until = Time.time + seconds;
            double deadline = UnityEditor.EditorApplication.timeSinceStartup + 15;
            while (Time.time < until)
            {
                Assert.That(UnityEditor.EditorApplication.timeSinceStartup, Is.LessThan(deadline),
                    "Play Mode stopped advancing game time.");
                yield return null;
            }
        }
        private static void Teleport(Rigidbody2D body, Vector3 position)
        {
            body.transform.position = position;
            body.position = position;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }
        private CreatureDefinitionSO Species(string name)
            => AssetDatabase.LoadAssetAtPath<CreatureDefinitionSO>(PrototypeBuilder.Root + "/" + name + ".asset");
        private SquadController EquipPair()
        {
            var capture = player.GetComponent<CaptureSystem>();
            var squad = player.GetComponent<SquadController>();
            Assert.That(squad.Equip(capture.RegisterCapture(Species("Musguim"), .4f), true), Is.True);
            Assert.That(squad.Equip(capture.RegisterCapture(Species("Braseco"), .7f), false), Is.True);
            return squad;
        }
        [UnityTest] public IEnumerator DeathDropsExactBalanceAndRecoveryPaysOnlyOnce()
        {
            var death = player.GetComponent<PlayerDeathSystem>();
            var wallet = player.GetComponent<EtherWallet>();
            wallet.Add(23.75f);
            float carried = wallet.CurrentEther;
            Vector3 location = new(-9, -4, 0);
            Teleport(player.GetComponent<Rigidbody2D>(), location);
            player.TryExecuteAttack(PlayerState.AttackLight);
            player.HealthSystem.TakeDamage(10000);
            Assert.That(player.CurrentState, Is.EqualTo(PlayerState.Dead));
            Assert.That(player.GetComponent<HitboxController>().IsActive, Is.False);
            Assert.That(wallet.CurrentEther, Is.Zero);
            Assert.That(death.Marker.Amount, Is.EqualTo(carried));
            Assert.That(death.Marker.transform.position, Is.EqualTo(location));
            Assert.That(death.Marker.TryRecover(), Is.False, "Dead player must not collect the Eco.");
            Assert.That(death.Respawn(), Is.True);
            Assert.That(death.Respawn(), Is.False);
            Assert.That(player.CanAct, Is.True);
            Assert.That(player.IsInvincible, Is.False);
            Assert.That(player.HealthSystem.NormalizedHP, Is.EqualTo(1));
            Assert.That(death.Marker.TryRecover(), Is.False, "Distant Eco must not be collected.");
            wallet.Add(7);
            Teleport(player.GetComponent<Rigidbody2D>(), location);
            yield return null; // Real proximity collection, not just a direct API call.
            Assert.That(wallet.CurrentEther, Is.EqualTo(carried + 7));
            Assert.That(death.Marker.IsAvailable, Is.False);
            Assert.That(death.Marker.TryRecover(), Is.False);
        }

        [UnityTest] public IEnumerator SecondDeathReplacesEcoAndZeroBalanceDestroysOldPayload()
        {
            var death = player.GetComponent<PlayerDeathSystem>();
            var wallet = player.GetComponent<EtherWallet>();
            Teleport(player.GetComponent<Rigidbody2D>(), new Vector3(-9, -4));
            player.HealthSystem.TakeDamage(10000);
            var marker = death.Marker;
            death.Respawn();
            wallet.Add(19.5f);
            Vector3 second = new(-8, 4);
            Teleport(player.GetComponent<Rigidbody2D>(), second);
            player.HealthSystem.TakeDamage(10000);
            Assert.That(death.Marker, Is.SameAs(marker), "Reuse a single marker.");
            Assert.That(marker.Amount, Is.EqualTo(19.5f));
            Assert.That(marker.transform.position, Is.EqualTo(second));
            death.Respawn();
            Teleport(player.GetComponent<Rigidbody2D>(), new Vector3(-10, -3));
            player.HealthSystem.TakeDamage(10000);
            Assert.That(marker.Amount, Is.Zero);
            Assert.That(marker.IsAvailable, Is.False);
            death.Respawn();
            Teleport(player.GetComponent<Rigidbody2D>(), second);
            yield return null;
            Assert.That(wallet.CurrentEther, Is.Zero);
        }

        [UnityTest] public IEnumerator RespawnPreservesWorldCapturesUpgradesAndSquadIdentity()
        {
            var squad = EquipPair();
            var active = squad.ActiveMember;
            var passive = squad.PassiveMember;
            var capture = player.GetComponent<CaptureSystem>();
            var vigor = AssetDatabase.LoadAssetAtPath<AttributeUpgradeSO>(PrototypeBuilder.Root + "/Vigor.asset");
            player.GetComponent<PlayerProgression>().TryUpgrade(vigor);
            var enemies = Object.FindObjectsByType<EnemyController>();
            foreach (var enemy in enemies) { enemy.enabled = false; enemy.StopMovement(); }
            enemies[0].Health.TakeDamage(10000);
            enemies[1].Health.TakeDamage(5);
            float woundedHP = enemies[1].Health.CurrentHP;
            enemies[2].GetComponent<CreatureController>().ApplyCapture();
            capture.AddSpheres(-2);
            int spheres = capture.SphereCount;
            Teleport(player.GetComponent<Rigidbody2D>(), new Vector3(-9, -4));
            player.HealthSystem.TakeDamage(10000);
            Assert.That(squad.Companion.gameObject.activeSelf, Is.False);
            var death = player.GetComponent<PlayerDeathSystem>();
            death.Respawn();
            yield return null;
            Assert.That(squad.ActiveMember, Is.SameAs(active));
            Assert.That(squad.PassiveMember, Is.SameAs(passive));
            Assert.That(player.HealthSystem.MaxHP, Is.EqualTo(80));
            Assert.That(player.HealthSystem.NormalizedHP, Is.EqualTo(1));
            Assert.That(squad.Companion.GetComponent<HealthSystem>().NormalizedHP, Is.EqualTo(1));
            Assert.That(Vector2.Distance(squad.Companion.transform.position, player.transform.position), Is.LessThan(3));
            Assert.That(capture.Captured.Count, Is.EqualTo(2));
            Assert.That(capture.SphereCount, Is.EqualTo(spheres));
            Assert.That(enemies[0].Health.IsDead, Is.True);
            Assert.That(enemies[1].Health.CurrentHP, Is.EqualTo(woundedHP));
            Assert.That(enemies[2].gameObject.activeSelf, Is.False);
            Assert.That(player.GetComponent<Rigidbody2D>().position, Is.EqualTo((Vector2)death.LastAnchor.RespawnPosition));
            Assert.That(squad.TrySwap(), Is.True, "Controls must work after respawn.");
            yield return null;
            death.LastAnchor.Rest();
            yield return null;
            Assert.That(Object.FindObjectsByType<EnemyController>().Length, Is.EqualTo(3));
            Assert.That(death.Marker.IsAvailable, Is.True, "Rest must not remove the Eco.");
        }

        [UnityTest] public IEnumerator RevisitingAnAnchorMakesItTheLatestCheckpoint()
        {
            var first = Object.FindAnyObjectByType<AnchorpointController>();
            var second = Object.Instantiate(first.gameObject, new Vector3(-9, 4), Quaternion.identity)
                .GetComponent<AnchorpointController>();
            var death = player.GetComponent<PlayerDeathSystem>();
            yield return null;
            Teleport(player.GetComponent<Rigidbody2D>(), second.RespawnPosition);
            yield return null;
            Assert.That(death.LastAnchor, Is.SameAs(second));
            Teleport(player.GetComponent<Rigidbody2D>(), new Vector3(-9, -4));
            yield return null;
            player.HealthSystem.TakeDamage(10000);
            death.Respawn();
            Assert.That(player.GetComponent<Rigidbody2D>().position, Is.EqualTo((Vector2)second.RespawnPosition));
            yield return null;
            Teleport(player.GetComponent<Rigidbody2D>(), first.RespawnPosition);
            yield return null;
            Assert.That(death.LastAnchor, Is.SameAs(first));
            player.HealthSystem.TakeDamage(10000);
            death.Respawn();
            Assert.That(player.GetComponent<Rigidbody2D>().position, Is.EqualTo((Vector2)first.RespawnPosition));
        }

        [UnityTest] public IEnumerator EnemyDeathPaysOnceButCaptureAndResetDoNotPay()
        {
            var wallet = player.GetComponent<EtherWallet>();
            var enemies = Object.FindObjectsByType<EnemyController>();
            float before = wallet.CurrentEther;
            float expected = enemies[0].Data.etherDrop;
            enemies[0].Health.TakeDamage(10000);
            enemies[0].Health.TakeDamage(10000);
            enemies[1].GetComponent<CreatureController>().ApplyCapture();
            Assert.That(wallet.CurrentEther, Is.EqualTo(before + expected));
            Object.FindAnyObjectByType<WorldResetSystem>().ResetWorld();
            yield return null;
            Assert.That(wallet.CurrentEther, Is.EqualTo(before + expected));
        }

        [UnityTest] public IEnumerator DeathCancelsCaptureInFlightAndClosesAnchorMenu()
        {
            var capture = player.GetComponent<CaptureSystem>();
            var target = Object.FindAnyObjectByType<CreatureController>();
            target.GetComponent<EnemyController>().enabled = false;
            Teleport(target.GetComponent<Rigidbody2D>(), player.transform.position + Vector3.right);
            Assert.That(capture.TryCaptureNearest(), Is.True);
            int spheres = capture.SphereCount;
            var anchor = Object.FindAnyObjectByType<AnchorpointController>();
            anchor.OpenMenu();
            Assert.That(anchor.GetComponent<Palsoul.UI.AnchorpointMenuUI>().IsOpen, Is.True);
            player.HealthSystem.TakeDamage(10000);
            Assert.That(anchor.GetComponent<Palsoul.UI.AnchorpointMenuUI>().IsOpen, Is.False);
            player.GetComponent<PlayerDeathSystem>().Respawn();
            Teleport(target.GetComponent<Rigidbody2D>(), new Vector3(10, 4));
            yield return WaitForGameTime(.5f);
            Assert.That(capture.Captured.Count, Is.Zero);
            Assert.That(capture.SphereCount, Is.EqualTo(spheres));
            Assert.That(player.CanAct, Is.True);
        }

        [UnityTest] public IEnumerator SwapPreservesIndividualHPPositionsSpeciesAndStamina()
        {
            var squad = EquipPair();
            var active = squad.ActiveMember;
            var passive = squad.PassiveMember;
            Vector3 oldPosition = player.transform.position;
            Vector3 companionPosition = squad.Companion.transform.position;
            float stamina = player.StaminaSystem.CurrentStamina;
            Assert.That(squad.TrySwap(), Is.True);
            Assert.That(squad.ActiveMember, Is.SameAs(passive));
            Assert.That(squad.Companion.Definition, Is.SameAs(active.definition));
            Assert.That(player.HealthSystem.NormalizedHP, Is.EqualTo(.7f).Within(.001));
            Assert.That(squad.Companion.GetComponent<HealthSystem>().NormalizedHP, Is.EqualTo(.4f).Within(.001));
            Assert.That(player.GetComponent<Rigidbody2D>().position, Is.EqualTo((Vector2)companionPosition));
            Assert.That(squad.Companion.GetComponent<Rigidbody2D>().position, Is.EqualTo((Vector2)oldPosition));
            Assert.That(player.StaminaSystem.CurrentStamina, Is.EqualTo(stamina));
            yield return WaitForGameTime(.6f);
            Assert.That(squad.TrySwap(), Is.True);
            Assert.That(player.HealthSystem.NormalizedHP, Is.EqualTo(.4f).Within(.001));
            Assert.That(Object.FindObjectsByType<SquadMemberAI>().Length, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator UpgradesSurviveFormChangesAndUnsupportedUpgradesDoNotCharge()
        {
            var squad = EquipPair();
            var progression = player.GetComponent<PlayerProgression>();
            var vigor = AssetDatabase.LoadAssetAtPath<AttributeUpgradeSO>(PrototypeBuilder.Root + "/Vigor.asset");
            Assert.That(progression.TryUpgrade(vigor), Is.True);
            Assert.That(player.HealthSystem.MaxHP, Is.EqualTo(80));
            Assert.That(squad.TrySwap(), Is.True);
            Assert.That(player.HealthSystem.MaxHP, Is.EqualTo(100));
            var unsupported = ScriptableObject.CreateInstance<AttributeUpgradeSO>();
            unsupported.attributeType = AttributeType.Strength;
            float ether = player.GetComponent<EtherWallet>().CurrentEther;
            Assert.That(progression.TryUpgrade(unsupported), Is.False);
            Assert.That(player.GetComponent<EtherWallet>().CurrentEther, Is.EqualTo(ether));
            Object.Destroy(unsupported);
            yield return null;
        }
        [UnityTest] public IEnumerator CaptureConsumesOneSphereAndRegistersOwnedIndividual()
        {
            var capture = player.GetComponent<CaptureSystem>();
            var target = Object.FindAnyObjectByType<CreatureController>();
            target.GetComponent<EnemyController>().enabled = false;
            target.GetComponent<HealthSystem>().SetState(70, .1f);
            Teleport(player.GetComponent<Rigidbody2D>(), target.transform.position + Vector3.left);
            Physics2D.SyncTransforms();
            var sphere = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CaptureSphereDataSO>(PrototypeBuilder.Root + "/Sphere.asset"));
            sphere.baseChance = 1;
            capture.SetActiveSphere(sphere);
            int before = capture.SphereCount;
            Assert.That(capture.TryCaptureNearest(), Is.True);
            Assert.That(capture.TryCaptureNearest(), Is.False);
            yield return WaitForGameTime(.4f);
            Assert.That(capture.SphereCount, Is.EqualTo(before - 1));
            Assert.That(capture.Captured.Count, Is.EqualTo(1));
            Assert.That(target.gameObject.activeSelf, Is.False);
            Assert.That(capture.Bestiary.HasCaptured(target.Definition), Is.True);
            var template = AssetDatabase.LoadAssetAtPath<BestiaryData>(PrototypeBuilder.Root + "/BestiaryTemplate.asset");
            Assert.That(template.TotalSpeciesDiscovered, Is.Zero);
            Object.Destroy(sphere);
        }
        [UnityTest] public IEnumerator RestHealsSquadAndResetsWorldWithoutLosingCaptures()
        {
            var squad = EquipPair();
            var anchor = Object.FindAnyObjectByType<AnchorpointController>();
            var enemy = Object.FindAnyObjectByType<EnemyController>();
            enemy.Health.TakeDamage(10000);
            yield return null;
            anchor.Rest();
            yield return null;
            Assert.That(player.HealthSystem.NormalizedHP, Is.EqualTo(1));
            Assert.That(squad.Companion.GetComponent<HealthSystem>().NormalizedHP, Is.EqualTo(1));
            Assert.That(player.GetComponent<CaptureSystem>().Captured.Count, Is.EqualTo(2));
            Assert.That(Object.FindObjectsByType<EnemyController>().Length, Is.EqualTo(3));
        }
        [UnityTest] public IEnumerator DeathAndMenuBlockActionsAndSwap()
        {
            var squad = EquipPair();
            player.SetInputBlocked(true);
            Assert.That(squad.TrySwap(), Is.False);
            Assert.That(player.GetComponent<CaptureSystem>().TryCaptureNearest(), Is.False);
            player.SetInputBlocked(false);
            player.HealthSystem.TakeDamage(10000);
            Assert.That(squad.TrySwap(), Is.False);
            yield return new WaitForFixedUpdate();
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(Vector2.zero));
        }

        [UnityTest] public IEnumerator LightComboRestartsAndChargesStaminaForBothSwings()
        {
            float initial = player.StaminaSystem.CurrentStamina;
            float cost = player.LightAttackData.staminaCost;
            player.TryExecuteAttack(PlayerState.AttackLight);
            yield return WaitForGameTime(.32f);
            var field = typeof(PlayerController).GetField("_attackLightState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ((PlayerAttackState)field.GetValue(player)).QueueCombo();
            yield return WaitForGameTime(.3f);
            Assert.That(player.CurrentState, Is.EqualTo(PlayerState.AttackLight));
            Assert.That(player.StaminaSystem.CurrentStamina, Is.EqualTo(initial - cost * 2).Within(.001));
        }

        [UnityTest] public IEnumerator EnragedSwingDealsBonusOnlyOnceAcrossMultipleColliders()
        {
            var enemy = Object.FindAnyObjectByType<EnemyController>();
            enemy.enabled = false;
            Teleport(enemy.Rb, player.transform.position + Vector3.left * 1.1f);
            enemy.Rb.linearVelocity = Vector2.zero;
            enemy.GetComponent<CreatureController>().ApplyCaptureFailed();
            var extraCollider = new GameObject("SecondHurtbox", typeof(BoxCollider2D));
            extraCollider.transform.SetParent(player.transform, false);
            extraCollider.layer = player.gameObject.layer;
            extraCollider.GetComponent<BoxCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms();
            float initial = player.HealthSystem.CurrentHP;
            enemy.Hitbox.Activate(enemy.Data.attackData, Vector2.right);
            yield return WaitForGameTime(.7f);
            Assert.That(player.HealthSystem.CurrentHP,
                Is.EqualTo(initial - enemy.Data.attackData.baseDamage * 1.5f).Within(.001));
        }

        [UnityTest] public IEnumerator KeyboardMovesDiagonallyAndDodgeHasTimedInvincibility()
        {
            // Batch mode has no focused Game View. Route only this test's simulated
            // keyboard to the game and restore the editor settings afterwards.
            var previousFocus = InputSystem.settings.editorInputBehaviorInPlayMode;
            var previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputUser.PerformPairingWithDevice(keyboard, player.GetComponent<PlayerInput>().user);
                var body = player.GetComponent<Rigidbody2D>();
                Vector2 start = body.position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                Assert.That(player.MoveInput.x, Is.GreaterThan(0), "Move input must reach PlayerController.");
                yield return WaitForGameTime(.12f);
                float axialSpeed = body.linearVelocity.magnitude;
                Assert.That(axialSpeed, Is.GreaterThan(0));
                Assert.That(body.position.x, Is.GreaterThan(start.x));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.W));
                InputSystem.Update();
                yield return WaitForGameTime(.12f);
                Assert.That(body.linearVelocity.magnitude, Is.EqualTo(axialSpeed).Within(.01));
                Assert.That(body.linearVelocity.y, Is.GreaterThan(0));
                float stamina = player.StaminaSystem.CurrentStamina;
                var data = AssetDatabase.LoadAssetAtPath<StaminaSO>(PrototypeBuilder.Root + "/Stamina.asset");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.W, Key.Space));
                InputSystem.Update();
                yield return WaitForGameTime(.1f);
                Assert.That(player.CurrentState, Is.EqualTo(PlayerState.Dodging));
                Assert.That(player.IsInvincible, Is.True);
                Assert.That(player.StaminaSystem.CurrentStamina, Is.EqualTo(stamina - data.dodgeCost));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                yield return WaitForGameTime(data.dodgeDuration);
                Assert.That(player.IsInvincible, Is.False);
                Assert.That(player.CurrentState, Is.Not.EqualTo(PlayerState.Dodging));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.editorInputBehaviorInPlayMode = previousFocus;
                InputSystem.settings.backgroundBehavior = previousBackground;
            }
        }

        [UnityTest] public IEnumerator InvincibilityRejectsDamageAndStaminaRejectsOverspending()
        {
            var attack = player.LightAttackData;
            float initial = player.HealthSystem.CurrentHP;
            player.IsInvincible = true;
            player.GetComponent<HurtboxController>().ReceiveHit(attack, Vector2.right, false);
            Assert.That(player.HealthSystem.CurrentHP, Is.EqualTo(initial));
            player.IsInvincible = false;
            player.GetComponent<HurtboxController>().ReceiveHit(attack, Vector2.right, false);
            Assert.That(player.HealthSystem.CurrentHP, Is.EqualTo(initial - attack.baseDamage));
            float stamina = player.StaminaSystem.CurrentStamina;
            Assert.That(player.StaminaSystem.TryConsume(stamina + 1), Is.False);
            Assert.That(player.StaminaSystem.TryConsume(-1), Is.False);
            Assert.That(player.StaminaSystem.CurrentStamina, Is.EqualTo(stamina));
            yield return null;
        }
    }
}
