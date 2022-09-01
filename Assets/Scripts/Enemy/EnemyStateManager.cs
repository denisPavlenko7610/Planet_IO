using UnityEngine;

namespace Planet_IO
{
    public class EnemyStateManager : MonoBehaviour
    {
        public EnemyState _currentState;

        private void Update() => RunStateMachine();

        private void RunStateMachine()
        {
            EnemyState nextState = _currentState?.RunCurrentState();

            if (nextState != null) SwitchToTheNextState(nextState);
        }

        private void SwitchToTheNextState(EnemyState nextState) => _currentState = nextState;
    }
}
