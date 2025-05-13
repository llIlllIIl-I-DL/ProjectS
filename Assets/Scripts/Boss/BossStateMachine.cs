using UnityEngine;

namespace BossFSM
{
    public class BossStateMachine : MonoBehaviour
    {
        protected BossState currentState{get; private set;} 
        protected BossState previousState{get; private set;} 

        public BossState CurrentState => currentState;
        public BossState PreviousState => previousState;

        protected virtual void Update()
        {
            currentState?.UpdateState();
        }

        public void ChangeState(BossState newState)
        {
            if (currentState != null)
            {
                currentState.ExitState();
                previousState = currentState;
            }

            currentState = newState;
            currentState.EnterState();
        }
    }
} 