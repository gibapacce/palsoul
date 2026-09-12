namespace Palsoul.Combat
{
    /// <summary>
    /// Enum com todos os estados possíveis do Player.
    /// Novos estados (Dodge, Attack, Dead, etc.) serão adicionados nos MVPs seguintes.
    /// </summary>
    public enum PlayerState
    {
        Idle,
        Moving,
        Dodging,    // MVP Item 2
        AttackLight, // MVP Item 3
        AttackHeavy, // MVP Item 3
        Stagger,     // MVP Item 3
        Dead         // MVP Item 7
    }
}
