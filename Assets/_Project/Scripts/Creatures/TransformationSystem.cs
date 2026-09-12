using UnityEngine;
using Palsoul.Core;
using Palsoul.Combat;

namespace Palsoul.Creatures
{
    /// <summary>
    /// Gerencia a Forma Ativa do jogador — a criatura que o jogador "é" em campo.
    /// GDD seção 5.4: ao ativar uma criatura como Forma Ativa no Ancoradouro,
    /// o sprite/animator/moveset do jogador são substituídos pelos da criatura.
    ///
    /// Responsabilidades:
    ///   - Receber um CreatureDefinitionSO e aplicá-lo ao PlayerController.
    ///   - Trocar RuntimeAnimatorController, AttackDataSO leve/pesado e HP máximo.
    ///   - Expor a Forma Ativa atual para o SquadController e SaveSystem.
    ///   - Disparar eventos para a UI (HUD de squad) atualizar.
    ///
    /// Esta classe é o ponto de entrada chamado pelo Ancoradouro (MVP 7) e pelo
    /// SquadController (troca de controle em campo).
    ///
    /// Setup: adicione ao mesmo GO do PlayerController.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(HealthSystem))]
    public class TransformationSystem : MonoBehaviour
    {
        // ── Referências ────────────────────────────────────────────────────────
        private PlayerController _playerController;
        private HealthSystem     _healthSystem;
        private SpriteRenderer   _spriteRenderer;

        // ── Estado ─────────────────────────────────────────────────────────────
        private CreatureDefinitionSO _activeForm;

        /// <summary>A criatura que o jogador está sendo atualmente.</summary>
        public CreatureDefinitionSO ActiveForm => _activeForm;

        /// <summary>True se há uma Forma Ativa definida.</summary>
        public bool HasActiveForm => _activeForm != null;

        // ── Eventos ────────────────────────────────────────────────────────────
        /// <summary>Disparado ao trocar de Forma Ativa. Parâmetro: nova definição (pode ser null = forma humana).</summary>
        public event System.Action<CreatureDefinitionSO> OnFormChanged;

        // ── Animator padrão (forma humana — salvo antes da primeira transformação) ──
        private RuntimeAnimatorController _humanAnimatorController;
        private AttackDataSO              _humanLightAttack;
        private AttackDataSO              _humanHeavyAttack;
        private float                     _humanMaxHP;

        // ─────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _healthSystem     = GetComponent<HealthSystem>();
            _spriteRenderer   = GetComponent<SpriteRenderer>();

            // Salva o estado padrão do jogador para poder restaurar se necessário
            if (_playerController != null)
            {
                _humanAnimatorController = _playerController.Animator.runtimeAnimatorController;
                _humanLightAttack        = _playerController.LightAttackData;
                _humanHeavyAttack        = _playerController.HeavyAttackData;
            }

            if (_healthSystem != null)
                _humanMaxHP = _healthSystem.MaxHP;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region API Pública

        /// <summary>
        /// Ativa uma criatura como Forma Ativa.
        /// Chamado pelo Ancoradouro (MVP 7) ou pelo SquadController (troca em campo).
        /// </summary>
        /// <param name="definition">Definição da espécie. Null = restaura forma humana.</param>
        public void SetActiveForm(CreatureDefinitionSO definition)
        {
            _activeForm = definition;

            if (definition == null)
            {
                RestoreHumanForm();
                return;
            }

            ApplyCreatureForm(definition);
            OnFormChanged?.Invoke(definition);

#if UNITY_EDITOR
            Debug.Log($"[TransformationSystem] Forma Ativa definida: {definition.creatureName}");
#endif
        }

        /// <summary>
        /// Restaura a forma humana (sem criatura ativa).
        /// Raramente usado no gameplay normal — mais para debug e tela de morte.
        /// </summary>
        public void RestoreHumanForm()
        {
            if (_playerController != null)
            {
                _playerController.Animator.runtimeAnimatorController = _humanAnimatorController;
                _playerController.SetAttackData(_humanLightAttack, _humanHeavyAttack);
                transform.localScale = Vector3.one;
            }

            if (_healthSystem != null)
                _healthSystem.SetMaxHP(_humanMaxHP, keepRatio: false);

            _activeForm = null;
            OnFormChanged?.Invoke(null);

#if UNITY_EDITOR
            Debug.Log("[TransformationSystem] Forma humana restaurada.");
#endif
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Aplicação da Forma

        private void ApplyCreatureForm(CreatureDefinitionSO def)
        {
            if (_playerController == null) return;

            // ── 1. AnimatorController ──────────────────────────────────────────
            if (def.animatorController != null)
                _playerController.Animator.runtimeAnimatorController = def.animatorController;
            else
                Debug.LogWarning($"[TransformationSystem] {def.creatureName} não tem AnimatorController atribuído. Mantendo o atual.");

            // ── 2. Movesets (AttackData) ───────────────────────────────────────
            _playerController.SetAttackData(def.lightAttack, def.heavyAttack);

            // ── 3. HP máximo ───────────────────────────────────────────────────
            // O HP é ajustado para o baseHP da criatura.
            // keepRatio: false — a Forma Ativa começa com HP cheio ao transformar.
            if (_healthSystem != null)
                _healthSystem.SetMaxHP(def.baseHP, keepRatio: false);

            // ── 4. Escala do sprite ────────────────────────────────────────────
            transform.localScale = Vector3.one * def.spriteScale;

            // ── 5. Velocidade de movimento ─────────────────────────────────────
            // PlayerMovementSO é fixo por enquanto; a velocidade base da criatura
            // é aplicada diretamente no Rigidbody via override no PlayerController.
            _playerController.SetMoveSpeedOverride(def.baseMoveSpeed);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Debug

        private void OnGUI()
        {
#if UNITY_EDITOR
            string formName = _activeForm != null ? _activeForm.creatureName : "Humano";
            GUI.color = Color.cyan;
            GUI.Label(new Rect(10, 62, 250, 20), $"Forma Ativa: {formName}");
            GUI.color = Color.white;
#endif
        }

        #endregion
    }
}
