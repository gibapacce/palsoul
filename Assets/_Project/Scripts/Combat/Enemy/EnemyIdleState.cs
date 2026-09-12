using UnityEngine;

namespace Palsoul.Combat
{
    /// <summary>
    /// Estado Idle do inimigo.
    /// O inimigo fica parado no ponto de spawn, olhando para baixo.
    /// Verifica a cada frame se o player entrou no raio de detecção.
    /// Após um tempo configurável, transiciona para Patrol.
    /// </summary>
    public class EnemyIdleState : IState
    {
        private readonly EnemyController _enemy;
        private float _idleTimer;
        private const float IdleToPatrolDelay = 2f;  // segundos em idle antes de começar a patrulhar

        public EnemyIdleState(EnemyController enemy) => _enemy = enemy;

        public void Enter()
        {
            _idleTimer = 0f;
            _enemy.StopMovement();
            _enemy.Animator.SetBool(EnemyController.HashIsMoving,  false);
            _enemy.Animator.SetBool(EnemyController.HashIsChasing, false);
        }

        public void Tick()
        {
            // Prioridade 1: detecta player → Chase imediato
            if (_enemy.PlayerTransform != null
             && _enemy.DistanceToPlayer() <= _enemy.Data.detectionRadius)
            {
                _enemy.IsAlerted = true;
                _enemy.TransitionTo(EnemyState.Chase);
                return;
            }

            // Prioridade 2: após delay, começa a patrulhar
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= IdleToPatrolDelay)
                _enemy.TransitionTo(EnemyState.Patrol);
        }

        public void Exit() { }
    }
}
