using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct PathPoint 
{
	public Vector3 position;
	public Vector3 direction;
	public float speed;
}

public interface ParametrizedVelocity
{
	Vector3 GetVelocityAtTime { get; }
}

public class EntitySpline
{
	private List<PathPoint> pathPoints;

	private float correctionCoefficient;

	private float dampingCoefficientOffSpline = 1.0f;

	private int m_currentPathPointIndex;

	private float m_distanceUntilMoveToNextPathPoint;

	private void FollowSpline(Rigidbody body) 
	{
		Vector3 currentVelocity = body.velocity;
		PathPoint targetPathPoint = pathPoints[m_currentPathPointIndex];

		Vector3 displacementToTarget = targetPathPoint.position - body.position;

		float currentSpeedParallel = Vector3.Dot(currentVelocity, targetPathPoint.direction);

		Vector3 currentVelParallel = targetPathPoint.direction * currentSpeedParallel;
		Vector3 currentVelPerpendicular = (currentVelocity - currentVelParallel).normalized;

		float projectedTimeTilNextPathPoint = 2 * displacementToTarget.magnitude / (currentSpeedParallel + targetPathPoint.speed);

		currentVelocity += (targetPathPoint.direction * targetPathPoint.speed-currentVelParallel) * Time.deltaTime / projectedTimeTilNextPathPoint;

		float perpAccelScale = Vector3.Dot(currentVelPerpendicular.normalized, displacementToTarget);

		currentVelocity += -currentVelPerpendicular * perpAccelScale * Time.deltaTime;
		
		currentVelocity.Normalize();

	}
	
}
