using UnityEngine;

namespace Palsoul.Combat
{
    /// <summary>
    /// Estado Patrol do inimigo.
    /// Gera waypoints aleatórios dentro do patrolRadius ao redor do spawnPosition.
    /// Move-se entre waypoints, esperando patrolWaitTime em cada um.
    /// Detecta o player e transiciona para Chase.
    /// </summary>
    public class EnemyPatrolState : IState
    {
        private readonly EnemyController _enemy;

        private Vector2 _currentWaypoint;
        private float   _waitTimer;
        private bool    _isWaiting;

        private const float WaypointReachedThreshold = 0.15f;

        public EnemyPatrolState(EnemyController enemy) => _enemy = enemy;

        public void Enter()
        {
            _isWaiting = false;
            _waitTimer = 0f;
            PickNewWaypoint();

            _enemy.Animator.SetBool(EnemyController.HashIsMoving,  true);
            _enemy.Animator.SetBool(EnemyController.HashIsChasing, false);
        }

        public void Tick()
        {
            // Prioridade 1: detecta player → Chase
            if (_enemy.PlayerTransform != null
             && _enemy.DistanceToPlayer() <= _enemy.Data.detectionRadius)
            {
                _enemy.IsAlerted = true;
                _enemy.TransitionTo(EnemyState.Chase);
                return;
            }

            if (_isWaiting)
            {
                _enemy.StopMovement();
                _waitTimer += Time.deltaTime;
                if (_waitTimer >= _enemy.Data.patrolWaitTime)
                {
                    _isWaiting = false;
                    PickNewWaypoint();
                    _enemy.Animator.SetBool(EnemyController.HashIsMoving, true);
                }
                return;
            }

            // Move em direção ao waypoint atual
            _enemy.MoveTowards(_currentWaypoint, _enemy.Data.moveSpeed);

            // Chegou ao waypoint?
            float dist = Vector2.Distance(_enemy.Rb.position, _currentWaypoint);
            if (dist <= WaypointReachedThreshold)
            {
                _isWaiting = true;
                _waitTimer = 0f;
                _enemy.Animator.SetBool(EnemyController.HashIsMoving, false);
            }
        }

        public void Exit()
        {
            _enemy.StopMovement();
        }

        // ── Geração de waypoint ────────────────────────────────────────────────

        private void PickNewWaypoint()
        {
            // Ponto aleatório dentro do patrolRadius ao redor do spawn
            Vector2 randomOffset = Random.insideUnitCircle * _enemy.Data.patrolRadius;
            _currentWaypoint     = (Vector2)_enemy.SpawnPosition + randomOffset;
        }
    }
}
