using UnityEngine;

namespace BossFSM
{
    public abstract class BossState
    {
        protected BossStateMachine stateMachine;
        protected Boss boss;

        public BossState(BossStateMachine stateMachine, Boss boss)
        {
            this.stateMachine = stateMachine;
            this.boss = boss;
        }

        public virtual void EnterState() { }
        public virtual void UpdateState() { }
        public virtual void ExitState() { }
    }
} 