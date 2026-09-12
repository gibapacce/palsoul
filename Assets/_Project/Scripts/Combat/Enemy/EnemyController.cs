using UnityEngine;
using Palsoul.Core;

namespace Palsoul.Combat
{
    /// <summary>
    /// Hub central do inimigo básico.
    /// Gerencia a State Machine (Idle → Patrol → Chase → Attack → Stagger → Death),
    /// expõe referências aos sistemas (HealthSystem, HitboxController, Rigidbody2D)
    /// e mantém dados de estado compartilhados entre os estados (posição do player,
    /// cooldown de ataque, flag IsAlerted para bônus de captura furtiva).
    ///
    /// Requisitos de componentes no prefab:
    ///   - Rigidbody2D   (Dynamic, Gravity Scale 0, Freeze Rotation Z)
    ///   - Collider2D    (layer "Enemy")
    ///   - Animator      (parâmetros: IsMoving bool, IsChasing bool, IsAttacking bool,
    ///                    IsStaggered bool, IsDead bool, MoveX/MoveY float)
    ///   - HealthSystem
    ///   - HitboxController
    ///   - HurtboxController (filho com Collider2D trigger, layer "Hurtbox")
    ///   - EnemyDataSO atribuído no Inspector
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(HitboxController))]
    public class EnemyController : MonoBehaviour
    {
        // ── Dados de design ────────────────────────────────────────────────────
        [Header("Dados do Inimigo")]
        [SerializeField] private EnemyDataSO enemyData;

        // ── Referências de componentes ─────────────────────────────────────────
        private Rigidbody2D      _rb;
        private Animator         _animator;
        private HealthSystem     _health;
        private HitboxController _hitbox;
        private HurtboxController _hurtbox;

        // Propriedades públicas para os estados acessarem
        public Rigidbody2D      Rb          => _rb;
        public Animator         Animator    => _animator;
        public HealthSystem     Health      => _health;
        public HitboxController Hitbox      => _hitbox;
        public EnemyDataSO      Data        => enemyData;

        // ── State Machine ──────────────────────────────────────────────────────
        private IState      _currentState;
        private EnemyState  _currentStateEnum;

        private EnemyIdleState    _idleState;
        private EnemyPatrolState  _patrolState;
        private EnemyChaseState   _chaseState;
        private EnemyAttackState  _attackState;
        private EnemyStaggerState _staggerState;
        private EnemyDeathState   _deathState;

        public EnemyState CurrentState => _currentStateEnum;

        // ── Estado compartilhado entre states ─────────────────────────────────
        /// <summary>Transform do player. Encontrado em Start via tag "Player".</summary>
        public Transform PlayerTransform  { get; private set; }

        /// <summary>True quando o inimigo detectou o player e está perseguindo/atacando.</summary>
        public bool IsAlerted             { get; set; }

        /// <summary>Ponto de spawn original (usado pelo patrol para não vagar infinitamente).</summary>
        public Vector3 SpawnPosition      { get; private set; }

        /// <summary>Cooldown de ataque compartilhado (decrementado no Update).</summary>
        public float AttackCooldownTimer  { get; set; }

        // ── Animator hashes ────────────────────────────────────────────────────
        public static readonly int HashIsMoving   = Animator.StringToHash("IsMoving");
        public static readonly int HashIsChasing  = Animator.StringToHash("IsChasing");
        public static readonly int HashIsAttacking= Animator.StringToHash("IsAttacking");
        public static readonly int HashIsStaggered= Animator.StringToHash("IsStaggered");
        public static readonly int HashIsDead     = Animator.StringToHash("IsDead");
        public static readonly int HashMoveX      = Animator.StringToHash("MoveX");
        public static readonly int HashMoveY      = Animator.StringToHash("MoveY");

        // ─────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            _rb       = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _health   = GetComponent<HealthSystem>();
            _hitbox   = GetComponent<HitboxController>();
            _hurtbox  = GetComponentInChildren<HurtboxController>();

            SpawnPosition = transform.position;

            if (enemyData == null)
                Debug.LogError($"[EnemyController] EnemyDataSO não atribuído em {gameObject.name}!", this);

            // Instancia estados
            _idleState    = new EnemyIdleState(this);
            _patrolState  = new EnemyPatrolState(this);
            _chaseState   = new EnemyChaseState(this);
            _attackState  = new EnemyAttackState(this);
            _staggerState = new EnemyStaggerState(this);
            _deathState   = new EnemyDeathState(this);
        }

        private void Start()
        {
            // Localiza o player pela tag (evita FindObjectOfType a cada frame)
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                PlayerTransform = playerGO.transform;
            else
                Debug.LogWarning("[EnemyController] Nenhum GameObject com tag 'Player' encontrado.", this);

            // Assina eventos dos sistemas
            if (_health   != null) _health.OnDeath    += OnDeath;
            if (_hurtbox  != null) _hurtbox.OnStagger += OnStagger;

            TransitionTo(EnemyState.Idle);
        }

        private void OnDestroy()
        {
            if (_health  != null) _health.OnDeath    -= OnDeath;
            if (_hurtbox != null) _hurtbox.OnStagger -= OnStagger;
        }

        private void Update()
        {
            _currentState?.Tick();

            // Decrementa cooldown de ataque
            if (AttackCooldownTimer > 0f)
                AttackCooldownTimer -= Time.deltaTime;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region State Machine

        public void TransitionTo(EnemyState newState)
        {
            if (_currentStateEnum == newState && _currentState != null) return;

            _currentState?.Exit();
            _currentStateEnum = newState;

            _currentState = newState switch
            {
                EnemyState.Idle    => _idleState,
                EnemyState.Patrol  => _patrolState,
                EnemyState.Chase   => _chaseState,
                EnemyState.Attack  => _attackState,
                EnemyState.Stagger => _staggerState,
                EnemyState.Death   => _deathState,
                _                  => _idleState
            };

            _currentState.Enter();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Callbacks de Eventos

        private void OnDeath()
        {
            TransitionTo(EnemyState.Death);
        }

        private void OnStagger(Vector2 knockbackDir)
        {
            // Só staggers se não estiver morto
            if (_currentStateEnum == EnemyState.Death) return;
            _staggerState.SetKnockbackDirection(knockbackDir);
            TransitionTo(EnemyState.Stagger);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Helpers Públicos

        /// <summary>Distância atual até o player. Retorna float.MaxValue se player não encontrado.</summary>
        public float DistanceToPlayer()
        {
            if (PlayerTransform == null) return float.MaxValue;
            return Vector2.Distance(transform.position, PlayerTransform.position);
        }

        /// <summary>Direção normalizada do inimigo para o player.</summary>
        public Vector2 DirectionToPlayer()
        {
            if (PlayerTransform == null) return Vector2.zero;
            return ((Vector2)(PlayerTransform.position - transform.position)).normalized;
        }

        /// <summary>Move o Rigidbody2D em direção ao target com a velocidade fornecida.</summary>
        public void MoveTowards(Vector2 targetPos, float speed)
        {
            Vector2 dir = ((Vector2)targetPos - (Vector2)transform.position).normalized;
            _rb.linearVelocity = dir * speed;

            // Atualiza parâmetros de direção no Animator
            _animator.SetFloat(HashMoveX, dir.x);
            _animator.SetFloat(HashMoveY, dir.y);
        }

        /// <summary>Para o movimento do Rigidbody2D.</summary>
        public void StopMovement()
        {
            _rb.linearVelocity = Vector2.zero;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (enemyData == null) return;

            // Raio de detecção
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, enemyData.detectionRadius);

            // Raio de perda de aggro
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, enemyData.loseAggroRadius);

            // Raio de ataque
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, enemyData.attackRange);

            // Label de estado
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1.2f,
                $"{enemyData.enemyName} [{_currentStateEnum}]{(IsAlerted ? " !" : "")}");
#endif
        }

        #endregion
    }

    // ── Enum de estados do inimigo ─────────────────────────────────────────────
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Stagger,
        Death
    }
}
