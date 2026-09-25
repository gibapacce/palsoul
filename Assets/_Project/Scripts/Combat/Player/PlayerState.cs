namespace Palsoul.Combat
{
    /// <summary>
    /// Enum com todos os estados possíveis do Player.
    /// Inclui o estado Dead e retorno ao Idle pelo respawn do MVP 8.
    /// </summary>
    public enum PlayerState
    {
        Idle,
        Moving,
        Dodging,    // MVP Item 2
        AttackLight, // MVP Item 3
        AttackHeavy, // MVP Item 3
        Stagger,     // MVP Item 3
        Dead         // MVP Item 8
    }
}
