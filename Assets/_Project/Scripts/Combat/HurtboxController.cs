using UnityEngine;
using Palsoul.Core;

namespace Palsoul.Combat
{
    /// <summary>
    /// Recebe hits da HitboxController e os repassa ao HealthSystem.
    ///
    /// Responsabilidades:
    ///   - Consultar IsInvincible antes de aceitar o hit (i-frames do dodge).
    ///   - Calcular dano com multiplicador furtivo e elemental (elemental = MVP 5, por ora = 1.0).
    ///   - Aplicar knockback via Rigidbody2D.
    ///   - Disparar evento de stagger se o dano ultrapassar o limiar de poise.
    ///
    /// Setup no prefab:
    ///   - Adicione ao mesmo GameObject (ou filho) do Rigidbody2D da entidade.
    ///   - Atribua o layer "Hurtbox" a este GameObject.
    ///   - Adicione um Collider2D com IsTrigger = true (será encontrado pelo OverlapBox).
    ///   - HealthSystem e Rigidbody2D devem estar no mesmo GameObject ou pai.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HurtboxController : MonoBehaviour
    {
        [Header("Poise")]
        [Tooltip("Limiar de dano por hit acima do qual a entidade entra em stagger. 0 = sempre staggers.")]
        [Min(0f)]
        [SerializeField] private float poiseThreshold = 0f;

        // ── Referências (buscadas no pai se não atribuídas) ───────────────────
        private HealthSystem  _healthSystem;
        private Rigidbody2D   _rb;

        // ── Referência ao PlayerController para checar i-frames ───────────────
        // Usamos interface para não criar dependência circular com PlayerController.
        // Qualquer componente que implemente IInvincible pode ser checado.
        private IInvincible _invincibleOwner;

        // ── Evento de stagger ─────────────────────────────────────────────────
        /// <summary>Disparado quando o hit causa stagger (dano > poiseThreshold).</summary>
        public event System.Action<Vector2> OnStagger;   // direção do knockback

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            // Busca componentes no próprio GO ou nos pais
            _healthSystem    = GetComponentInParent<HealthSystem>();
            _rb              = GetComponentInParent<Rigidbody2D>();
            _invincibleOwner = GetComponentInParent<IInvincible>();

            if (_healthSystem == null)
                Debug.LogWarning($"[HurtboxController] Nenhum HealthSystem encontrado em {gameObject.name} ou seus pais.", this);
        }

        // ─────────────────────────────────────────────────────────────────────
        #region API Pública

        /// <summary>
        /// Chamado pela HitboxController do atacante.
        /// </summary>
        /// <param name="attack">Dados do ataque.</param>
        /// <param name="attackDirection">Direção normalizada do atacante para o alvo.</param>
        /// <param name="isStealth">True se foi ataque furtivo/pelas costas.</param>
        public void ReceiveHit(AttackDataSO attack, Vector2 attackDirection, bool isStealth, float damageMultiplier = 1f)
        {
            if (_healthSystem == null || _healthSystem.IsDead) return;

            // Respeita i-frames
            if (_invincibleOwner != null && _invincibleOwner.IsInvincible) return;

            // ── Cálculo de dano ────────────────────────────────────────────────
            if (attack == null) return;
            float damage = attack.baseDamage * damageMultiplier;

            // Multiplicador furtivo
            if (isStealth)
                damage *= attack.stealthMultiplier;

            // Multiplicador elemental (MVP 5 — placeholder = 1.0 por enquanto)
            float elementalMultiplier = 1f;
            damage *= elementalMultiplier;

            // Aplica dano
            _healthSystem.TakeDamage(damage);

            // ── Knockback ──────────────────────────────────────────────────────
            if (_rb != null && attack.knockbackForce > 0f)
                ApplyKnockback(attackDirection, attack.knockbackForce, attack.knockbackDuration);

            // ── Stagger ────────────────────────────────────────────────────────
            if (damage > poiseThreshold)
                OnStagger?.Invoke(attackDirection);

#if UNITY_EDITOR
            Debug.Log($"[HurtboxController] {gameObject.name} recebeu {damage:F1} de dano " +
                      $"(furtivo: {isStealth}, knockback: {attack.knockbackForce}).");
#endif
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Knockback

        private void ApplyKnockback(Vector2 direction, float force, float duration)
        {
            if (_rb == null) return;
            // Aplica impulso imediato; a fricção/deceleration do Rigidbody fará o resto.
            // Se necessário, uma coroutine pode reverter a velocidade após 'duration'.
            _rb.linearVelocity = direction.normalized * force;
            StartCoroutine(ResetVelocityAfter(duration));
        }

        private System.Collections.IEnumerator ResetVelocityAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_rb != null)
                _rb.linearVelocity = Vector2.zero;
        }

        #endregion
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Interface leve para desacoplar HurtboxController de PlayerController.
    /// Qualquer entidade com i-frames implementa esta interface.
    /// </summary>
    public interface IInvincible
    {
        bool IsInvincible { get; }
    }
}
