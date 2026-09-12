using UnityEngine;
using UnityEngine.InputSystem;
using Palsoul.Core;

namespace Palsoul.Combat
{
    /// <summary>
    /// Controlador principal do player.
    /// Gerencia a State Machine (Idle / Moving / Dodging) e lê input via Unity Input System.
    /// Todos os parâmetros de movimento e stamina vêm de ScriptableObjects — sem magic numbers aqui.
    ///
    /// Requisitos de componentes no prefab:
    ///   - Rigidbody2D   (Body Type: Dynamic, Gravity Scale: 0, Collision Detection: Continuous, Freeze Rotation Z)
    ///   - Collider2D    (ex.: CapsuleCollider2D)
    ///   - Animator      (parâmetros: "IsMoving" bool, "IsDodging" bool, "MoveX" float, "MoveY" float)
    ///   - PlayerInput   (Action Asset: InputActions, Behavior: Send Messages)
    ///   - StaminaSystem (mesmo GameObject)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(StaminaSystem))]
    public class PlayerController : MonoBehaviour
    {
        // ── Dados de design (nunca hardcoded) ─────────────────────────────────
        [Header("Dados de Movimento")]
        [Tooltip("ScriptableObject com moveSpeed e deceleration.")]
        [SerializeField] private PlayerMovementSO movementData;

        [Header("Dados de Stamina / Dodge")]
        [Tooltip("ScriptableObject com stamina, dodge e i-frames.")]
        [SerializeField] private StaminaSO staminaData;

        // ── Referências de componentes ─────────────────────────────────────────
        private Rigidbody2D   _rb;
        private Animator      _animator;
        private StaminaSystem _staminaSystem;

        /// <summary>Referência pública ao Animator para os estados lerem (sem GetComponent).</summary>
        public Animator Animator => _animator;

        // ── State Machine ──────────────────────────────────────────────────────
        private IState      _currentState;
        private PlayerState _currentStateEnum;

        private PlayerIdleState   _idleState;
        private PlayerMovingState _movingState;
        private PlayerDodgeState  _dodgeState;

        // ── Input ──────────────────────────────────────────────────────────────
        /// <summary>Vetor de input de movimento (raw, não normalizado).</summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>Última direção válida de movimento (para idle direcional e dodge sem input).</summary>
        public Vector2 LastMoveDirection { get; private set; } = Vector2.down;

        // ── I-Frames ───────────────────────────────────────────────────────────
        /// <summary>
        /// Flag de invencibilidade. Setada pelo PlayerDodgeState durante a janela de i-frames.
        /// Consultada pelo HitboxSystem (MVP 3) para ignorar dano.
        /// </summary>
        public bool IsInvincible { get; set; }

        // ── Animator hashes ────────────────────────────────────────────────────
        private static readonly int HashIsMoving  = Animator.StringToHash("IsMoving");
        private static readonly int HashMoveX     = Animator.StringToHash("MoveX");
        private static readonly int HashMoveY     = Animator.StringToHash("MoveY");

        // ─────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            _rb            = GetComponent<Rigidbody2D>();
            _animator      = GetComponent<Animator>();
            _staminaSystem = GetComponent<StaminaSystem>();

            if (movementData == null)
                Debug.LogError("[PlayerController] PlayerMovementSO não atribuído!", this);

            if (staminaData == null)
                Debug.LogError("[PlayerController] StaminaSO não atribuído!", this);

            // Instancia estados — passam referências necessárias no construtor
            _idleState   = new PlayerIdleState(this);
            _movingState = new PlayerMovingState(this);
            _dodgeState  = new PlayerDodgeState(this, staminaData, _rb);
        }

        private void Start()
        {
            TransitionTo(PlayerState.Idle);
        }

        private void Update()
        {
            _currentState?.Tick();

            // Dodge fora do estado ativo também precisa decrementar o cooldown
            if (_currentStateEnum != PlayerState.Dodging)
                _dodgeState?.TickCooldown();

            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            // Movimento físico só é aplicado fora do estado Dodging
            // (o PlayerDodgeState controla o Rigidbody diretamente durante o roll)
            if (_currentStateEnum != PlayerState.Dodging)
                ApplyMovement();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region State Machine

        /// <summary>
        /// Realiza a transição para um novo estado.
        /// Chama Exit() no estado atual e Enter() no novo.
        /// </summary>
        public void TransitionTo(PlayerState newState)
        {
            if (_currentStateEnum == newState && _currentState != null) return;

            _currentState?.Exit();
            _currentStateEnum = newState;

            _currentState = newState switch
            {
                PlayerState.Idle    => _idleState,
                PlayerState.Moving  => _movingState,
                PlayerState.Dodging => _dodgeState,
                // Futuros: AttackLight, AttackHeavy, Stagger, Dead (MVPs 3–7)
                _                   => _idleState
            };

            _currentState.Enter();
        }

        public PlayerState CurrentState => _currentStateEnum;

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Input Callbacks (Send Messages via PlayerInput)

        private void OnMove(InputValue value)
        {
            MoveInput = value.Get<Vector2>();

            if (MoveInput.sqrMagnitude > 0.01f)
                LastMoveDirection = MoveInput.normalized;
        }

        /// <summary>
        /// Chamado pelo PlayerInput quando a action "Dodge" é pressionada.
        /// Valida cooldown e stamina antes de transicionar.
        /// </summary>
        private void OnDodge(InputValue value)
        {
            if (!value.isPressed) return;

            // Bloqueia dodge durante o próprio estado de dodge
            if (_currentStateEnum == PlayerState.Dodging) return;

            // Verifica cooldown
            if (_dodgeState != null && _dodgeState.IsOnCooldown) return;

            // Verifica stamina suficiente
            if (staminaData == null || _staminaSystem == null) return;
            if (!_staminaSystem.TryConsume(staminaData.dodgeCost))
            {
                // Sem stamina: feedback visual (flash vermelho no sprite)
                TriggerNoStaminaFeedback();
                return;
            }

            TransitionTo(PlayerState.Dodging);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Movimento Físico

        private void ApplyMovement()
        {
            if (movementData == null) return;

            if (MoveInput.sqrMagnitude > 0.01f)
                _rb.linearVelocity = MoveInput.normalized * movementData.moveSpeed;
            else
                _rb.linearVelocity *= movementData.deceleration;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Animator

        private void UpdateAnimator()
        {
            bool isMoving = MoveInput.sqrMagnitude > 0.01f
                         && _currentStateEnum != PlayerState.Dodging;

            _animator.SetBool(HashIsMoving, isMoving);
            _animator.SetFloat(HashMoveX, LastMoveDirection.x);
            _animator.SetFloat(HashMoveY, LastMoveDirection.y);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Feedback Visual

        /// <summary>
        /// Flash vermelho no SpriteRenderer quando tenta dodge sem stamina.
        /// Sem áudio — desacoplado via evento futuro no EventBus (MVP 3+).
        /// </summary>
        private void TriggerNoStaminaFeedback()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            // Inicia coroutine de flash se não houver uma rodando
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
        #region Gizmos de Debug

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsInvincible ? Color.yellow : Color.cyan;
            Gizmos.DrawRay(transform.position, (Vector3)LastMoveDirection * 0.6f);

            // Label de estado atual na Scene view
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.8f,
                $"{_currentStateEnum}{(IsInvincible ? " [I]" : "")}");
#endif
        }

        #endregion
    }

    // =========================================================================
    // Estados Idle e Moving — simples, mantidos no mesmo arquivo
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
