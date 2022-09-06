using UnityEngine;

namespace Planet_IO
{
    public class EnemyStateManager : MonoBehaviour
    {
        public IState _currentState;

        private void Update() => RunStateMachine();

        private void RunStateMachine()
        {
            IState nextState = _currentState?.RunCurrentState();

            if (nextState != null) SwitchToTheNextState(nextState);
        }

        private void SwitchToTheNextState(IState nextState) => _currentState = nextState;
    }
}
