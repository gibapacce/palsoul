using UnityEngine;
using Palsoul.Core;
using Palsoul.Combat;

namespace Palsoul.Creatures
{
    [RequireComponent(typeof(Rigidbody2D), typeof(HealthSystem), typeof(HitboxController))]
    public class SquadMemberAI : MonoBehaviour
    {
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField, Min(0)] private float followDistance = 2f;
        [SerializeField, Min(0)] private float attackDetectRadius = 4f;
        [SerializeField, Min(0)] private float attackRange = 1f;
        [SerializeField, Min(0)] private float extraCooldown = 0.3f;
        private CapturedCreature member;
        private Transform player;
        private Rigidbody2D body;
        private HealthSystem health;
        private HitboxController hitbox;
        private Animator animator;
        private float timer;
        private enum State { Follow, Chase, Attack, Dead }
        private State state;
        public CreatureDefinitionSO Definition => member?.definition;
        public bool IsAlive => health != null && !health.IsDead;
        public bool IsPlayerControlled { get; set; }
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<HealthSystem>();
            hitbox = GetComponent<HitboxController>();
            animator = GetComponent<Animator>();
        }
        public void Initialize(CapturedCreature creature, Transform target, float healthBonus)
        {
            member = creature;
            player = target;
            if (body == null) Awake();
            hitbox.Deactivate();
            timer = 0;
            body.linearVelocity = Vector2.zero;
            health.SetState(Definition.baseHP + healthBonus, member.healthRatio);
            animator.runtimeAnimatorController = Definition.animatorController;
            GetComponent<SpriteRenderer>().sprite = Definition.worldSprite;
            transform.localScale = Vector3.one * Definition.spriteScale;
            state = State.Follow;
        }
        private void FixedUpdate()
        {
            if (member == null) return;
            member.healthRatio = health.NormalizedHP;
            if (!IsAlive || IsPlayerControlled || player == null || player.GetComponent<HealthSystem>().IsDead)
            {
                state = State.Dead;
                body.linearVelocity = Vector2.zero;
                hitbox.Deactivate();
                return;
            }
            timer = Mathf.Max(0, timer - Time.fixedDeltaTime);
            if (state == State.Attack && timer > 0)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }
            Transform nearest = null;
            float distance = float.MaxValue;
            foreach (var collider in Physics2D.OverlapCircleAll(transform.position, attackDetectRadius, enemyLayer))
            {
                var enemy = collider.GetComponentInParent<EnemyController>();
                if (enemy == null || enemy.Health.IsDead) continue;
                float d = Vector2.Distance(transform.position, enemy.transform.position);
                if (d < distance) { distance = d; nearest = enemy.transform; }
            }
            if (nearest != null && distance <= attackRange && Definition.lightAttack != null)
            {
                state = State.Attack;
                body.linearVelocity = Vector2.zero;
                hitbox.Activate(Definition.lightAttack, nearest.position - transform.position);
                animator.SetTrigger("AttackLight");
                timer = Definition.lightAttack.duration + Definition.lightAttack.recoveryTime + extraCooldown;
                return;
            }
            state = nearest != null ? State.Chase : State.Follow;
            Vector2 delta = (nearest != null ? nearest.position : player.position) - transform.position;
            bool moving = nearest != null || delta.magnitude > followDistance;
            body.linearVelocity = moving ? delta.normalized * Definition.baseMoveSpeed : Vector2.zero;
            animator.SetBool("IsMoving", moving);
            if (moving) { animator.SetFloat("MoveX", delta.normalized.x); animator.SetFloat("MoveY", delta.normalized.y); }
        }
    }
}
