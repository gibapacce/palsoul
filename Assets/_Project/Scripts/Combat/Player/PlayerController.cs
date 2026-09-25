using UnityEngine;
using UnityEngine.InputSystem;
using Palsoul.Core;

namespace Palsoul.Combat
{
    /// <summary>
    /// Controlador principal do player.
    /// Gerencia a State Machine (Idle / Moving / Dodging / AttackLight / AttackHeavy)
    /// e lê input via Unity Input System (Send Messages).
    ///
    /// Implementa IInvincible para que o HurtboxController possa checar i-frames sem
    /// criar dependência circular.
    ///
    /// Requisitos de componentes no prefab:
    ///   - Rigidbody2D   (Dynamic, Gravity Scale 0, Continuous, Freeze Rotation Z)
    ///   - Collider2D    (ex.: CapsuleCollider2D) na layer "Player"
    ///   - Animator      (parâmetros: IsMoving bool, IsDodging bool, MoveX/MoveY float,
    ///                    AttackLight trigger, AttackHeavy trigger, AttackSpeed float)
    ///   - PlayerInput   (Action Asset: InputActions, Behavior: Send Messages)
    ///   - StaminaSystem (mesmo GameObject)
    ///   - HealthSystem  (mesmo GameObject, tag "Player")
    ///   - HitboxController (mesmo GameObject)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(StaminaSystem))]
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(HitboxController))]
    public class PlayerController : MonoBehaviour, IInvincible
    {
        // ── Dados de design ────────────────────────────────────────────────────
        [Header("Dados de Movimento")]
        [SerializeField] private PlayerMovementSO movementData;

        [Header("Dados de Stamina / Dodge")]
        [SerializeField] private StaminaSO staminaData;

        [Header("Ataques")]
        [Tooltip("AttackDataSO do ataque leve (rápido, baixo dano).")]
        [SerializeField] private AttackDataSO lightAttackData;

        [Tooltip("AttackDataSO do ataque pesado (lento, alto dano).")]
        [SerializeField] private AttackDataSO heavyAttackData;

        // ── Referências de componentes ─────────────────────────────────────────
        private Rigidbody2D    _rb;
        private Animator       _animator;
        private StaminaSystem  _staminaSystem;
        private HealthSystem   _healthSystem;
        private HitboxController _hitbox;
        private HurtboxController _hurtbox;
        private PlayerStaggerState _staggerState;
        [SerializeField, Min(0)] private float staggerDuration = .25f;

        public Animator       Animator       => _animator;
        public HealthSystem   HealthSystem   => _healthSystem;
        public StaminaSystem  StaminaSystem  => _staminaSystem;

        // Expostos para o TransformationSystem ler/escrever os AttackData da Forma Ativa
        public AttackDataSO LightAttackData => lightAttackData;
        public AttackDataSO HeavyAttackData => heavyAttackData;

        // ── State Machine ──────────────────────────────────────────────────────
        private IState      _currentState;
        private PlayerState _currentStateEnum;

        private PlayerIdleState   _idleState;
        private PlayerMovingState _movingState;
        private PlayerDodgeState  _dodgeState;
        private PlayerAttackState _attackLightState;
        private PlayerAttackState _attackHeavyState;

        // ── Input ──────────────────────────────────────────────────────────────
        public Vector2 MoveInput         { get; private set; }
        public Vector2 LastMoveDirection { get; private set; } = Vector2.down;

        // ── IInvincible ────────────────────────────────────────────────────────
        /// <summary>
        /// Flag de invencibilidade — true durante a janela de i-frames do dodge.
        /// Consultada pelo HurtboxController via interface IInvincible.
        /// </summary>
        public bool IsInvincible { get; set; }
        public bool InputBlocked { get; private set; }
        public bool CanAct => !InputBlocked && _healthSystem != null && !_healthSystem.IsDead
            && (_currentStateEnum == PlayerState.Idle || _currentStateEnum == PlayerState.Moving);
        public void SetInputBlocked(bool blocked)
        {
            InputBlocked = blocked;
            if (!blocked) return;
            MoveInput = Vector2.zero;
            _lightAttackBuffered = _heavyAttackBuffered = false;
            TransitionTo(PlayerState.Idle);
            _rb.linearVelocity = Vector2.zero;
        }

        // ── Animator hashes ────────────────────────────────────────────────────
        private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int HashMoveX    = Animator.StringToHash("MoveX");
        private static readonly int HashMoveY    = Animator.StringToHash("MoveY");

        // ── Override de velocidade (Forma Ativa pode ter velocidade diferente) ───
        private float _moveSpeedOverride = -1f;  // -1 = usa PlayerMovementSO
        // Permite que input pressionado durante animação seja processado no próximo frame válido
        private bool _lightAttackBuffered;
        private bool _heavyAttackBuffered;

        // ─────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            _rb            = GetComponent<Rigidbody2D>();
            _animator      = GetComponent<Animator>();
            _staminaSystem = GetComponent<StaminaSystem>();
            _healthSystem  = GetComponent<HealthSystem>();
            _hitbox        = GetComponent<HitboxController>();
            _hurtbox = GetComponentInChildren<HurtboxController>();
            _staggerState = new PlayerStaggerState(this, staggerDuration);

            ValidateReferences();

            // Instancia estados
            _idleState        = new PlayerIdleState(this);
            _movingState      = new PlayerMovingState(this);
            _dodgeState       = new PlayerDodgeState(this, staminaData, _rb);
            _attackLightState = new PlayerAttackState(this, _hitbox, lightAttackData,  PlayerState.AttackLight);
            _attackHeavyState = new PlayerAttackState(this, _hitbox, heavyAttackData,  PlayerState.AttackHeavy);
        }

        private void Start()
        {
            TransitionTo(PlayerState.Idle);

            // Assina morte para bloquear input
            if (_healthSystem != null)
                _healthSystem.OnDeath += OnPlayerDeath;
            if (_hurtbox != null) _hurtbox.OnStagger += OnStagger;
        }

        private void OnDestroy()
        {
            if (_healthSystem != null)
                _healthSystem.OnDeath -= OnPlayerDeath;
            if (_hurtbox != null) _hurtbox.OnStagger -= OnStagger;
        }

        private void Update()
        {
            if (InputBlocked || _healthSystem.IsDead) return;
            _currentState?.Tick();

            if (_currentStateEnum != PlayerState.Dodging)
                _dodgeState?.TickCooldown();

            ProcessBufferedInputs();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            if (InputBlocked || _healthSystem.IsDead) { _rb.linearVelocity = Vector2.zero; return; }
            if (_currentStateEnum == PlayerState.Stagger) return;
            // Movimento físico só fora de Dodge e Ataques
            bool isActing = _currentStateEnum == PlayerState.Dodging
                         || _currentStateEnum == PlayerState.AttackLight
                         || _currentStateEnum == PlayerState.AttackHeavy;

            if (!isActing)
                ApplyMovement();
            else if (_currentStateEnum == PlayerState.AttackLight
                  || _currentStateEnum == PlayerState.AttackHeavy)
                // Para o player enquanto ataca (remove momentum)
                _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, 0.4f);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region State Machine

        public void TransitionTo(PlayerState newState)
        {
            if (_currentStateEnum == newState && _currentState != null) return;

            _currentState?.Exit();
            _currentStateEnum = newState;

            _currentState = newState switch
            {
                PlayerState.Idle        => _idleState,
                PlayerState.Moving      => _movingState,
                PlayerState.Dodging     => _dodgeState,
                PlayerState.AttackLight => _attackLightState,
                PlayerState.AttackHeavy => _attackHeavyState,
                PlayerState.Stagger => _staggerState,
                _                       => _idleState
            };

            _currentState.Enter();
        }

        public PlayerState CurrentState => _currentStateEnum;

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Input Callbacks (Send Messages)

        private void OnMove(InputValue value)
        {
            if (InputBlocked || _healthSystem.IsDead) return;
            MoveInput = value.Get<Vector2>();
            if (MoveInput.sqrMagnitude > 0.01f)
                LastMoveDirection = MoveInput.normalized;
        }

        private void OnDodge(InputValue value)
        {
            if (InputBlocked) return;
            if (_currentStateEnum == PlayerState.Stagger) return;
            if (!value.isPressed) return;
            if (_currentStateEnum == PlayerState.Dodging) return;
            if (_healthSystem != null && _healthSystem.IsDead) return;
            if (_dodgeState != null && _dodgeState.IsOnCooldown) return;
            if (staminaData == null || _staminaSystem == null) return;

            if (!_staminaSystem.TryConsume(staminaData.dodgeCost))
            {
                TriggerNoStaminaFeedback();
                return;
            }

            TransitionTo(PlayerState.Dodging);
        }

        private void OnAttackLight(InputValue value)
        {
            if (InputBlocked) return;
            if (!value.isPressed) return;
            if (_healthSystem != null && _healthSystem.IsDead) return;

            // Se já está atacando levemente, enfileira combo
            if (_currentStateEnum == PlayerState.AttackLight)
            {
                _attackLightState?.QueueCombo();
                return;
            }

            // Bufferiza se estiver em dodge ou ataque pesado
            if (_currentStateEnum == PlayerState.Dodging
             || _currentStateEnum == PlayerState.AttackHeavy)
            {
                _lightAttackBuffered = true;
                return;
            }

            TryExecuteAttack(PlayerState.AttackLight);
        }

        private void OnAttackHeavy(InputValue value)
        {
            if (InputBlocked) return;
            if (!value.isPressed) return;
            if (_healthSystem != null && _healthSystem.IsDead) return;

            if (_currentStateEnum == PlayerState.Dodging
             || _currentStateEnum == PlayerState.AttackLight
             || _currentStateEnum == PlayerState.AttackHeavy)
            {
                _heavyAttackBuffered = true;
                return;
            }

            TryExecuteAttack(PlayerState.AttackHeavy);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Ataque

        public void TryExecuteAttack(PlayerState attackState)
        {
            if (!CanAct) return;
            AttackDataSO data = attackState == PlayerState.AttackLight
                ? lightAttackData
                : heavyAttackData;

            if (data == null)
            {
                Debug.LogWarning($"[PlayerController] {attackState} sem AttackDataSO atribuído!", this);
                return;
            }

            // Verifica e consome stamina
            if (_staminaSystem != null && !_staminaSystem.TryConsume(data.staminaCost))
            {
                TriggerNoStaminaFeedback();
                return;
            }

            TransitionTo(attackState);
        }

        /// <summary>
        /// Processa inputs bufferizados ao entrar em estados "livres" (Idle/Moving).
        /// Garante responsividade mesmo com pequena janela de input durante ações.
        /// </summary>
        private void ProcessBufferedInputs()
        {
            bool canAct = _currentStateEnum == PlayerState.Idle
                       || _currentStateEnum == PlayerState.Moving;
            if (!canAct) return;

            // Pesado tem prioridade sobre leve (decisão de design)
            if (_heavyAttackBuffered)
            {
                _heavyAttackBuffered = false;
                _lightAttackBuffered = false;
                TryExecuteAttack(PlayerState.AttackHeavy);
                return;
            }

            if (_lightAttackBuffered)
            {
                _lightAttackBuffered = false;
                TryExecuteAttack(PlayerState.AttackLight);
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Movimento Físico

        private void ApplyMovement()
        {
            if (movementData == null) return;
            float speed = _moveSpeedOverride > 0f ? _moveSpeedOverride : movementData.moveSpeed;
            if (MoveInput.sqrMagnitude > 0.01f)
                _rb.linearVelocity = MoveInput.normalized * speed;
            else
                _rb.linearVelocity *= movementData.deceleration;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Morte

        private void OnPlayerDeath()
        {
            _currentState?.Exit();
            _lightAttackBuffered = _heavyAttackBuffered = false;
            MoveInput = Vector2.zero;
            // Cancela qualquer ação em andamento
            _hitbox.Deactivate();
            _rb.linearVelocity = Vector2.zero;
            // MVP 8 adicionará a lógica completa de morte (Éter/Eco)
            _animator.SetTrigger("Death");
            Debug.Log("[PlayerController] Player morreu.");
        }

        private void OnStagger(Vector2 direction)
        {
            if (_healthSystem.IsDead || InputBlocked) return;
            _lightAttackBuffered = _heavyAttackBuffered = false;
            TransitionTo(PlayerState.Stagger);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Animator

        private void UpdateAnimator()
        {
            bool isMoving = MoveInput.sqrMagnitude > 0.01f
                         && _currentStateEnum == PlayerState.Moving;

            _animator.SetBool(HashIsMoving, isMoving);
            _animator.SetFloat(HashMoveX, LastMoveDirection.x);
            _animator.SetFloat(HashMoveY, LastMoveDirection.y);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Feedback Visual

        private void TriggerNoStaminaFeedback()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            StopCoroutine(nameof(NoStaminaFlash));
            StartCoroutine(nameof(NoStaminaFlash), sr);
        }

        private System.Collections.IEnumerator NoStaminaFlash(SpriteRenderer sr)
        {
            Color original = sr.color;
            sr.color = new Color(1f, 0.3f, 0.3f, 1f);
            yield return new WaitForSeconds(0.1f);
            sr.color = original;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region API de Transformação (chamada pelo TransformationSystem)

        /// <summary>
        /// Troca os AttackDataSO usados nos estados de ataque leve e pesado.
        /// Chamado pelo TransformationSystem ao mudar a Forma Ativa.
        /// Reconstrói os estados de ataque com os novos dados.
        /// </summary>
        public void SetAttackData(AttackDataSO light, AttackDataSO heavy)
        {
            lightAttackData = light;
            heavyAttackData = heavy;

            // Reconstrói os estados de ataque com os novos SOs
            _attackLightState = new PlayerAttackState(this, _hitbox, lightAttackData, PlayerState.AttackLight);
            _attackHeavyState = new PlayerAttackState(this, _hitbox, heavyAttackData, PlayerState.AttackHeavy);

            // Se estava atacando, volta ao Idle para evitar estado inválido
            if (_currentStateEnum == PlayerState.AttackLight
             || _currentStateEnum == PlayerState.AttackHeavy)
            {
                _hitbox.Deactivate();
                TransitionTo(PlayerState.Idle);
            }
        }

        /// <summary>
        /// Define um override de velocidade de movimento para a Forma Ativa.
        /// Passa -1 para voltar a usar o PlayerMovementSO.
        /// </summary>
        public void SetMoveSpeedOverride(float speed) => _moveSpeedOverride = speed;

        #endregion

        #region Validação e Debug

        private void ValidateReferences()
        {
            if (movementData   == null) Debug.LogError("[PlayerController] PlayerMovementSO não atribuído!", this);
            if (staminaData    == null) Debug.LogError("[PlayerController] StaminaSO não atribuído!", this);
            if (lightAttackData == null) Debug.LogWarning("[PlayerController] LightAttackData não atribuído!", this);
            if (heavyAttackData == null) Debug.LogWarning("[PlayerController] HeavyAttackData não atribuído!", this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsInvincible ? Color.yellow : Color.cyan;
            Gizmos.DrawRay(transform.position, (Vector3)LastMoveDirection * 0.6f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.9f,
                $"{_currentStateEnum}{(IsInvincible ? " [I]" : "")}");
#endif
        }

        #endregion
    }

    // =========================================================================
    // Estados Idle e Moving
    // =========================================================================

    internal class PlayerIdleState : IState
    {
        private readonly PlayerController _player;
        public PlayerIdleState(PlayerController player) => _player = player;
        public void Enter() { }
        public void Tick()
        {
            if (_player.MoveInput.sqrMagnitude > 0.01f)
                _player.TransitionTo(PlayerState.Moving);
        }
        public void Exit() { }
    }

    internal class PlayerMovingState : IState
    {
        private readonly PlayerController _player;
        public PlayerMovingState(PlayerController player) => _player = player;
        public void Enter() { }
        public void Tick()
        {
            if (_player.MoveInput.sqrMagnitude <= 0.01f)
                _player.TransitionTo(PlayerState.Idle);
        }
        public void Exit() { }
    }
}
