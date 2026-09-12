using UnityEngine;
using Palsoul.Progression;

namespace Palsoul.Core
{
    /// <summary>
    /// ScriptableObject que define um Ancoradouro específico.
    /// Cada instância representa um Ancoradouro da região (pode haver múltiplos por mapa).
    ///
    /// Crie assets em ScriptableObjects/Buildings via Assets > Create > Palsoul > Anchorpoint Data.
    /// </summary>
    [CreateAssetMenu(fileName = "Anchorpoint_Region01", menuName = "Palsoul/Anchorpoint Data")]
    public class AnchorpointDataSO : ScriptableObject
    {
        [Header("Identificação")]
        public string anchorpointName = "Ancoradouro";

        [Tooltip("ID único desta região (usado pelo SaveSystem para fast travel).")]
        public string regionID = "region_01";

        [Header("Atributos Upgradeáveis")]
        [Tooltip("Lista de atributos que podem ser comprados neste Ancoradouro.")]
        public AttributeUpgradeSO[] availableUpgrades;

        [Header("Raio de Interação")]
        [Tooltip("Raio em que o player pode interagir com este Ancoradouro.")]
        [Min(0f)]
        public float interactionRadius = 2f;
    }
}
