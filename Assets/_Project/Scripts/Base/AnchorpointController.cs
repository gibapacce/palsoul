using UnityEngine;
using UnityEngine.InputSystem;
using Palsoul.Core;
using Palsoul.Progression;
using Palsoul.Creatures;

namespace Palsoul.Base
{
    /// <summary>
    /// Controlador do Ancoradouro — checkpoint central do PalSoul.
    /// GDD seção 5.5 + 14: equivalente ao bonfire (Souls) + Palbox (Palworld).
    ///
    /// Ações disponíveis ao jogador:
    ///   1. Descansar — cura HP/Stamina, restaura itens de cura, reseta o mundo.
    ///   2. Gastar Éter em atributos (1 atributo no MVP).
    ///   3. Trocar Forma Ativa (chamar SquadController.SetActiveFormOnly).
    ///   4. Viagem rápida entre Ancoradouros descobertos (placeholder no MVP).
    ///
    /// Setup no prefab do Ancoradouro:
    ///   - Adicione este componente.
    ///   - Atribua o AnchorpointDataSO no Inspector.
    ///   - Adicione um Collider2D com IsTrigger = true (raio de interação visual).
    ///   - O AnchorpointMenuUI deve estar no mesmo GO ou filho.
    /// </summary>
    public class AnchorpointController : MonoBehaviour
    {
        // ── Dados ──────────────────────────────────────────────────────────────
        [Header("Dados")]
        [SerializeField] private AnchorpointDataSO data;

        // ── Referências de cena (encontradas em runtime) ───────────────────────
        private WorldResetSystem _worldReset;
        private AnchorpointMenuUI _menuUI;

        // ── Referências do player (encontradas via tag) ────────────────────────
        private Core.HealthSystem       _playerHealth;
        private Core.StaminaSystem      _playerStamina;
        private EtherWallet             _etherWallet;
        private SquadController         _squadController;

        // ── Estado ─────────────────────────────────────────────────────────────
        private bool _playerInRange;
        private bool _isDiscovered;

        // Níveis de atributos atuais (índice = posição em data.availableUpgrades)
        private int[] _attributeLevels;

        // ── Eventos ────────────────────────────────────────────────────────────
        /// <summary>Disparado quando o jogador descansa. Parâmetro: este Ancoradouro.</summary>
        public event System.Action<AnchorpointController> OnPlayerRested;

        /// <summary>Disparado ao fazer upgrade de atributo. Parâmetros: (SO do atributo, novo nível).</summary>
        public event System.Action<AttributeUpgradeSO, int> OnAttributeUpgraded;

        /// <summary>Disparado ao descobrir este Ancoradouro pela primeira vez.</summary>
        public event System.Action<AnchorpointController> OnDiscovered;

        // ── Propriedades ───────────────────────────────────────────────────────
        public AnchorpointDataSO Data        => data;
        public bool              IsDiscovered => _isDiscovered;
        public int[]             AttributeLevels => _attributeLevels;

        // ─────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            _menuUI      = GetComponentInChildren<AnchorpointMenuUI>();
            _worldReset  = FindFirstObjectByType<WorldResetSystem>();

            if (data == null)
                Debug.LogError($"[AnchorpointController] AnchorpointDataSO não atribuído em {gameObject.name}!", this);

            // Inicializa array de níveis de atributos
            int upgradeCount = data != null && data.availableUpgrades != null
                ? data.availableUpgrades.Length : 0;
            _attributeLevels = new int[upgradeCount];
        }

        private void Start()
        {
            // Localiza componentes do player
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                _playerHealth   = playerGO.GetComponent<Core.HealthSystem>();
                _playerStamina  = playerGO.GetComponent<Core.StaminaSystem>();
                _etherWallet    = playerGO.GetComponent<EtherWallet>();
                _squadController = playerGO.GetComponent<SquadController>();
            }
            else
            {
                Debug.LogWarning("[AnchorpointController] Nenhum Player encontrado!", this);
            }

            // Começa com menu fechado
            _menuUI?.Hide();
        }

        private void Update()
        {
            if (!_playerInRange) return;

            // Verifica input de interação (tecla E / botão South)
            // O PlayerInput usa Send Messages, mas este componente está num GO diferente.
            // Usamos o Input System diretamente aqui para simplificar.
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                OpenMenu();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Trigger de Proximidade

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInRange = true;

            // Descoberta do Ancoradouro
            if (!_isDiscovered)
            {
                _isDiscovered = true;
                OnDiscovered?.Invoke(this);
#if UNITY_EDITOR
                Debug.Log($"[AnchorpointController] {data?.anchorpointName} descoberto!");
#endif
            }

            _menuUI?.ShowPrompt();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInRange = false;
            CloseMenu();
            _menuUI?.HidePrompt();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Menu

        private void OpenMenu()
        {
            _menuUI?.Show(this);
        }

        public void CloseMenu()
        {
            _menuUI?.Hide();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Ações do Ancoradouro

        /// <summary>
        /// Ação "Descansar":
        ///   - Cura HP e Stamina do player ao máximo.
        ///   - Reseta o mundo (respawna inimigos não-chefe).
        ///   - Dispara OnPlayerRested para outros sistemas (ex.: recarregar esferas de cura).
        /// </summary>
        public void Rest()
        {
            // Cura HP
            _playerHealth?.RestoreFull();

            // Restaura Stamina
            _playerStamina?.RestoreFull();

            // Reseta o mundo
            _worldReset?.ResetWorld();

            OnPlayerRested?.Invoke(this);

#if UNITY_EDITOR
            Debug.Log($"[AnchorpointController] Player descansou em '{data?.anchorpointName}'. Mundo resetado.");
#endif
        }

        /// <summary>
        /// Ação "Upgrade de Atributo":
        ///   - Verifica se o jogador tem Éter suficiente.
        ///   - Consome Éter e incrementa o nível do atributo.
        ///   - Aplica o efeito no player (HP, Stamina, etc.).
        /// </summary>
        /// <param name="upgradeIndex">Índice em data.availableUpgrades.</param>
        public void UpgradeAttribute(int upgradeIndex)
        {
            if (data == null || data.availableUpgrades == null) return;
            if (upgradeIndex < 0 || upgradeIndex >= data.availableUpgrades.Length) return;

            var upgrade = data.availableUpgrades[upgradeIndex];
            int currentLevel = _attributeLevels[upgradeIndex];

            if (currentLevel >= upgrade.maxLevel)
            {
                Debug.Log($"[AnchorpointController] {upgrade.attributeName} já está no nível máximo.");
                return;
            }

            float cost = upgrade.GetCostForLevel(currentLevel);
            if (_etherWallet == null || !_etherWallet.TrySpend(cost))
            {
                Debug.Log($"[AnchorpointController] Éter insuficiente! Necessário: {cost:F0}");
                return;
            }

            _attributeLevels[upgradeIndex]++;
            int newLevel = _attributeLevels[upgradeIndex];

            // Aplica efeito no player
            ApplyAttributeEffect(upgrade, newLevel);

            OnAttributeUpgraded?.Invoke(upgrade, newLevel);

#if UNITY_EDITOR
            Debug.Log($"[AnchorpointController] {upgrade.attributeName} → nível {newLevel} " +
                      $"(custo: {cost:F0} Éter)");
#endif
        }

        /// <summary>
        /// Aplica o efeito de um atributo no player baseado no tipo e nível.
        /// </summary>
        private void ApplyAttributeEffect(AttributeUpgradeSO upgrade, int level)
        {
            float totalValue = upgrade.GetTotalValueAtLevel(level);

            switch (upgrade.attributeType)
            {
                case Progression.AttributeType.Vigor:
                    // Aumenta HP máximo (base + upgrade)
                    _playerHealth?.SetMaxHP(100f + totalValue, keepRatio: true);
                    break;

                case Progression.AttributeType.ActionVigor:
                    // Aumenta Stamina máxima — StaminaSystem não tem SetMaxStamina ainda,
                    // mas o SO tem maxStamina como campo público que pode ser editado em runtime.
                    // Por ora logamos; a implementação completa virá com o SaveSystem.
                    Debug.Log($"[AnchorpointController] ActionVigor nível {level}: +{totalValue} stamina (aplicação completa no MVP 11).");
                    break;

                case Progression.AttributeType.Strength:
                case Progression.AttributeType.Dexterity:
                case Progression.AttributeType.ElementalAffinity:
                    // Afetam dano — será aplicado pelo sistema de ataque no MVP futuro.
                    Debug.Log($"[AnchorpointController] {upgrade.attributeType} nível {level}: +{totalValue} (aplicação completa no MVP futuro).");
                    break;
            }
        }

        /// <summary>
        /// Ação "Trocar Forma Ativa":
        ///   Chama SquadController.SetActiveFormOnly com a nova definição.
        ///   Chamado pela UI com a criatura selecionada.
        /// </summary>
        public void SwapActiveForm(Core.CreatureDefinitionSO definition)
        {
            if (_squadController == null)
            {
                Debug.LogWarning("[AnchorpointController] SquadController não encontrado no player!");
                return;
            }

            _squadController.SetActiveFormOnly(definition);

#if UNITY_EDITOR
            Debug.Log($"[AnchorpointController] Forma Ativa trocada para: {definition?.creatureName}");
#endif
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Helpers Públicos

        /// <summary>Retorna o custo do próximo nível de um atributo.</summary>
        public float GetNextUpgradeCost(int upgradeIndex)
        {
            if (data?.availableUpgrades == null) return 0f;
            if (upgradeIndex < 0 || upgradeIndex >= data.availableUpgrades.Length) return 0f;
            return data.availableUpgrades[upgradeIndex].GetCostForLevel(_attributeLevels[upgradeIndex]);
        }

        /// <summary>Nível atual de um atributo.</summary>
        public int GetAttributeLevel(int upgradeIndex)
        {
            if (_attributeLevels == null || upgradeIndex >= _attributeLevels.Length) return 0;
            return _attributeLevels[upgradeIndex];
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (data == null) return;
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, data.interactionRadius);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f,
                $"⚓ {data.anchorpointName}{(_isDiscovered ? "" : " [?]")}");
#endif
        }

        #endregion
    }
}
