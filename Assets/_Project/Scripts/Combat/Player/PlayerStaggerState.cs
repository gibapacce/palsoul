using UnityEngine;
namespace Palsoul.Combat
{
    public sealed class PlayerStaggerState : IState
    {
        private readonly PlayerController player;
        private readonly float duration;
        private float elapsed;
        public PlayerStaggerState(PlayerController controller, float seconds) { player = controller; duration = seconds; }
        public void Enter() { elapsed = 0; player.Animator.SetBool("IsStaggered", true); }
        public void Tick()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration) player.TransitionTo(PlayerState.Idle);
        }
        public void Exit() => player.Animator.SetBool("IsStaggered", false);
    }
}
