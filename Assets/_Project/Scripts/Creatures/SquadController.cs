using UnityEngine;
using UnityEngine.InputSystem;
using Palsoul.Core;
using Palsoul.Combat;
using Palsoul.Progression;

namespace Palsoul.Creatures
{
    [RequireComponent(typeof(TransformationSystem))]
    public class SquadController : MonoBehaviour
    {
        [SerializeField] private GameObject squadMemberPrefab;
        [SerializeField] private Vector2 squadSpawnOffset = new(1.5f, 0f);
        [SerializeField, Min(0)] private float swapCooldown = 0.5f;
        private readonly CapturedCreature[] members = new CapturedCreature[2];
        private SquadMemberAI companion;
        private int activeSlot;
        private float cooldown;
        public CapturedCreature ActiveMember => members[activeSlot];
        public CapturedCreature PassiveMember => members[1 - activeSlot];
        public CreatureDefinitionSO ActiveFormDefinition => ActiveMember?.definition;
        public CreatureDefinitionSO PassiveFormDefinition => PassiveMember?.definition;
        public int ActiveSlot => activeSlot;
        public SquadMemberAI Companion => companion;
        public event System.Action<CreatureDefinitionSO, int> OnSquadSwapped;
        private void Update() => cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
        private void OnSwap(InputValue value) { if (value.isPressed) TrySwap(); }
        public bool TrySwap()
        {
            var player = GetComponent<PlayerController>();
            if (cooldown > 0 || !player.CanAct || ActiveMember == null || PassiveMember == null
                || companion == null || !companion.IsAlive) return false;
            SyncHealth();
            Vector3 previousPosition = transform.position;
            Vector3 nextPosition = companion.transform.position;
            activeSlot = 1 - activeSlot;
            GetComponent<Rigidbody2D>().position = nextPosition;
            GetComponent<TransformationSystem>().ApplyForm(ActiveMember.definition, ActiveMember.healthRatio);
            companion.transform.position = previousPosition;
            companion.GetComponent<Rigidbody2D>().position = previousPosition;
            companion.Initialize(PassiveMember, transform, HealthBonus);
            cooldown = swapCooldown;
            OnSquadSwapped?.Invoke(ActiveFormDefinition, activeSlot);
            return true;
        }
        public bool Equip(CapturedCreature creature, bool active)
        {
            var roster = GetComponent<CaptureSystem>();
            if (creature == null || roster == null || !roster.Owns(creature)
                || GetComponent<HealthSystem>().IsDead || (active && creature.healthRatio <= 0)) return false;
            int slot = active ? activeSlot : 1 - activeSlot;
            if (members[1 - slot] == creature) return false;
            if (members[slot] == creature) return true;
            SyncHealth();
            members[slot] = creature;
            if (active) GetComponent<TransformationSystem>().ApplyForm(creature.definition, creature.healthRatio);
            else SetupCompanion();
            return true;
        }
        public void SyncHealth()
        {
            if (ActiveMember != null) ActiveMember.healthRatio = GetComponent<HealthSystem>().NormalizedHP;
            if (PassiveMember != null && companion != null)
                PassiveMember.healthRatio = companion.GetComponent<HealthSystem>().NormalizedHP;
        }
        private float HealthBonus => GetComponent<PlayerProgression>()?.HealthBonus ?? 0;
        private void SetupCompanion()
        {
            if (PassiveMember == null || squadMemberPrefab == null) return;
            if (companion == null)
                companion = Instantiate(squadMemberPrefab, transform.position + (Vector3)squadSpawnOffset,
                    Quaternion.identity).GetComponent<SquadMemberAI>();
            companion.Initialize(PassiveMember, transform, HealthBonus);
        }
        public void RefreshPassiveHealth()
        {
            if (companion != null && PassiveMember != null)
                companion.GetComponent<HealthSystem>().SetMaxHP(PassiveMember.definition.baseHP + HealthBonus, true);
        }
        public void Rest()
        {
            var roster = GetComponent<CaptureSystem>();
            if (roster != null) foreach (var creature in roster.Captured) creature.healthRatio = 1;
            if (ActiveMember != null) GetComponent<TransformationSystem>().ApplyForm(ActiveMember.definition, 1);
            SetupCompanion();
        }
        public void SuspendForDeath()
        {
            SyncHealth();
            if (companion != null)
            {
                companion.GetComponent<HitboxController>().Deactivate();
                companion.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                companion.gameObject.SetActive(false);
            }
        }
        public void Respawn()
        {
            cooldown = 0;
            if (ActiveMember != null)
            {
                ActiveMember.healthRatio = 1;
                GetComponent<TransformationSystem>().ApplyForm(ActiveMember.definition, 1);
            }
            if (PassiveMember != null) PassiveMember.healthRatio = 1;
            SetupCompanion();
            if (companion != null)
            {
                Vector3 position = transform.position + (Vector3)squadSpawnOffset;
                companion.transform.position = position;
                companion.GetComponent<Rigidbody2D>().position = position;
                companion.gameObject.SetActive(true);
            }
        }
        private void OnDestroy() { if (companion != null) Destroy(companion.gameObject); }
    }
}
