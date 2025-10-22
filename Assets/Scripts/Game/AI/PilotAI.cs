using UnityEngine;
using GMLM.AI;
using System.Collections.Generic;

namespace GMLM.Game
{
    [RequireComponent(typeof(Pilot))]
    public class PilotAI: BehaviorTreeRunner
    {
        protected override Node InitializeTree(IBlackboard blackboard)
        {
            // Blackboard priming
            blackboard.SetTransform("self", this.transform);

            // Actions
            var updateTarget = new UpdateTargetAction(blackboard, "self", "target", "teamId", 2.0f);
            var strafe = new MaintainRangeStrafeAction(blackboard, "self", "target", 0.75f, 0f, 0.4f, 2f);
            //var evadeToggle = new EvadeToggleAction(blackboard, "self", strafe, 0.35f);
            var evadeDash = new EvadeDashAction(blackboard, "self", 0.35f, strafe); // strafe 참조 전달
            var attack = new AttackAction(blackboard, "self", "target", 0f); // 무기 사거리 사용
            var idle = new IdleAction(blackboard);

            var parallel = BehaviorTreeBuilder
                .Parallel(Parallel.Policy.RequireOne)
                .Add(evadeDash)
                .Add(strafe)
                .Add(attack)
                .Build();

            var root = BehaviorTreeBuilder
                .Sequence()
                .Add(updateTarget)
                .Add(parallel)
                .Build();

            return root;
        }
    }
}