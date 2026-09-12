using UnityEngine;

namespace Palsoul.Core
{
    /// <summary>
    /// ScriptableObject com todos os parâmetros de um tipo de inimigo.
    /// Crie assets em ScriptableObjects/Enemies via Assets > Create > Palsoul > Enemy Data.
    ///
    /// Reutilizado por EnemyController — nunca hardcode valores no MonoBehaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Palsoul/Enemy Data")]
    public class EnemyDataSO : ScriptableObject
    {
        [Header("Identificação")]
        public string enemyName = "Inimigo";

        [Header("Stats")]
        [Min(1f)] public float maxHP        = 50f;
        [Min(0f)] public float moveSpeed    = 2.5f;
        [Min(0f)] public float chaseSpeed   = 3.5f;  // velocidade ao perseguir player

        [Header("Raios de IA (unidades Unity)")]
        [Tooltip("Raio em que o inimigo detecta o player e inicia perseguição.")]
        [Min(0f)] public float detectionRadius  = 6f;

        [Tooltip("Raio em que o inimigo perde o rastro do player e volta a patrulhar.")]
        [Min(0f)] public float loseAggroRadius  = 10f;

        [Tooltip("Distância mínima do player para executar o ataque.")]
        [Min(0f)] public float attackRange      = 1.2f;

        [Header("Ataque")]
        [Tooltip("Dados do ataque do inimigo (dano, hitbox, timing).")]
        public AttackDataSO attackData;

        [Tooltip("Cooldown entre ataques consecutivos (segundos).")]
        [Min(0f)] public float attackCooldown   = 1.5f;

        [Header("Patrol")]
        [Tooltip("Distância máxima dos waypoints aleatórios de patrol em relação ao ponto de spawn.")]
        [Min(0f)] public float patrolRadius     = 4f;

        [Tooltip("Tempo de espera em cada waypoint de patrol (segundos).")]
        [Min(0f)] public float patrolWaitTime   = 1.5f;

        [Header("Stagger")]
        [Tooltip("Duração do estado de stagger ao receber hit que supera o poise (segundos).")]
        [Min(0f)] public float staggerDuration  = 0.4f;

        [Header("Death")]
        [Tooltip("Tempo em segundos antes de destruir o GameObject após a morte.")]
        [Min(0f)] public float deathDestroyDelay = 1.5f;

        [Header("Loot")]
        [Tooltip("Quantidade de Éter dropado ao morrer.")]
        [Min(0f)] public float etherDrop        = 10f;
    }
}
