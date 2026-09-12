using UnityEngine;

namespace Palsoul.Progression
{
    /// <summary>
    /// Define um atributo upgradeável no Ancoradouro (ex.: Vigor, Vigor de Ação, Força).
    /// GDD seção 6: atributos manuais estilo Souls, comprados com Éter.
    ///
    /// Crie assets em ScriptableObjects/Progression via Assets > Create > Palsoul > Attribute Upgrade.
    /// </summary>
    [CreateAssetMenu(fileName = "Attr_Vigor", menuName = "Palsoul/Attribute Upgrade")]
    public class AttributeUpgradeSO : ScriptableObject
    {
        [Header("Identificação")]
        public string attributeName = "Vigor";

        [Tooltip("Descrição curta exibida na UI do Ancoradouro.")]
        public string description = "Aumenta o HP máximo.";

        [Header("Progressão")]
        [Tooltip("Nível máximo deste atributo.")]
        [Min(1)]
        public int maxLevel = 10;

        [Tooltip("Custo em Éter por nível. Índice 0 = custo do nível 1, índice 1 = custo do nível 2, etc. " +
                 "Se a curva tiver menos pontos que maxLevel, o último valor é reutilizado.")]
        public AnimationCurve etherCostCurve = AnimationCurve.Linear(0, 50, 9, 500);

        [Header("Efeito")]
        [Tooltip("Tipo de atributo que este upgrade afeta.")]
        public AttributeType attributeType = AttributeType.Vigor;

        [Tooltip("Quantidade adicionada ao stat base por nível.")]
        [Min(0f)]
        public float valuePerLevel = 10f;

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Custo em Éter para ir do nível <paramref name="currentLevel"/> para o próximo.</summary>
        public float GetCostForLevel(int currentLevel)
        {
            if (currentLevel >= maxLevel) return float.MaxValue;   // já no máximo
            // AnimationCurve: X = índice de nível (0-based), Y = custo
            return etherCostCurve.Evaluate(currentLevel);
        }

        /// <summary>Valor total acumulado ao nível <paramref name="level"/>.</summary>
        public float GetTotalValueAtLevel(int level)
            => valuePerLevel * Mathf.Clamp(level, 0, maxLevel);
    }

    public enum AttributeType
    {
        Vigor,          // HP máximo
        ActionVigor,    // Stamina máxima
        Strength,       // Dano físico
        Dexterity,      // Velocidade de ataque / custo de stamina reduzido
        ElementalAffinity // Dano / resistência elemental
    }
}
