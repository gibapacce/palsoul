using UnityEngine;
using Palsoul.Core;
using Palsoul.Combat;
using Palsoul.Progression;

namespace Palsoul.Creatures
{
    [RequireComponent(typeof(PlayerController), typeof(HealthSystem))]
    public class TransformationSystem : MonoBehaviour
    {
        private PlayerController player;
        private HealthSystem health;
        private SpriteRenderer visual;
        private RuntimeAnimatorController humanAnimator;
        private Sprite humanSprite;
        private AttackDataSO humanLight, humanHeavy;
        private Vector3 humanScale;
        private float humanMaxHP;
        private bool initialized;
        public CreatureDefinitionSO ActiveForm { get; private set; }
        public bool HasActiveForm => ActiveForm != null;
        public event System.Action<CreatureDefinitionSO> OnFormChanged;
        private void Start() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            player = GetComponent<PlayerController>();
            health = GetComponent<HealthSystem>();
            visual = GetComponent<SpriteRenderer>();
            humanAnimator = GetComponent<Animator>().runtimeAnimatorController;
            humanSprite = visual != null ? visual.sprite : null;
            humanScale = transform.localScale;
            humanLight = player.LightAttackData;
            humanHeavy = player.HeavyAttackData;
            humanMaxHP = health.MaxHP;
            initialized = true;
        }
        public void SetActiveForm(CreatureDefinitionSO definition)
            => ApplyForm(definition, GetComponent<HealthSystem>().NormalizedHP);
        public void ApplyForm(CreatureDefinitionSO definition, float healthRatio)
        {
            Initialize();
            ActiveForm = definition;
            GetComponent<Animator>().runtimeAnimatorController = definition != null
                ? definition.animatorController : humanAnimator;
            if (visual != null) visual.sprite = definition != null ? definition.worldSprite : humanSprite;
            player.SetAttackData(definition != null ? definition.lightAttack : humanLight,
                definition != null ? definition.heavyAttack : humanHeavy);
            player.SetMoveSpeedOverride(definition != null ? definition.baseMoveSpeed : -1f);
            transform.localScale = definition != null ? humanScale * definition.spriteScale : humanScale;
            health.SetState(MaximumHP, healthRatio);
            OnFormChanged?.Invoke(definition);
        }
        private float MaximumHP => (ActiveForm != null ? ActiveForm.baseHP : humanMaxHP)
            + (GetComponent<PlayerProgression>()?.HealthBonus ?? 0f);
        public void RefreshHealthMaximum()
        {
            Initialize();
            health.SetMaxHP(MaximumHP, true);
        }
        public void RestoreHumanForm() => SetActiveForm(null);
    }
}
