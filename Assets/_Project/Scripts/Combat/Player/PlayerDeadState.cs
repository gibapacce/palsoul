namespace Palsoul.Combat
{
    public sealed class PlayerDeadState : IState
    {
        private readonly PlayerController player;
        public PlayerDeadState(PlayerController player) => this.player = player;
        public void Enter()
        {
            player.IsInvincible = false;
            player.Animator.SetBool("IsMoving", false);
            player.Animator.SetTrigger("Death");
        }
        public void Tick() { }
        public void Exit() => player.Animator.ResetTrigger("Death");
    }
}
