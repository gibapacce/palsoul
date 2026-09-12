using UnityEngine;

namespace Palsoul.Core
{
    /// <summary>
    /// ScriptableObject que representa um elemento (Fogo, Água, Terra, etc.).
    /// A matriz de vantagens/desvantagens será implementada na ElementMatrix (MVP 5).
    /// Por enquanto serve como referência tipada — evita usar strings/enums que precisariam
    /// ser alterados para cada novo elemento adicionado.
    ///
    /// Crie assets em ScriptableObjects/Elements via Assets > Create > Palsoul > Element.
    /// </summary>
    [CreateAssetMenu(fileName = "Element_Neutro", menuName = "Palsoul/Element")]
    public class ElementSO : ScriptableObject
    {
        [Header("Identificação")]
        public string elementName = "Neutro";

        [Tooltip("Cor associada ao elemento (usada em VFX e UI).")]
        public Color elementColor = Color.white;

        [Tooltip("Ícone do elemento para uso na UI (Palpedia, HUD de squad).")]
        public Sprite icon;
    }
}
