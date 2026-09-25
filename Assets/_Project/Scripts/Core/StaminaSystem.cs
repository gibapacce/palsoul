using System;
using UnityEngine;

namespace Palsoul.Core
{
    /// <summary>
    /// Gerencia a barra de stamina do player: consumo, regeneração com delay e eventos para a UI.
    ///
    /// Regras:
    ///   - Toda constante de balanceamento vem do StaminaSO injetado.
    ///   - Comunicação com UI e Áudio exclusivamente via C# events (sem referência direta).
    ///   - Pode ser reutilizado em criaturas/inimigos futuros (sem acoplamento ao PlayerController).
    ///
    /// Setup no prefab do Player:
    ///   Adicione este MonoBehaviour ao mesmo GameObject do PlayerController e arraste o StaminaData SO.
    /// </summary>
    public class StaminaSystem : MonoBehaviour
    {
        // ── Dados de design ────────────────────────────────────────────────────
        [Header("Dados de Stamina")]
        [Tooltip("ScriptableObject com maxStamina, regenRate, regenDelay e parâmetros de dodge.")]
        [SerializeField] private StaminaSO staminaData;

        // ── Estado interno ─────────────────────────────────────────────────────
        private float _currentStamina;
        private float _regenDelayTimer;   // countdown até começar a regenerar
        private bool  _isRegenerating;

        // ── Eventos (UI e Áudio assinam aqui — Core não referencia UI diretamente) ──
        /// <summary>Disparado sempre que a stamina muda. Parâmetros: (stamina atual, stamina máxima).</summary>
        public event Action<float, float> OnStaminaChanged;

        /// <summary>Disparado quando a stamina chega a zero.</summary>
        public event Action OnStaminaDepleted;

        /// <summary>Disparado quando a stamina começa a regenerar após o delay.</summary>
        public event Action OnStaminaRegenStarted;

        // ── Propriedades públicas ──────────────────────────────────────────────
        public float CurrentStamina => _currentStamina;
        public float MaxStamina     => staminaData != null ? staminaData.maxStamina : 0f;
        public float NormalizedStamina => MaxStamina > 0f ? _currentStamina / MaxStamina : 0f;

        // ─────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (staminaData == null)
            {
                Debug.LogError("[StaminaSystem] StaminaSO não atribuído! " +
                               "Arraste o asset StaminaData para o campo no Inspector.", this);
                return;
            }

            _currentStamina = staminaData.maxStamina;
            _regenDelayTimer = 0f;
            _isRegenerating  = true;
        }

        private void Update()
        {
            if (staminaData == null) return;
            HandleRegeneration();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region API Pública

        /// <summary>
        /// Tenta consumir <paramref name="amount"/> de stamina.
        /// Retorna <c>true</c> se havia stamina suficiente e o consumo foi feito.
        /// Retorna <c>false</c> se stamina insuficiente (sem consumir nada).
        /// </summary>
        public bool TryConsume(float amount)
        {
            if (amount < 0 || !float.IsFinite(amount)) return false;
            if (staminaData == null) return false;
            if (_currentStamina < amount) return false;

            _currentStamina  -= amount;
            _currentStamina   = Mathf.Max(_currentStamina, 0f);
            _regenDelayTimer  = staminaData.regenDelay;
            _isRegenerating   = false;

            OnStaminaChanged?.Invoke(_currentStamina, staminaData.maxStamina);

            if (_currentStamina <= 0f)
                OnStaminaDepleted?.Invoke();

            return true;
        }

        /// <summary>
        /// Restaura toda a stamina instantaneamente (usado ao descansar no Ancoradouro — MVP 6).
        /// </summary>
        public void RestoreFull()
        {
            if (staminaData == null) return;
            _currentStamina  = staminaData.maxStamina;
            _regenDelayTimer = 0f;
            _isRegenerating  = true;
            OnStaminaChanged?.Invoke(_currentStamina, staminaData.maxStamina);
        }

        /// <summary>
        /// Verifica se há stamina suficiente para uma ação sem consumi-la.
        /// </summary>
        public bool HasEnough(float amount) => _currentStamina >= amount;

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Regeneração

        private void HandleRegeneration()
        {
            // Stamina já cheia — nada a fazer
            if (_currentStamina >= staminaData.maxStamina) return;

            // Countdown do delay pós-ação
            if (_regenDelayTimer > 0f)
            {
                _regenDelayTimer -= Time.deltaTime;
                return;
            }

            // Começa a regenerar
            if (!_isRegenerating)
            {
                _isRegenerating = true;
                OnStaminaRegenStarted?.Invoke();
            }

            float prev = _currentStamina;
            _currentStamina += staminaData.regenRate * Time.deltaTime;
            _currentStamina  = Mathf.Min(_currentStamina, staminaData.maxStamina);

            // Só dispara evento se o valor realmente mudou (evita spam de eventos)
            if (!Mathf.Approximately(prev, _currentStamina))
                OnStaminaChanged?.Invoke(_currentStamina, staminaData.maxStamina);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Debug

        private void OnGUI()
        {
#if UNITY_EDITOR
            // Barra de stamina simples no Editor para teste sem HUD implementado
            if (staminaData == null) return;
            float ratio = NormalizedStamina;
            GUI.color = Color.yellow;
            GUI.Box(new Rect(10, 40, 200 * ratio, 20), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(10, 40, 200, 20),
                $"Stamina: {_currentStamina:F0} / {staminaData.maxStamina:F0}");
#endif
        }

        #endregion
    }
}
