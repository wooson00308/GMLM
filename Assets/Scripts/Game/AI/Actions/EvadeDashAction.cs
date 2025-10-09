using Cysharp.Threading.Tasks;
using UnityEngine;
using GMLM.AI;

namespace GMLM.Game
{
    public class EvadeDashAction : ActionNode
    {
        private readonly string _selfKey;
        private readonly float _ttiThreshold;
        private readonly MaintainRangeStrafeAction _strafeAction;

        public EvadeDashAction(IBlackboard blackboard, string selfKey = "self", float ttiThreshold = 0.25f, MaintainRangeStrafeAction strafeAction = null)
            : base(blackboard)
        {
            _selfKey = selfKey;
            _ttiThreshold = Mathf.Max(0.05f, ttiThreshold);
            _strafeAction = strafeAction;
        }

        public override UniTask<NodeStatus> Execute()
        {
            var self = Blackboard.GetTransform(_selfKey);
            if (self == null) return new UniTask<NodeStatus>(NodeStatus.Failure);

            var mecha = self.GetComponent<Mecha>();
            var sensor = self.GetComponent<MechaProjectileSensor>();
            if (mecha == null || sensor == null) return new UniTask<NodeStatus>(NodeStatus.Failure);

            // 고위협 투사체만 대시 발동 (바주카/미사일 등)
            // 일반 투사체는 스트레이프로 자연 회피
            if (sensor.IncomingIsHighThreat && sensor.IncomingTTI < _ttiThreshold && mecha.CanDash())
            {
                // 투사체 진행방향에 수직으로 회피
                Vector2 d = sensor.IncomingDir;
                if (d.sqrMagnitude > 0f)
                {
                    Vector3 perp = new Vector3(-d.y, d.x, 0f);
                    // 안전한 쪽 선택(간단: 타겟 반대편을 선호) - 여기서는 랜덤으로 결정
                    if (Random.value > 0.5f) perp = -perp;
                    
                    bool dashSuccess = mecha.TryDash(perp);
                    
                    // 대시 성공 시 스트레이프 방향 업데이트
                    if (dashSuccess && _strafeAction != null)
                    {
                        _strafeAction.UpdateDirectionFromDash(perp);
                    }
                }
                return new UniTask<NodeStatus>(NodeStatus.Running);
            }

            return new UniTask<NodeStatus>(NodeStatus.Failure);
        }
    }
}


