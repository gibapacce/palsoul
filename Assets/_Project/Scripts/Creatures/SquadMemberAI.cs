using UnityEngine;
using Palsoul.Core;

namespace Palsoul.Creatures
{
    /// <summary>
    /// IA autônoma da criatura do squad quando não está sob controle direto do jogador.
    /// GDD seção 5.4: "a criatura que perde o controle opera com IA autônoma."
    ///
    /// Comportamento:
    ///   1. Segue o player mantendo uma distância mínima (não fica colado).
    ///   2. Detecta inimigos próximos e os ataca com o AttackData da sua espécie.
    ///   3. Quando recebe controle (IsPlayerControlled = true), para a IA completamente.
    ///
    /// Setup: este componente é adicionado ao prefab da criatura do squad (um GO separado
    /// do player que representa a segunda criatura). O SquadController liga/desliga
    /// via IsPlayerControlled.
    /// </summary>
    public class SquadMemberAI : MonoBehaviour
    {
        // ── Referências ────────────────────────────────────────────────────────
        [Header("Definição da Espécie")]
        [SerializeField] private CreatureDefinitionSO _definition;
        public CreatureDefinitionSO Definition => _definition;

        private Rigidbody2D      _rb;
        private HealthSystem     _health;
        private Combat.HitboxController _hitbox;
        private Animator         _animator;

        // ── Configuração ───────────────────────────────────────────────────────
        [Header("Seguimento")]
        [Tooltip("Distância mínima do player (não chega mais perto que isso).")]
        [SerializeField] private float followMinDistance = 1.5f;

        [Tooltip("Distância máxima antes de começar a seguir o player.")]
        [SerializeField] private float followMaxDistance = 4f;

        [Header("Combate Autônomo")]
        [Tooltip("Raio de detecção de inimigos para ataque autônomo.")]
        [SerializeField] private float attackDetectRadius = 3f;

        [Tooltip("LayerMask dos inimigos que a IA vai atacar.")]
        [SerializeField] private LayerMask enemyLayer;

        // ── Estado ─────────────────────────────────────────────────────────────
        private bool    _isPlayerControlled;
        private float   _attackCooldown;
        private Transform _playerTransform;

        // Animator hashes
        private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int HashMoveX    = Animator.StringToHash("MoveX");
        private static readonly int HashMoveY    = Animator.StringToHash("MoveY");

        // ── Propriedades ───────────────────────────────────────────────────────
        public bool IsPlayerControlled
        {
            get => _isPlayerControlled;
            set
            {
                _isPlayerControlled = value;
                if (value) StopAutonomousBehavior();
            }
        }

        public bool IsAlive => _health != null && !_health.IsDead;

        // ─────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            _rb      = GetComponent<Rigidbody2D>();
            _health  = GetComponent<HealthSystem>();
            _hitbox  = GetComponent<Combat.HitboxController>();
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) _playerTransform = playerGO.transform;
        }

        private void Update()
        {
            if (_isPlayerControlled || !IsAlive) return;

            _attackCooldown -= Time.deltaTime;

            // Prioridade 1: ataca inimigo próximo
            if (TryAttackNearbyEnemy()) return;

            // Prioridade 2: segue o player
            FollowPlayer();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region IA Autônoma

        private bool TryAttackNearbyEnemy()
        {
            if (_attackCooldown > 0f) return false;
            if (_definition?.lightAttack == null) return false;

            Collider2D[] enemies = Physics2D.OverlapCircleAll(
                transform.position, attackDetectRadius, enemyLayer);

            if (enemies.Length == 0) return false;

            // Ataca o mais próximo
            Collider2D nearest = null;
            float minDist = float.MaxValue;
            foreach (var col in enemies)
            {
                float d = Vector2.Distance(transform.position, col.transform.position);
                if (d < minDist) { minDist = d; nearest = col; }
            }

            if (nearest == null) return false;

            Vector2 dir = ((Vector2)(nearest.transform.position - transform.position)).normalized;
            _hitbox?.Activate(_definition.lightAttack, dir);
            _attackCooldown = _definition.lightAttack.duration
                            + _definition.lightAttack.recoveryTime + 0.3f; // cooldown extra

            // Para o movimento durante o ataque
            _rb.linearVelocity = Vector2.zero;
            _animator?.SetBool(HashIsMoving, false);

            return true;
        }

        private void FollowPlayer()
        {
            if (_playerTransform == null) return;

            float dist = Vector2.Distance(transform.position, _playerTransform.position);

            if (dist > followMaxDistance)
            {
                // Move em direção ao player
                Vector2 dir = ((Vector2)(_playerTransform.position - transform.position)).normalized;
                float speed = _definition != null ? _definition.baseMoveSpeed : 3f;
                _rb.linearVelocity = dir * speed;

                _animator?.SetBool(HashIsMoving, true);
                _animator?.SetFloat(HashMoveX, dir.x);
                _animator?.SetFloat(HashMoveY, dir.y);
            }
            else if (dist < followMinDistance)
            {
                // Afasta levemente para não sobrepor o player
                Vector2 dir = ((Vector2)(transform.position - _playerTransform.position)).normalized;
                float speed = _definition != null ? _definition.baseMoveSpeed * 0.5f : 1.5f;
                _rb.linearVelocity = dir * speed;
            }
            else
            {
                _rb.linearVelocity = Vector2.zero;
                _animator?.SetBool(HashIsMoving, false);
            }
        }

        private void StopAutonomousBehavior()
        {
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            _hitbox?.Deactivate();
            _animator?.SetBool(HashIsMoving, false);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, followMaxDistance);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackDetectRadius);
        }

        #endregion
    }
}
