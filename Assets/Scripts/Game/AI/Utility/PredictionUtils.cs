using UnityEngine;

namespace GMLM.Game
{
	public static class PredictionUtils
	{
		public static float ComputeClosestApproachTime(Vector2 posA, Vector2 velA, Vector2 posB, Vector2 velB)
		{
			Vector2 r0 = posB - posA;
			Vector2 vRel = velB - velA;
			float vRel2 = vRel.sqrMagnitude;
			if (vRel2 <= 1e-8f) return 0f;
			float tStar = -Vector2.Dot(r0, vRel) / vRel2;
			return tStar;
		}

		public static float ComputeDistanceAtTime(Vector2 posA, Vector2 velA, Vector2 posB, Vector2 velB, float t)
		{
			Vector2 a = posA + velA * t;
			Vector2 b = posB + velB * t;
			return (b - a).magnitude;
		}

		public static bool WillPathsCrossWithin(Vector2 posA, Vector2 velA, Vector2 posB, Vector2 velB, float horizon, float clearance, out float crossTime, out float dMin)
		{
			crossTime = 0f;
			dMin = float.PositiveInfinity;
			if (horizon <= 0f) return false;
			float tStar = ComputeClosestApproachTime(posA, velA, posB, velB);
			tStar = Mathf.Clamp(tStar, 0f, horizon);
			dMin = ComputeDistanceAtTime(posA, velA, posB, velB, tStar);
			bool cross = dMin <= Mathf.Max(0f, clearance);
			if (cross) crossTime = tStar;
			return cross;
		}

		public static bool TryFirstOrderIntercept(Vector2 shooterPos, float projectileSpeed, Vector2 targetPos, Vector2 targetVel, out Vector2 aimDirection, out float interceptTime)
		{
			aimDirection = Vector2.zero;
			interceptTime = 0f;
			projectileSpeed = Mathf.Max(1e-3f, projectileSpeed);
			Vector2 r = targetPos - shooterPos;
			float a = Vector2.Dot(targetVel, targetVel) - projectileSpeed * projectileSpeed;
			float b = 2f * Vector2.Dot(r, targetVel);
			float c = Vector2.Dot(r, r);
			if (Mathf.Abs(a) < 1e-6f)
			{
				// Linear solution when a ~ 0
				float t = -c / Mathf.Max(1e-6f, b);
				if (t <= 0f) return false;
				Vector2 interceptPoint = targetPos + targetVel * t;
				aimDirection = (interceptPoint - shooterPos).normalized;
				interceptTime = t;
				return true;
			}
			float disc = b * b - 4f * a * c;
			if (disc < 0f) return false;
			float sqrt = Mathf.Sqrt(disc);
			float t0 = (-b - sqrt) / (2f * a);
			float t1 = (-b + sqrt) / (2f * a);
			float tBest = (t0 > 0f && t1 > 0f) ? Mathf.Min(t0, t1) : Mathf.Max(t0, t1);
			if (tBest <= 0f) return false;
			Vector2 aimPoint = targetPos + targetVel * tBest;
			aimDirection = (aimPoint - shooterPos).normalized;
			interceptTime = tBest;
			return true;
		}
	}
}


