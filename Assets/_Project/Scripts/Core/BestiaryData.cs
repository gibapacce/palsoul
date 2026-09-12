using System.Collections.Generic;
using UnityEngine;

namespace Palsoul.Core
{
    /// <summary>
    /// ScriptableObject que armazena o progresso do Bestiário do jogador.
    /// Guarda a lista de espécies capturadas e o total de capturas por espécie.
    ///
    /// Crie UM asset em ScriptableObjects/Player via Assets > Create > Palsoul > Bestiary Data.
    /// Este asset é a "fonte da verdade" em runtime; o SaveSystem (MVP 11) serializa
    /// seu conteúdo para JSON ao salvar.
    ///
    /// Eventos disparados:
    ///   OnNewSpeciesCaptured(CreatureDefinitionSO) — primeira captura de uma espécie
    ///   OnCaptureCounted(CreatureDefinitionSO, int) — qualquer captura (contagem atualizada)
    /// </summary>
    [CreateAssetMenu(fileName = "BestiaryData", menuName = "Palsoul/Bestiary Data")]
    public class BestiaryData : ScriptableObject
    {
        // ── Estado interno ─────────────────────────────────────────────────────
        // Serializado para poder inspecionar no Editor durante o jogo
        [SerializeField] private List<BestiaryEntry> _entries = new List<BestiaryEntry>();

        // ── Eventos ────────────────────────────────────────────────────────────
        public event System.Action<CreatureDefinitionSO>       OnNewSpeciesCaptured;
        public event System.Action<CreatureDefinitionSO, int>  OnCaptureCounted;

        // ── Propriedades ───────────────────────────────────────────────────────
        public IReadOnlyList<BestiaryEntry> Entries => _entries;
        public int TotalSpeciesDiscovered => _entries.Count;

        // ─────────────────────────────────────────────────────────────────────
        #region API Pública

        /// <summary>
        /// Registra uma captura de <paramref name="creature"/>.
        /// Se for a primeira vez, dispara OnNewSpeciesCaptured.
        /// Sempre dispara OnCaptureCounted com a contagem atualizada.
        /// </summary>
        public void RegisterCapture(CreatureDefinitionSO creature)
        {
            if (creature == null) return;

            var entry = _entries.Find(e => e.definition == creature);
            bool isNew = entry == null;

            if (isNew)
            {
                entry = new BestiaryEntry { definition = creature, captureCount = 0 };
                _entries.Add(entry);
            }

            entry.captureCount++;

            if (isNew)
                OnNewSpeciesCaptured?.Invoke(creature);

            OnCaptureCounted?.Invoke(creature, entry.captureCount);

#if UNITY_EDITOR
            Debug.Log($"[Bestiary] {creature.creatureName} registrado " +
                      $"(total capturas: {entry.captureCount}, nova espécie: {isNew})");
#endif
        }

        /// <summary>Retorna true se a espécie já foi capturada ao menos uma vez.</summary>
        public bool HasCaptured(CreatureDefinitionSO creature)
            => _entries.Exists(e => e.definition == creature);

        /// <summary>Retorna o número de capturas de uma espécie específica.</summary>
        public int GetCaptureCount(CreatureDefinitionSO creature)
        {
            var entry = _entries.Find(e => e.definition == creature);
            return entry?.captureCount ?? 0;
        }

        /// <summary>Limpa todos os dados (usado ao iniciar um novo jogo).</summary>
        public void Clear() => _entries.Clear();

        #endregion
    }

    // ── Entry serializada ──────────────────────────────────────────────────────
    [System.Serializable]
    public class BestiaryEntry
    {
        public CreatureDefinitionSO definition;
        public int                  captureCount;
    }
}
