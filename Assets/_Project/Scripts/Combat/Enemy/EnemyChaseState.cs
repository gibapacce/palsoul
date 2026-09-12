using UnityEngine;

namespace Palsoul.Combat
{
    /// <summary>
    /// Estado Chase do inimigo.
    /// Persegue o player com chaseSpeed.
    /// Transiciona para Attack quando entra no attackRange.
    /// Perde o aggro e volta para Patrol se o player sair do loseAggroRadius.
    /// </summary>
    public class EnemyChaseState : IState
    {
        private readonly EnemyController _enemy;

        public EnemyChaseState(EnemyController enemy) => _enemy = enemy;

        public void Enter()
        {
            _enemy.Animator.SetBool(EnemyController.HashIsMoving,  true);
            _enemy.Animator.SetBool(EnemyController.HashIsChasing, true);
        }

        public void Tick()
        {
            if (_enemy.PlayerTransform == null) return;

            float dist = _enemy.DistanceToPlayer();

            // Perdeu o rastro do player
            if (dist > _enemy.Data.loseAggroRadius)
            {
                _enemy.IsAlerted = false;
                _enemy.TransitionTo(EnemyState.Patrol);
                return;
            }

            // Dentro do alcance de ataque e cooldown zerado → Attack
            if (dist <= _enemy.Data.attackRange && _enemy.AttackCooldownTimer <= 0f)
            {
                _enemy.TransitionTo(EnemyState.Attack);
                return;
            }

            // Continua perseguindo
            _enemy.MoveTowards(_enemy.PlayerTransform.position, _enemy.Data.chaseSpeed);
        }

        public void Exit()
        {
            _enemy.StopMovement();
            _enemy.Animator.SetBool(EnemyController.HashIsChasing, false);
        }
    }
}
