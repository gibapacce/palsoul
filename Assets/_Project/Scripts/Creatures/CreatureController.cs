using UnityEngine;
using Palsoul.Core;

namespace Palsoul.Creatures
{
    /// <summary>
    /// MonoBehaviour presente em criaturas selvagens capturáveis.
    /// Estende o comportamento do EnemyController com propriedades específicas
    /// de criatura: definição de espécie, status effects, flag de fúria pós-falha
    /// de captura, e exposição de HP% e IsAlerted para a CaptureFormula.
    ///
    /// Composição em vez de herança de EnemyController para manter flexibilidade:
    /// uma criatura selvagem tem um EnemyController + este componente no mesmo GO.
    ///
    /// Setup no prefab de criatura:
    ///   - Todos os componentes de EnemyController (Rigidbody2D, Animator, etc.)
    ///   - Este componente com CreatureDefinitionSO atribuído
    ///   - Tag "Creature" no GameObject raiz
    /// </summary>
    [RequireComponent(typeof(Combat.EnemyController))]
    [RequireComponent(typeof(HealthSystem))]
    public class CreatureController : MonoBehaviour
    {
        // ── Definição da espécie ───────────────────────────────────────────────
        [Header("Espécie")]
        [Tooltip("ScriptableObject que define esta espécie (stats, elemento, moveset, etc.)")]
        [SerializeField] private CreatureDefinitionSO _definition;

        public CreatureDefinitionSO Definition => _definition;

        // ── Referências ────────────────────────────────────────────────────────
        private HealthSystem          _health;
        private Combat.EnemyController _enemyCtrl;

        // ── Status Effects ─────────────────────────────────────────────────────
        private CaptureStatus _activeStatus = CaptureStatus.None;
        public  CaptureStatus ActiveStatus  => _activeStatus;

        // ── Fúria pós-falha de captura ─────────────────────────────────────────
        private bool  _isEnraged;
        private float _enrageTimer;

        [Header("Fúria (falha de captura)")]
        [Tooltip("Duração do buff de fúria após uma esfera falhar (segundos).")]
        [SerializeField] private float enrageDuration  = 5f;

        [Tooltip("Multiplicador de dano durante a fúria.")]
        [Min(1f)]
        [SerializeField] private float enrageDamageMultiplier = 1.5f;

        public bool  IsEnraged              => _isEnraged;
        public float EnrageDamageMultiplier => _isEnraged ? enrageDamageMultiplier : 1f;

        // ── Propriedades para a CaptureFormula ────────────────────────────────
        /// <summary>HP normalizado atual (0–1). Passado para CaptureFormula.Calculate().</summary>
        public float HPNormalized => _health != null ? _health.NormalizedHP : 1f;

        /// <summary>Proxy de IsAlerted do EnemyController (não alertado = elegível para furtivo).</summary>
        public bool IsAlerted => _enemyCtrl != null && _enemyCtrl.IsAlerted;

        /// <summary>True se a criatura pode ser capturada (viva e não é chefe).</summary>
        public bool IsCapturable => _health != null && !_health.IsDead;

        // ── Eventos ────────────────────────────────────────────────────────────
        /// <summary>Disparado quando a criatura é capturada com sucesso.</summary>
        public event System.Action<CreatureDefinitionSO> OnCaptured;

        /// <summary>Disparado quando uma esfera falha (fúria ativada).</summary>
        public event System.Action OnCaptureFailed;

        // ─────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            _health    = GetComponent<HealthSystem>();
            _enemyCtrl = GetComponent<Combat.EnemyController>();

            if (_definition == null)
                Debug.LogError($"[CreatureController] CreatureDefinitionSO não atribuído em {gameObject.name}!", this);
        }

        private void Update()
        {
            TickEnrage();
            TickStatusEffects();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Captura

        /// <summary>
        /// Chamado pelo CaptureSystem quando a captura é bem-sucedida.
        /// Desativa o GameObject e notifica via evento.
        /// </summary>
        public void ApplyCapture()
        {
            if (!IsCapturable) return;

            OnCaptured?.Invoke(_definition);

#if UNITY_EDITOR
            Debug.Log($"[CreatureController] {_definition?.creatureName} foi capturada!");
#endif
            // Desativa o GO (não destrói — pode ser reativado pelo reset de mundo)
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Chamado pelo CaptureSystem quando a captura falha.
        /// Ativa o estado de fúria (buff de dano temporário — GDD 5.2).
        /// </summary>
        public void ApplyCaptureFailed()
        {
            _isEnraged   = true;
            _enrageTimer = enrageDuration;

            OnCaptureFailed?.Invoke();

#if UNITY_EDITOR
            Debug.Log($"[CreatureController] Captura falhou! {_definition?.creatureName} entrou em fúria por {enrageDuration}s.");
#endif
            // Flash visual de fúria (vermelho intenso)
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = new Color(1f, 0.2f, 0.2f, 1f);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Status Effects

        /// <summary>Aplica um status effect à criatura.</summary>
        public void ApplyStatus(CaptureStatus status)
        {
            _activeStatus |= status;
        }

        /// <summary>Remove um status effect.</summary>
        public void RemoveStatus(CaptureStatus status)
        {
            _activeStatus &= ~status;
        }

        /// <summary>Verifica se um status está ativo.</summary>
        public bool HasStatus(CaptureStatus status) => _activeStatus.HasFlag(status);

        private void TickStatusEffects()
        {
            // Placeholder: statuses sem duração própria no MVP.
            // No MVP futuro cada status terá um timer individual.
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Fúria

        private void TickEnrage()
        {
            if (!_isEnraged) return;

            _enrageTimer -= Time.deltaTime;
            if (_enrageTimer <= 0f)
            {
                _isEnraged = false;

                // Restaura cor original
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = Color.white;

#if UNITY_EDITOR
                Debug.Log($"[CreatureController] {_definition?.creatureName} saiu da fúria.");
#endif
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (_definition == null) return;

            // Mostra limiar de HP viável para captura
            float threshold = _definition.captureHPThreshold;
            float currentHP = HPNormalized;
            bool capturable = currentHP <= threshold;

            Gizmos.color = capturable ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.15f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1.8f,
                $"{_definition.creatureName} HP:{currentHP:P0}{(_isEnraged ? " [ENRAGED]" : "")}");
#endif
        }

        #endregion
    }
}
