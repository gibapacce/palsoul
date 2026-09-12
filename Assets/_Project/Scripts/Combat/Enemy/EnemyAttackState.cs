using UnityEngine;

namespace Palsoul.Combat
{
    /// <summary>
    /// Estado Attack do inimigo.
    /// Para o movimento, dispara o HitboxController com o AttackData do EnemyDataSO,
    /// aguarda a duração total do ataque e volta para Chase.
    /// Seta o cooldown de ataque no EnemyController para evitar spam.
    ///
    /// Telegraph: o inimigo para completamente antes de atacar (windup visual claro,
    /// conforme GDD seção 5.7 — "todo ataque com telegraph de no mínimo N frames").
    /// </summary>
    public class EnemyAttackState : IState
    {
        private readonly EnemyController _enemy;

        private float _attackTimer;
        private bool  _attackStarted;

        // Duração de windup antes do hitbox ativar (telegraph visual)
        // Usa hitboxActiveStart do AttackDataSO como duração do "freeze" pré-ataque
        private Vector2 _attackDirection;

        public EnemyAttackState(EnemyController enemy) => _enemy = enemy;

        public void Enter()
        {
            _attackTimer  = 0f;
            _attackStarted = false;

            _enemy.StopMovement();

            // Fixa a direção do ataque no momento do Enter (não acompanha o player durante o swing)
            _attackDirection = _enemy.DirectionToPlayer();

            _enemy.Animator.SetBool(EnemyController.HashIsMoving,   false);
            _enemy.Animator.SetBool(EnemyController.HashIsAttacking, true);

            // Ativa hitbox imediatamente — o HitboxController gerencia a janela internamente
            if (_enemy.Data.attackData != null)
                _enemy.Hitbox.Activate(_enemy.Data.attackData, _attackDirection);
            else
                Debug.LogWarning($"[EnemyAttackState] AttackData não atribuído em {_enemy.gameObject.name}!");

#if UNITY_EDITOR
            Debug.Log($"[EnemyAttack] {_enemy.Data.enemyName} atacou na direção {_attackDirection}");
#endif
        }

        public void Tick()
        {
            if (_enemy.Data.attackData == null) return;

            _attackTimer += Time.deltaTime;

            float totalDuration = _enemy.Data.attackData.duration
                                + _enemy.Data.attackData.recoveryTime;

            if (_attackTimer >= totalDuration)
            {
                // Seta cooldown antes de sair
                _enemy.AttackCooldownTimer = _enemy.Data.attackCooldown;
                _enemy.TransitionTo(EnemyState.Chase);
            }
        }

        public void Exit()
        {
            _enemy.Hitbox.Deactivate();
            _enemy.Animator.SetBool(EnemyController.HashIsAttacking, false);
        }
    }
}
