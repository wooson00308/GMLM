using UnityEngine;

namespace GMLM.AI
{
    public abstract class Consideration
    {
        protected readonly IBlackboard Blackboard;
        protected readonly AnimationCurve ResponseCurve;

        protected Consideration(IBlackboard blackboard, AnimationCurve responseCurve)
        {
            Blackboard = blackboard;
            ResponseCurve = responseCurve;
        }

        public float GetScore()
        {
            var rawValue = GetRawValue();
            return ResponseCurve.Evaluate(rawValue);
        }

        protected abstract float GetRawValue();
    }
} 