using System.Collections;
using UnityEngine;

namespace Palsoul.Combat
{
    /// <summary>
    /// Estado Death do inimigo.
    /// Para todo movimento, cancela hitbox, dispara animação de morte,
    /// aguarda deathDestroyDelay e destrói o GameObject.
    ///
    /// Drop de Éter: dispara o evento OnEnemyDied com o valor de etherDrop
    /// para que o EtherWallet do player (MVP 8) possa coletar.
    /// Por enquanto registra no log.
    ///
    /// Reset de inimigos ao descansar no Ancoradouro (GDD seção 5.6):
    /// o inimigo é destruído ao morrer; o sistema de reset (MVP 7) instancia
    /// os prefabs novamente ao descansar.
    /// </summary>
    public class EnemyDeathState : IState
    {
        private readonly EnemyController _enemy;

        public EnemyDeathState(EnemyController enemy) => _enemy = enemy;

        public void Enter()
        {
            // Para tudo
            _enemy.Hitbox.Deactivate();
            _enemy.StopMovement();
            _enemy.Rb.simulated = false;   // remove da simulação física

            // Desliga todos os colliders para não bloquear o player
            foreach (var col in _enemy.GetComponentsInChildren<Collider2D>())
                col.enabled = false;

            // Animator
            _enemy.Animator.SetBool(EnemyController.HashIsMoving,   false);
            _enemy.Animator.SetBool(EnemyController.HashIsChasing,  false);
            _enemy.Animator.SetBool(EnemyController.HashIsAttacking,false);
            _enemy.Animator.SetBool(EnemyController.HashIsStaggered,false);
            _enemy.Animator.SetBool(EnemyController.HashIsDead,     true);

            // Drop de Éter (placeholder até MVP 8)
#if UNITY_EDITOR
            Debug.Log($"[EnemyDeath] {_enemy.Data.enemyName} morreu. " +
                      $"Drop: {_enemy.Data.etherDrop} Éter.");
#endif
            // Inicia destruição com delay para a animação de morte tocar
            _enemy.StartCoroutine(DestroyAfterDelay());
        }

        public void Tick()  { /* aguarda a coroutine */ }
        public void Exit()  { }

        private IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(_enemy.Data.deathDestroyDelay);

            // Fade out simples (SpriteRenderer alpha)
            var sr = _enemy.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float elapsed = 0f;
                float fadeDuration = 0.4f;
                Color original = sr.color;

                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                    sr.color = new Color(original.r, original.g, original.b, alpha);
                    yield return null;
                }
            }

            Object.Destroy(_enemy.gameObject);
        }
    }
}
