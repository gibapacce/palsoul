using UnityEngine;

namespace Palsoul.Core
{
    /// <summary>
    /// Define uma espécie de criatura capturável ("Companion").
    /// GDD seção 5.4: cada espécie tem stats base, elemento, moveset, aptidões de trabalho.
    ///
    /// Crie assets em ScriptableObjects/Creatures via Assets > Create > Palsoul > Creature Definition.
    ///
    /// Uma instância deste SO representa a ESPÉCIE (não o indivíduo capturado).
    /// O estado de uma criatura capturada individual (nível, HP atual) será gerenciado
    /// pelo SaveSystem (MVP 11).
    /// </summary>
    [CreateAssetMenu(fileName = "Creature_New", menuName = "Palsoul/Creature Definition")]
    public class CreatureDefinitionSO : ScriptableObject
    {
        [Header("Identidade")]
        [Tooltip("Nome da espécie (ex.: 'Slime de Fogo').")]
        public string creatureName = "Criatura";

        [Tooltip("Descrição curta para o Bestiário (lore).")]
        [TextArea(2, 4)]
        public string loreDescription = "";

        [Tooltip("Sprite de retrato para a UI do Bestiário e HUD de squad.")]
        public Sprite portrait;
        public Sprite worldSprite;

        [Header("Elemento")]
        [Tooltip("Elemento desta espécie. Null = Neutro/Físico.")]
        public ElementSO element;

        [Header("Stats Base")]
        [Min(1f)] public float baseHP       = 80f;
        [Min(0f)] public float baseAttack   = 12f;
        [Min(0f)] public float baseDefense  = 5f;
        [Min(0f)] public float baseMoveSpeed = 3.5f;

        [Header("Captura")]
        [Tooltip("Raridade da espécie (afeta bônus de XP do Bestiário e dificuldade de captura).")]
        public CreatureRarity rarity = CreatureRarity.Common;

        [Tooltip("Limiar de HP normalizado abaixo do qual a captura se torna viável (ex.: 0.20 = 20% HP).")]
        [Range(0f, 1f)]
        public float captureHPThreshold = 0.20f;

        [Header("Moveset (Forma Ativa)")]
        [Tooltip("Ataque leve desta espécie quando controlada pelo jogador.")]
        public AttackDataSO lightAttack;

        [Tooltip("Ataque pesado desta espécie quando controlada pelo jogador.")]
        public AttackDataSO heavyAttack;

        [Tooltip("Habilidade especial (cooldown). Null = sem habilidade especial.")]
        public AttackDataSO specialAbility;

        [Header("Work Affinity")]
        [Tooltip("Aptidões de trabalho na base. Selecione múltiplas via checkbox.")]
        public WorkAffinity workAffinity = WorkAffinity.None;

        [Header("Loot / Drop")]
        [Tooltip("Quantidade de Éter dropado ao ser derrotado (sem captura).")]
        [Min(0f)]
        public float etherDrop = 8f;

        [Header("Visual / Animator")]
        [Tooltip("AnimatorController desta espécie — trocado no PlayerController ao usar como Forma Ativa.")]
        public RuntimeAnimatorController animatorController;

        [Tooltip("Escala do sprite em relação ao player (1 = mesmo tamanho).")]
        [Min(0.1f)]
        public float spriteScale = 1f;
    }

    // ── Enum de raridade ──────────────────────────────────────────────────────
    public enum CreatureRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }
}
