using UnityEngine;
using UnityEngine.InputSystem;
using Palsoul.Core;

namespace Palsoul.Creatures
{
    /// <summary>
    /// Gerencia o squad de 2 criaturas do jogador em campo.
    /// GDD seção 5.4:
    ///   - Forma Ativa: criatura controlada diretamente pelo jogador.
    ///   - Squad Autônomo: segunda criatura, luta sozinha com SquadMemberAI.
    ///   - Troca de controle via botão "Swap" (Tab / LB): sem custo, tática pura.
    ///
    /// Responsabilidades:
    ///   - Manter os 2 slots de squad (Forma Ativa + Autônoma).
    ///   - Processar input de troca (OnSwap).
    ///   - Ao trocar: slot ativo → SquadMemberAI (IsPlayerControlled=false),
    ///               slot autônomo → TransformationSystem.SetActiveForm() + IsPlayerControlled=true.
    ///   - Expor API para o Ancoradouro (MVP 7) configurar o squad inicial.
    ///
    /// Setup: adicione ao mesmo GO do PlayerController.
    /// </summary>
    [RequireComponent(typeof(TransformationSystem))]
    public class SquadController : MonoBehaviour
    {
        // ── Referências ────────────────────────────────────────────────────────
        private TransformationSystem _transformationSystem;

        // ── Slots do squad ─────────────────────────────────────────────────────
        // Slot 0 = Forma Ativa (controlada pelo jogador)
        // Slot 1 = Criatura Autônoma
        private CreatureDefinitionSO[] _definitions  = new CreatureDefinitionSO[2];
        private SquadMemberAI[]        _squadMembers = new SquadMemberAI[2];
        private int                    _activeSlot   = 0;

        // ── Prefab da criatura do squad (o GO que representa o membro autônomo) ──
        [Header("Squad Member")]
        [Tooltip("Prefab da criatura do squad (com SquadMemberAI, HealthSystem, Rigidbody2D, etc.). " +
                 "Instanciado em runtime quando o slot 1 é preenchido.")]
        [SerializeField] private GameObject squadMemberPrefab;

        [Tooltip("Offset de spawn da criatura do squad em relação ao player.")]
        [SerializeField] private Vector2 squadSpawnOffset = new Vector2(1.5f, 0f);

        // ── Cooldown de troca (evita spam) ─────────────────────────────────────
        [Header("Troca")]
        [Tooltip("Cooldown mínimo entre trocas de controle (segundos).")]
        [Min(0f)]
        [SerializeField] private float swapCooldown = 0.5f;
        private float _swapCooldownTimer;

        // ── Eventos ────────────────────────────────────────────────────────────
        /// <summary>Disparado ao trocar de Forma Ativa em campo. Parâmetros: (nova forma, slot ativo).</summary>
        public event System.Action<CreatureDefinitionSO, int> OnSquadSwapped;

        // ── Propriedades ───────────────────────────────────────────────────────
        public CreatureDefinitionSO ActiveFormDefinition  => _definitions[_activeSlot];
        public CreatureDefinitionSO PassiveFormDefinition => _definitions[1 - _activeSlot];
        public int ActiveSlot => _activeSlot;

        // ─────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            _transformationSystem = GetComponent<TransformationSystem>();
        }

        private void Update()
        {
            if (_swapCooldownTimer > 0f)
                _swapCooldownTimer -= Time.deltaTime;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Input Callback (Send Messages)

        /// <summary>
        /// Chamado pelo PlayerInput quando a action "Swap" é pressionada (Tab / LB).
        /// </summary>
        private void OnSwap(InputValue value)
        {
            if (!value.isPressed) return;
            if (_swapCooldownTimer > 0f) return;
            if (_definitions[0] == null || _definitions[1] == null)
            {
                Debug.Log("[SquadController] Squad incompleto — precisa de 2 criaturas para trocar.");
                return;
            }

            SwapActiveSlot();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Troca de Controle

        private void SwapActiveSlot()
        {
            int previousSlot = _activeSlot;
            int newSlot      = 1 - _activeSlot;

            // Criatura anterior → IA autônoma
            if (_squadMembers[previousSlot] != null)
                _squadMembers[previousSlot].IsPlayerControlled = false;

            // Nova criatura ativa → PlayerController
            _activeSlot = newSlot;
            _transformationSystem.SetActiveForm(_definitions[_activeSlot]);

            // Criatura nova → marca como controlada pelo player (desativa IA)
            if (_squadMembers[_activeSlot] != null)
                _squadMembers[_activeSlot].IsPlayerControlled = true;

            _swapCooldownTimer = swapCooldown;
            OnSquadSwapped?.Invoke(_definitions[_activeSlot], _activeSlot);

#if UNITY_EDITOR
            Debug.Log($"[SquadController] Troca para slot {_activeSlot}: {_definitions[_activeSlot]?.creatureName}");
#endif
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region API Pública (chamada pelo Ancoradouro — MVP 7)

        /// <summary>
        /// Configura o squad completo (chamado ao sair do Ancoradouro ou ao iniciar o jogo).
        /// </summary>
        /// <param name="slot0">Criatura do slot 0 (Forma Ativa inicial).</param>
        /// <param name="slot1">Criatura do slot 1 (Autônoma inicial). Pode ser null.</param>
        public void SetSquad(CreatureDefinitionSO slot0, CreatureDefinitionSO slot1)
        {
            _definitions[0] = slot0;
            _definitions[1] = slot1;
            _activeSlot     = 0;

            // Aplica Forma Ativa
            _transformationSystem.SetActiveForm(slot0);

            // Instancia criatura do squad autônomo se slot1 preenchido
            SetupSquadMember(1, slot1);

#if UNITY_EDITOR
            Debug.Log($"[SquadController] Squad definido: [{slot0?.creatureName}] + [{slot1?.creatureName}]");
#endif
        }

        /// <summary>
        /// Troca apenas a Forma Ativa (slot 0) sem alterar o slot 1.
        /// Chamado pelo Ancoradouro quando o jogador troca a forma principal.
        /// </summary>
        public void SetActiveFormOnly(CreatureDefinitionSO definition)
        {
            _definitions[_activeSlot] = definition;
            _transformationSystem.SetActiveForm(definition);
        }

        /// <summary>
        /// Define a criatura do slot autônomo (slot 1).
        /// Destrói o GO anterior se existir.
        /// </summary>
        public void SetPassiveSlot(CreatureDefinitionSO definition)
        {
            int passiveSlot = 1 - _activeSlot;
            _definitions[passiveSlot] = definition;
            SetupSquadMember(passiveSlot, definition);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Setup de Membro do Squad

        private void SetupSquadMember(int slot, CreatureDefinitionSO definition)
        {
            // Destrói membro anterior se existir
            if (_squadMembers[slot] != null)
            {
                Destroy(_squadMembers[slot].gameObject);
                _squadMembers[slot] = null;
            }

            if (definition == null || squadMemberPrefab == null) return;

            // Instancia o prefab próximo ao player
            Vector3 spawnPos = transform.position + (Vector3)squadSpawnOffset;
            var go = Instantiate(squadMemberPrefab, spawnPos, Quaternion.identity);
            go.name = $"SquadMember_{definition.creatureName}";

            var ai = go.GetComponent<SquadMemberAI>();
            if (ai == null)
            {
                Debug.LogError("[SquadController] squadMemberPrefab não tem SquadMemberAI!", this);
                Destroy(go);
                return;
            }

            // Configura o Animator com o controller da criatura
            var animator = go.GetComponent<Animator>();
            if (animator != null && definition.animatorController != null)
                animator.runtimeAnimatorController = definition.animatorController;

            // Aplica escala da criatura
            go.transform.localScale = Vector3.one * definition.spriteScale;

            // Configura o HealthSystem com o HP base da criatura
            var health = go.GetComponent<HealthSystem>();
            if (health != null)
                health.SetMaxHP(definition.baseHP, keepRatio: false);

            _squadMembers[slot] = ai;

            // Slot ativo = player controlado; slot passivo = IA autônoma
            ai.IsPlayerControlled = (slot == _activeSlot);

#if UNITY_EDITOR
            Debug.Log($"[SquadController] SquadMember slot {slot} criado: {definition.creatureName} | " +
                      $"PlayerControlled={ai.IsPlayerControlled}");
#endif
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Debug

        private void OnGUI()
        {
#if UNITY_EDITOR
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, 80, 300, 20),
                $"Squad: [{_definitions[0]?.creatureName ?? "—"}] vs [{_definitions[1]?.creatureName ?? "—"}] | Ativo: slot {_activeSlot}");
            GUI.color = Color.white;
#endif
        }

        #endregion
    }
}
