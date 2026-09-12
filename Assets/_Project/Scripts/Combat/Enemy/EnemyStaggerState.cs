using UnityEngine;

namespace Palsoul.Combat
{
    /// <summary>
    /// Estado Stagger do inimigo.
    /// Aplicado quando o inimigo recebe um hit que supera seu poise (HurtboxController.OnStagger).
    /// Imobiliza o inimigo por staggerDuration, cancela o ataque em andamento,
    /// e retorna para Chase ao terminar.
    /// </summary>
    public class EnemyStaggerState : IState
    {
        private readonly EnemyController _enemy;
        private float   _staggerTimer;
        private Vector2 _knockbackDir;

        public EnemyStaggerState(EnemyController enemy) => _enemy = enemy;

        /// <summary>Chamado pelo EnemyController antes de TransitionTo(Stagger).</summary>
        public void SetKnockbackDirection(Vector2 dir) => _knockbackDir = dir;

        public void Enter()
        {
            _staggerTimer = 0f;

            // Cancela hitbox ativo caso estivesse atacando
            _enemy.Hitbox.Deactivate();
            _enemy.StopMovement();

            // Aplica impulso de knockback (o HurtboxController já fez isso,
            // mas garantimos que o movimento fica zerado após o impulso)
            // O Rigidbody já recebeu o impulso via HurtboxController.ApplyKnockback

            _enemy.Animator.SetBool(EnemyController.HashIsMoving,   false);
            _enemy.Animator.SetBool(EnemyController.HashIsAttacking, false);
            _enemy.Animator.SetBool(EnemyController.HashIsStaggered, true);

#if UNITY_EDITOR
            Debug.Log($"[EnemyStagger] {_enemy.Data.enemyName} entrou em stagger.");
#endif
        }

        public void Tick()
        {
            _staggerTimer += Time.deltaTime;

            if (_staggerTimer >= _enemy.Data.staggerDuration)
            {
                // Após stagger, retorna para Chase se alertado, senão Idle
                _enemy.TransitionTo(_enemy.IsAlerted ? EnemyState.Chase : EnemyState.Idle);
            }
        }

        public void Exit()
        {
            _enemy.Animator.SetBool(EnemyController.HashIsStaggered, false);
        }
    }
}
