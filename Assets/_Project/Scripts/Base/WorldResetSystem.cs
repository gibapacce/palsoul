using System.Collections.Generic;
using UnityEngine;

namespace Palsoul.Base
{
    /// <summary>
    /// Rastreia inimigos não-chefe da região e os respawna ao jogador descansar.
    /// GDD seção 5.5: "descansar respawna inimigos não-chefe do mundo."
    ///
    /// Funcionamento:
    ///   - Registre cada inimigo/criatura via RegisterSpawn() ao instanciar.
    ///   - Ao descansar, ResetWorld() destrói os GOs ativos e reinstancia todos do prefab.
    ///   - Chefes NÃO são registrados aqui (são permanentemente derrotados).
    ///
    /// Setup: adicione este componente a um GameObject de gerenciamento na cena (ex.: "WorldManager").
    /// O AnchorpointController referencia e chama ResetWorld() ao descansar.
    /// </summary>
    public class WorldResetSystem : MonoBehaviour
    {
        // ── Entrada de spawn ───────────────────────────────────────────────────
        [System.Serializable]
        public class SpawnEntry
        {
            public GameObject prefab;
            public Vector3    position;
            public Quaternion rotation;
            [HideInInspector] public GameObject instance; // instância atual (pode ser null se morreu)
        }

        // ── Lista de spawns da região ──────────────────────────────────────────
        [Header("Spawns da Região")]
        [Tooltip("Preencha no Editor com os prefabs e posições dos inimigos/criaturas da região.")]
        [SerializeField] private List<SpawnEntry> _spawnEntries = new List<SpawnEntry>();

        // ── Evento ─────────────────────────────────────────────────────────────
        /// <summary>Disparado após o reset completo do mundo. Parâmetro: número de entidades respawnadas.</summary>
        public event System.Action<int> OnWorldReset;

        // ─────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Start()
        {
            // Instancia todos os spawns ao carregar a cena
            SpawnAll();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region API Pública

        /// <summary>
        /// Registra um spawn dinamicamente em runtime (ex.: ao carregar de save).
        /// </summary>
        public void RegisterSpawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            _spawnEntries.Add(new SpawnEntry
            {
                prefab   = prefab,
                position = position,
                rotation = rotation
            });
        }

        /// <summary>
        /// Reseta o mundo: destrói todas as instâncias ativas e reinstancia tudo.
        /// Chamado pelo AnchorpointController ao descansar.
        /// </summary>
        public void ResetWorld()
        {
            // Destrói instâncias existentes
            foreach (var entry in _spawnEntries)
            {
                if (entry.instance != null)
                {
                    entry.instance.SetActive(false);
                    Destroy(entry.instance);
                }
                entry.instance = null;
            }

            int count = SpawnAll();
            OnWorldReset?.Invoke(count);

#if UNITY_EDITOR
            Debug.Log($"[WorldResetSystem] Mundo resetado. {count} entidade(s) respawnada(s).");
#endif
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Spawn

        private int SpawnAll()
        {
            int count = 0;
            foreach (var entry in _spawnEntries)
            {
                if (entry.prefab == null) continue;
                entry.instance = Instantiate(entry.prefab, entry.position, entry.rotation);
                count++;
            }
            return count;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Gizmos

        private void OnDrawGizmos()
        {
            foreach (var entry in _spawnEntries)
            {
                if (entry.prefab == null) continue;
                bool alive = entry.instance != null && entry.instance.activeSelf;
                Gizmos.color = alive ? new Color(1f, 0.5f, 0f, 0.5f) : new Color(0.4f, 0.4f, 0.4f, 0.3f);
                Gizmos.DrawSphere(entry.position, 0.3f);
            }
        }

        #endregion
    }
}
