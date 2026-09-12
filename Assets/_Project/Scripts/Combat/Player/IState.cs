namespace Palsoul.Combat
{
    /// <summary>
    /// Interface base para todos os estados da State Machine do Player (e futuramente inimigos).
    /// Cada estado implementa Enter (ao entrar), Tick (a cada Update) e Exit (ao sair).
    /// </summary>
    public interface IState
    {
        void Enter();
        void Tick();
        void Exit();
    }
}
