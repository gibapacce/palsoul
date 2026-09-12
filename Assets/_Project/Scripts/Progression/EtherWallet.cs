using System;
using UnityEngine;

namespace Palsoul.Progression
{
    /// <summary>
    /// Gerencia o Éter do jogador — a moeda de progressão soulslike do PalSoul.
    /// GDD seção 5.6: Éter é ganho matando inimigos/explorando, perdido ao morrer
    /// (recuperável via Eco) e gasto no Ancoradouro para upgrades de atributo.
    ///
    /// Comunicação com UI e Áudio via C# events — sem referência direta.
    /// O sistema de morte (MVP 8) usa TakeDeath() / RestoreFromEco().
    ///
    /// Setup: adicione ao mesmo GO do PlayerController (tag "Player").
    /// </summary>
    public class EtherWallet : MonoBehaviour
    {
        [Header("Éter Inicial")]
        [Tooltip("Quantidade de Éter com que o jogador começa.")]
        [Min(0f)]
        [SerializeField] private float startingEther = 0f;

        // ── Estado ─────────────────────────────────────────────────────────────
        private float _currentEther;

        // ── Eventos ────────────────────────────────────────────────────────────
        /// <summary>Disparado sempre que o Éter muda. Parâmetro: novo valor.</summary>
        public event Action<float> OnEtherChanged;

        /// <summary>Disparado ao ganhar Éter. Parâmetros: (quantidade ganha, total atual).</summary>
        public event Action<float, float> OnEtherGained;

        /// <summary>Disparado ao gastar Éter. Parâmetros: (quantidade gasta, total atual).</summary>
        public event Action<float, float> OnEtherSpent;

        // ── Propriedades ───────────────────────────────────────────────────────
        public float CurrentEther => _currentEther;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _currentEther = startingEther;
        }

        // ─────────────────────────────────────────────────────────────────────
        #region API Pública

        /// <summary>Adiciona Éter (ex.: ao matar inimigo).</summary>
        public void Add(float amount)
        {
            if (amount <= 0f) return;
            _currentEther += amount;
            OnEtherGained?.Invoke(amount, _currentEther);
            OnEtherChanged?.Invoke(_currentEther);
        }

        /// <summary>
        /// Tenta gastar <paramref name="amount"/> de Éter.
        /// Retorna true se havia saldo suficiente e o gasto foi feito.
        /// </summary>
        public bool TrySpend(float amount)
        {
            if (amount <= 0f) return true;
            if (_currentEther < amount) return false;

            _currentEther -= amount;
            OnEtherSpent?.Invoke(amount, _currentEther);
            OnEtherChanged?.Invoke(_currentEther);
            return true;
        }

        /// <summary>Verifica se há Éter suficiente sem consumir.</summary>
        public bool HasEnough(float amount) => _currentEther >= amount;

        /// <summary>
        /// Zera o Éter e retorna o valor perdido.
        /// Chamado pelo sistema de morte (MVP 8) — o valor retornado é armazenado no Eco.
        /// </summary>
        public float TakeDeath()
        {
            float lost = _currentEther;
            _currentEther = 0f;
            OnEtherChanged?.Invoke(_currentEther);
#if UNITY_EDITOR
            Debug.Log($"[EtherWallet] Morte! Éter perdido: {lost:F0}");
#endif
            return lost;
        }

        /// <summary>
        /// Restaura Éter ao alcançar o Eco (marcador de morte).
        /// Chamado pelo DeathMarker (MVP 8).
        /// </summary>
        public void RestoreFromEco(float amount)
        {
            if (amount <= 0f) return;
            _currentEther += amount;
            OnEtherGained?.Invoke(amount, _currentEther);
            OnEtherChanged?.Invoke(_currentEther);
#if UNITY_EDITOR
            Debug.Log($"[EtherWallet] Éter recuperado do Eco: {amount:F0} (total: {_currentEther:F0})");
#endif
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Debug OnGUI

        private void OnGUI()
        {
#if UNITY_EDITOR
            GUI.color = new Color(0.8f, 0.6f, 1f);
            GUI.Label(new Rect(10, 100, 200, 20), $"Éter: {_currentEther:F0}");
            GUI.color = Color.white;
#endif
        }

        #endregion
    }
}
