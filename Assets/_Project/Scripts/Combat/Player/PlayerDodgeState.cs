using UnityEngine;
using Palsoul.Core;

namespace Palsoul.Combat
{
    /// <summary>
    /// Estado de Dodge Roll do player.
    ///
    /// Responsabilidades:
    ///   - Movimentar o player na direção do dodge durante dodgeDuration.
    ///   - Ativar/desativar a flag IsInvincible do PlayerController na janela de i-frames.
    ///   - Ao terminar, retornar ao estado correto (Idle ou Moving conforme input atual).
    ///
    /// I-frames:
    ///   A janela de invencibilidade é [iFrameStart, iFrameEnd] em segundos dentro da duração
    ///   total do dodge. Esses valores vêm do StaminaSO e são configuráveis no Inspector.
    ///   O sistema de hitbox (MVP 3) consultará PlayerController.IsInvincible para ignorar hits.
    ///
    /// Diagrama de tempo (exemplo com dodgeDuration=0.4s, iStart=0.05s, iEnd=0.30s):
    ///   |--0.05s--|====== i-frames (0.25s) ======|--0.10s--|-- fim --|
    ///   ^Enter                                              ^i-frames off
    /// </summary>
    public class PlayerDodgeState : IState
    {
        // ── Referências ────────────────────────────────────────────────────────
        private readonly PlayerController _player;
        private readonly StaminaSO        _staminaData;
        private readonly Rigidbody2D      _rb;

        // Animator hash
        private static readonly int HashIsDodging = Animator.StringToHash("IsDodging");

        // ── Estado interno do dodge ────────────────────────────────────────────
        private float   _dodgeTimer;        // tempo decorrido desde Enter()
        private Vector2 _dodgeDirection;    // direção fixada no início do dodge

        // ── Cooldown compartilhado ─────────────────────────────────────────────
        // Usamos um campo estático para que o cooldown persista entre instâncias do estado
        // (a instância é reutilizada, mas o timer é reiniciado a cada Enter)
        private float _cooldownTimer;

        /// <summary>True se o dodge ainda está em cooldown (não pode iniciar novo dodge).</summary>
        public bool IsOnCooldown => _cooldownTimer > 0f;

        // ─────────────────────────────────────────────────────────────────────
        public PlayerDodgeState(PlayerController player, StaminaSO staminaData, Rigidbody2D rb)
        {
            _player      = player;
            _staminaData = staminaData;
            _rb          = rb;
        }

        // ─────────────────────────────────────────────────────────────────────
        #region IState

        public void Enter()
        {
            _dodgeTimer = 0f;

            // Fixa a direção do dodge: usa o input atual ou a última direção válida
            _dodgeDirection = _player.MoveInput.sqrMagnitude > 0.01f
                ? _player.MoveInput.normalized
                : _player.LastMoveDirection;

            // Inicia com invencibilidade desligada — será ligada em Tick() no frame correto
            _player.IsInvincible = false;

            // Dispara parâmetro de animação
            _player.Animator.SetBool(HashIsDodging, true);
        }

        public void Tick()
        {
            _dodgeTimer += Time.deltaTime;

            // Atualiza cooldown (decrementado aqui para ser frame-accurate)
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;

            // Janela de i-frames
            bool shouldBeInvincible = _dodgeTimer >= _staminaData.iFrameStart
                                   && _dodgeTimer <  _staminaData.iFrameEnd;

            if (_player.IsInvincible != shouldBeInvincible)
            {
                _player.IsInvincible = shouldBeInvincible;

#if UNITY_EDITOR
                if (shouldBeInvincible)
                    Debug.Log($"[Dodge] I-frames ON  @ t={_dodgeTimer:F3}s");
                else if (_dodgeTimer >= _staminaData.iFrameEnd)
                    Debug.Log($"[Dodge] I-frames OFF @ t={_dodgeTimer:F3}s");
#endif
            }

            // Movimento do dodge via Rigidbody2D
            _rb.linearVelocity = _dodgeDirection * _staminaData.dodgeSpeed;

            // Fim do dodge
            if (_dodgeTimer >= _staminaData.dodgeDuration)
            {
                // Inicia cooldown antes de sair
                _cooldownTimer = _staminaData.dodgeCooldown;

                // Retorna ao estado correto baseado no input atual
                PlayerState next = _player.MoveInput.sqrMagnitude > 0.01f
                    ? PlayerState.Moving
                    : PlayerState.Idle;

                _player.TransitionTo(next);
            }
        }

        public void Exit()
        {
            // Garante que i-frames são desligados ao sair do estado
            _player.IsInvincible = false;
            _player.Animator.SetBool(HashIsDodging, false);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Chamado externamente (pelo PlayerController) para decrementar o cooldown
        /// mesmo quando o estado não está ativo.
        /// </summary>
        public void TickCooldown()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }
    }
}
