using UnityEngine;
using UnityEngine.InputSystem;
using Palsoul.Core;

namespace Palsoul.Combat
{
    /// <summary>
    /// Controlador principal do player.
    /// Gerencia a State Machine (Idle / Moving) e lê input via Unity Input System.
    /// Todos os parâmetros de movimento vêm de PlayerMovementSO — sem magic numbers aqui.
    ///
    /// Requisitos de componentes no prefab:
    ///   - Rigidbody2D  (Body Type: Dynamic, Gravity Scale: 0, Collision Detection: Continuous)
    ///   - Collider2D   (ex.: CapsuleCollider2D)
    ///   - Animator     (com parâmetros: "IsMoving" (bool), "MoveX" (float), "MoveY" (float))
    ///   - PlayerInput  (Action Asset: InputActions, Behavior: Send Messages)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        // ── Dados de design (nunca hardcoded) ─────────────────────────────────
        [Header("Dados de Movimento")]
        [Tooltip("ScriptableObject com moveSpeed e deceleration. Crie em ScriptableObjects/Player.")]
        [SerializeField] private PlayerMovementSO movementData;

        // ── Referências de componentes ─────────────────────────────────────────
        private Rigidbody2D _rb;
        private Animator    _animator;

        // ── State Machine ──────────────────────────────────────────────────────
        private IState       _currentState;
        private PlayerState  _currentStateEnum;

        // Estados concretos (instanciados uma vez, reutilizados)
        private PlayerIdleState   _idleState;
        private PlayerMovingState _movingState;

        // ── Input ──────────────────────────────────────────────────────────────
        /// <summary>Vetor de input normalizado lido pelo Input System.</summary>
        public Vector2 MoveInput { get; private set; }

        // ── Animator hashes (evita string lookup a cada frame) ─────────────────
        private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int HashMoveX    = Animator.StringToHash("MoveX");
        private static readonly int HashMoveY    = Animator.StringToHash("MoveY");

        // ── Última direção (para animações de idle direcional) ─────────────────
        /// <summary>Última direção válida de movimento (usada em idle para manter sprite orientado).</summary>
        public Vector2 LastMoveDirection { get; private set; } = Vector2.down;

        // ─────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            _rb       = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();

            if (movementData == null)
            {
                Debug.LogError("[PlayerController] PlayerMovementSO não atribuído! " +
                               "Arraste o asset para o campo 'Movement Data' no Inspector.", this);
            }

            // Cria as instâncias dos estados passando referência ao controller
            _idleState   = new PlayerIdleState(this);
            _movingState = new PlayerMovingState(this);
        }

        private void Start()
        {
            // Estado inicial: Idle
            TransitionTo(PlayerState.Idle);
        }

        private void Update()
        {
            // Tick do estado atual
            _currentState?.Tick();

            // Atualiza o Animator
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            // Movimento físico centralizado aqui para uso via FixedUpdate
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
                // Futuros estados serão adicionados aqui (Dodge, Attack, etc.)
                _                   => _idleState
            };

            _currentState.Enter();
        }

        /// <summary>Retorna o enum do estado atual (útil para debug e transições condicionais).</summary>
        public PlayerState CurrentState => _currentStateEnum;

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Input Callbacks (chamados pelo PlayerInput via Send Messages)

        /// <summary>Chamado pelo componente PlayerInput quando a action "Move" muda.</summary>
        private void OnMove(InputValue value)
        {
            MoveInput = value.Get<Vector2>();

            // Atualiza última direção válida
            if (MoveInput.sqrMagnitude > 0.01f)
                LastMoveDirection = MoveInput.normalized;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Movimento Físico

        /// <summary>
        /// Aplica velocidade ou desaceleração no Rigidbody2D.
        /// Chamado em FixedUpdate pelo próprio controller.
        /// </summary>
        private void ApplyMovement()
        {
            if (movementData == null) return;

            if (MoveInput.sqrMagnitude > 0.01f)
            {
                // Move na direção do input normalizado × velocidade
                _rb.linearVelocity = MoveInput.normalized * movementData.moveSpeed;
            }
            else
            {
                // Desacelera suavemente ao soltar o input
                _rb.linearVelocity *= movementData.deceleration;
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Animator

        private void UpdateAnimator()
        {
            bool isMoving = MoveInput.sqrMagnitude > 0.01f;
            _animator.SetBool(HashIsMoving, isMoving);

            // Passa a última direção válida para blending direcional de sprites
            _animator.SetFloat(HashMoveX, LastMoveDirection.x);
            _animator.SetFloat(HashMoveY, LastMoveDirection.y);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Gizmos de Debug

        private void OnDrawGizmosSelected()
        {
            // Mostra a direção atual de movimento na cena para facilitar debug
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, (Vector3)LastMoveDirection * 0.5f);
        }

        #endregion
    }

    // =========================================================================
    // Estados concretos — definidos no mesmo arquivo para manter tudo junto
    // enquanto são simples; quando crescerem, mova para arquivos separados.
    // =========================================================================

    /// <summary>Estado Idle: player parado, sem input de movimento.</summary>
    internal class PlayerIdleState : IState
    {
        private readonly PlayerController _player;
        public PlayerIdleState(PlayerController player) => _player = player;

        public void Enter()
        {
            // Sem lógica especial de entrada por enquanto.
            // Futuro: transição de animação de entrada no idle.
        }

        public void Tick()
        {
            // Se houver input, transiciona para Moving
            if (_player.MoveInput.sqrMagnitude > 0.01f)
                _player.TransitionTo(PlayerState.Moving);
        }

        public void Exit() { }
    }

    /// <summary>Estado Moving: player em movimento.</summary>
    internal class PlayerMovingState : IState
    {
        private readonly PlayerController _player;
        public PlayerMovingState(PlayerController player) => _player = player;

        public void Enter()
        {
            // Futuro: disparar evento OnPlayerStartedMoving via EventBus.
        }

        public void Tick()
        {
            // Se não houver input, volta para Idle
            if (_player.MoveInput.sqrMagnitude <= 0.01f)
                _player.TransitionTo(PlayerState.Idle);
        }

        public void Exit() { }
    }
}
