using System;
using UnityEngine;

namespace Palsoul.Core
{
    /// <summary>
    /// Gerencia o HP de qualquer entidade combatente: player (Forma Ativa), criaturas do squad,
    /// inimigos e chefes.
    ///
    /// Comunicação com UI e Áudio exclusivamente via C# events — sem referência direta.
    ///
    /// Setup: adicione ao mesmo GameObject do Rigidbody2D da entidade e configure maxHP.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        [Header("HP")]
        [Tooltip("HP máximo desta entidade.")]
        [Min(1f)]
        [SerializeField] private float maxHP = 100f;

        [Tooltip("Se true, a entidade morre ao chegar a 0 HP e o evento OnDeath é disparado.")]
        [SerializeField] private bool canDie = true;

        // ── Estado interno ─────────────────────────────────────────────────────
        private float _currentHP;
        private bool  _isDead;

        // ── Eventos ────────────────────────────────────────────────────────────
        /// <summary>Disparado ao receber dano. Parâmetros: (dano aplicado, HP atual, HP máximo).</summary>
        public event Action<float, float, float> OnDamaged;

        /// <summary>Disparado ao ser curado. Parâmetros: (quantidade curada, HP atual, HP máximo).</summary>
        public event Action<float, float, float> OnHealed;

        /// <summary>Disparado quando HP chega a 0 (e canDie = true).</summary>
        public event Action OnDeath;

        /// <summary>Disparado sempre que o HP muda (dano ou cura). Útil para barra de HP.</summary>
        public event Action<float, float> OnHPChanged;   // (atual, máximo)

        // ── Propriedades ───────────────────────────────────────────────────────
        public float CurrentHP        => _currentHP;
        public float MaxHP            => maxHP;
        public float NormalizedHP     => maxHP > 0f ? _currentHP / maxHP : 0f;
        public bool  IsDead           => _isDead;

        // ── Inicialização ──────────────────────────────────────────────────────
        private void Awake()
        {
            _currentHP = maxHP;
            _isDead    = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        #region API Pública

        /// <summary>
        /// Aplica dano à entidade.
        /// Respeita a flag IsDead — entidades mortas não recebem mais dano.
        /// </summary>
        /// <param name="amount">Quantidade de dano (positivo).</param>
        public void TakeDamage(float amount)
        {
            if (_isDead || amount <= 0f) return;

            _currentHP -= amount;
            _currentHP  = Mathf.Max(_currentHP, 0f);

            OnDamaged?.Invoke(amount, _currentHP, maxHP);
            OnHPChanged?.Invoke(_currentHP, maxHP);

            if (canDie && _currentHP <= 0f)
                Die();
        }

        /// <summary>
        /// Cura a entidade por <paramref name="amount"/> pontos de HP.
        /// Não ultrapassa maxHP.
        /// </summary>
        public void Heal(float amount)
        {
            if (_isDead || amount <= 0f) return;

            float prev = _currentHP;
            _currentHP += amount;
            _currentHP  = Mathf.Min(_currentHP, maxHP);

            float actual = _currentHP - prev;
            if (actual > 0f)
            {
                OnHealed?.Invoke(actual, _currentHP, maxHP);
                OnHPChanged?.Invoke(_currentHP, maxHP);
            }
        }

        /// <summary>
        /// Restaura HP ao máximo (usado ao descansar no Ancoradouro — MVP 7).
        /// </summary>
        public void RestoreFull()
        {
            if (_isDead) return;
            _currentHP = maxHP;
            OnHPChanged?.Invoke(_currentHP, maxHP);
        }

        /// <summary>
        /// Reconfigura o HP máximo (usado ao nivelar atributo Vigor — MVP 7).
        /// Mantém a proporção de HP atual.
        /// </summary>
        public void SetState(float maximum, float ratio)
        {
            maxHP = Mathf.Max(1, maximum);
            _currentHP = maxHP * Mathf.Clamp01(ratio);
            _isDead = canDie && _currentHP <= 0;
            OnHPChanged?.Invoke(_currentHP, maxHP);
        }

        public void SetMaxHP(float newMax, bool keepRatio = true)
        {
            float ratio = keepRatio && maxHP > 0f ? _currentHP / maxHP : 1f;
            maxHP      = Mathf.Max(1f, newMax);
            _currentHP = keepRatio ? maxHP * ratio : maxHP;
            _currentHP = Mathf.Clamp(_currentHP, 0f, maxHP);
            OnHPChanged?.Invoke(_currentHP, maxHP);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Morte

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;
            OnDeath?.Invoke();

#if UNITY_EDITOR
            Debug.Log($"[HealthSystem] {gameObject.name} morreu.", this);
#endif
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Debug OnGUI

        private void OnGUI()
        {
#if UNITY_EDITOR
            if (Camera.main == null) return;
            // Só mostra para o objeto selecionado ou para o player (tag "Player")
            if (!gameObject.CompareTag("Player")) return;

            float ratio = NormalizedHP;
            GUI.color = Color.red;
            GUI.Box(new Rect(10, 10, 200 * ratio, 18), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(10, 10, 200, 18),
                $"HP: {_currentHP:F0} / {maxHP:F0}");
#endif
        }

        #endregion
    }
}
