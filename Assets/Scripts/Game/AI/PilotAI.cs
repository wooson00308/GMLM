using UnityEngine;
using GMLM.AI;

namespace GMLM.Game
{
    [RequireComponent(typeof(Pilot))]
    public class PilotAI: BehaviorTreeRunner
    {
        protected override Node InitializeTree(IBlackboard blackboard)
        {
            return null;
        }
    }
}