using UnityEngine;
using Palsoul.Core;

namespace Palsoul.Combat
{
    /// <summary>
    /// Estado de ataque do player — compartilhado entre ataque leve e pesado.
    /// A diferença entre os dois é apenas o AttackDataSO injetado no construtor.
    ///
    /// Fluxo por frame:
    ///   Enter()  → consome stamina, fixa direção, dispara animator trigger
    ///   Tick()   → avança timer, ativa hitbox na janela correta, detecta combo input
    ///   Exit()   → desativa hitbox, limpa flags
    ///
    /// Combo window:
    ///   Se o jogador pressionar ataque leve novamente durante a janela de combo
    ///   (após hitboxActiveEnd e antes do fim de duration), o próximo ataque é
    ///   enfileirado e disparado automaticamente ao sair deste estado.
    /// </summary>
    public class PlayerAttackState : IState
    {
        // ── Referências ────────────────────────────────────────────────────────
        private readonly PlayerController _player;
        private readonly HitboxController _hitbox;
        private readonly AttackDataSO     _attackData;
        private readonly PlayerState      _stateEnum;   // AttackLight ou AttackHeavy

        // Animator hashes
        private static readonly int HashAttackLight  = Animator.StringToHash("AttackLight");
        private static readonly int HashAttackHeavy  = Animator.StringToHash("AttackHeavy");
        private static readonly int HashAttackSpeed  = Animator.StringToHash("AttackSpeed");

        // ── Estado interno ─────────────────────────────────────────────────────
        private float   _timer;
        private bool    _hitboxFired;
        private bool    _comboQueued;     // jogador pressionou leve durante a combo window
        private bool    _inRecovery;

        // ── Combo window: intervalo de tempo (em segundos) após hitboxActiveEnd ──
        // onde um novo input de ataque leve é aceito para encadear.

        // ─────────────────────────────────────────────────────────────────────
        public PlayerAttackState(PlayerController player,
                                 HitboxController hitbox,
                                 AttackDataSO     attackData,
                                 PlayerState      stateEnum)
        {
            _player     = player;
            _hitbox     = hitbox;
            _attackData = attackData;
            _stateEnum  = stateEnum;
        }

        // ─────────────────────────────────────────────────────────────────────
        #region IState

        public void Enter()
        {
            _timer       = 0f;
            _hitboxFired = false;
            _comboQueued = false;
            _inRecovery  = false;

            // Ativa hitbox com a direção atual do player
            _hitbox.Activate(_attackData, _player.LastMoveDirection);

            // Dispara o trigger correto no Animator
            int trigger = _stateEnum == PlayerState.AttackLight ? HashAttackLight : HashAttackHeavy;
            _player.Animator.SetTrigger(trigger);

            // Ajusta velocidade da animação com base na duração configurada no SO
            // (assume que o clip base tem 1 segundo — AnimationSpeed = 1/duration)
            float clipDuration = _attackData.duration > 0f ? _attackData.duration : 1f;
            _player.Animator.SetFloat(HashAttackSpeed, 1f / clipDuration);

#if UNITY_EDITOR
            Debug.Log($"[Attack] Iniciou {_stateEnum} | dano={_attackData.baseDamage} " +
                      $"| hitbox [{_attackData.hitboxActiveStart:F2}s – {_attackData.hitboxActiveEnd:F2}s]");
#endif
        }

        public void Tick()
        {
            _timer += Time.deltaTime;

            // ── Fase de hitbox ─────────────────────────────────────────────────
            // O HitboxController já gerencia a janela internamente via seu próprio Update.
            // Aqui apenas registramos que a janela passou para habilitar a combo window.
            if (!_hitboxFired && _timer >= _attackData.hitboxActiveEnd)
                _hitboxFired = true;

            // ── Combo window (só para ataque leve) ─────────────────────────────
            // O input de combo é registrado pelo PlayerController via QueueCombo()
            // e checado aqui na saída do estado.

            // ── Recovery ───────────────────────────────────────────────────────
            float totalDuration = _attackData.duration + _attackData.recoveryTime;

            if (!_inRecovery && _timer >= _attackData.duration)
            {
                _inRecovery = true;
                // Para o movimento durante recovery (velocidade zero)
                // O PlayerController bloqueia input de movimento durante atacques via CurrentState check
            }

            // ── Fim do estado ──────────────────────────────────────────────────
            if (_timer >= totalDuration)
            {
                if (_comboQueued && _stateEnum == PlayerState.AttackLight)
                {
                    // Encadeia próximo ataque leve sem voltar para Idle
                    _player.TransitionTo(PlayerState.Idle);
                    _player.TryExecuteAttack(PlayerState.AttackLight);
                }
                else
                {
                    // Retorna ao estado de movimento correto
                    PlayerState next = _player.MoveInput.sqrMagnitude > 0.01f
                        ? PlayerState.Moving
                        : PlayerState.Idle;
                    _player.TransitionTo(next);
                }
            }
        }

        public void Exit()
        {
            _hitbox.Deactivate();
            _comboQueued = false;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Chamado pelo PlayerController quando o jogador pressiona ataque leve
        /// durante a execução deste estado. Enfileira o próximo golpe.
        /// </summary>
        public void QueueCombo()
        {
            // Só aceita combo após o hitbox fechar e antes do fim da duration
            bool inComboWindow = _hitboxFired && _timer < _attackData.duration + _attackData.comboWindowOffset;
            if (inComboWindow)
                _comboQueued = true;
        }
    }
}
