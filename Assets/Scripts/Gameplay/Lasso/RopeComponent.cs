using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeComponent : MonoBehaviour
{
    [Range(0.0f, 2.0f)]		[SerializeField] private float m_fGravityMultiplier = 1.0f;
    [Range(1.0f, 200.0f)]	[SerializeField] private float m_fLength = 10.0f;
    [Range(1, 100)]			[SerializeField] private uint  m_ropeIterations = 10;
	[Range(0.0f, 10.0f)]	[SerializeField] private float m_RopeRadius = 1.0f;
    [Range(0.0f, 1.0f)]		[SerializeField] private float m_fDistBetweenSegments = 0.5f;
	[Range(0.1f, 1.0f)]		[SerializeField] private float m_ColliderUpdateFrequency = 0.5f;

    [SerializeField] private Transform m_RopeTransform = default;
    [SerializeField] private LineRenderer m_RopeLineRenderer = default;

    [SerializeField] private readonly List<RopeSegmentComponent> m_RopeSegments = new List<RopeSegmentComponent>();
    [SerializeField] private float m_fDistBetweenFirstSegments;
	[SerializeField] private float m_fDistBetweenLastSegments;
	[SerializeField] private float m_fCurrentColliderUpdateVal;
	[SerializeField] private LayerMask m_RopeCollisionMask;

	[SerializeField] private Transform m_RopeAttachmentPoint;

	private void Awake()
	{
		m_RopeTransform = transform;
		m_RopeLineRenderer = GetComponent<LineRenderer>();
	}

	private void CreateRopeSegment() 
    {
		RopeSegmentComponent ropeObject = new RopeSegmentComponent(m_RopeTransform.position);

		// when we create a rope segment, add it to where our last segment is
        if (m_RopeSegments.Count >= 1) 
        {
			ropeObject.SetNewPosition(m_RopeSegments[m_RopeSegments.Count - 1].CurrentPosition);
        }

		m_RopeSegments.Add(ropeObject);
	}

    private void RemoveRopeSegment() 
    {
		m_RopeSegments.RemoveAt(m_RopeSegments.Count-1);	
    }

	private void OnValidate()
	{
		if (gameObject)
		{
			GenerateRopeSegments();
		}
	}

	private void GenerateRopeSegments() 
    {
		// add 1 at the end for end node - consider distBetweenSegments > length
		// or when length < 2 * distBetweenSegments
		// essentially, we need to include the end segment
        int numSegments = (int)Mathf.Ceil(m_fLength / m_fDistBetweenSegments) + 1;

        // fill in rope segments if we don't have enough
        for (int i = m_RopeSegments.Count; i < numSegments; i++) 
        {
            CreateRopeSegment();
        }
        // remove rope segments if we have too many
        for (int i = m_RopeSegments.Count - 1; i >= numSegments; i--) 
        {
            RemoveRopeSegment();
        }

        m_fDistBetweenFirstSegments = m_fLength % m_fDistBetweenSegments;
    }

	//   void FinishRelaxation() 
	//   {
	//	RopeSegmentComponent startSegment = m_RopeSegments[0];
	//	RopeSegmentComponent endSegment = m_RopeSegments[m_RopeSegments.Count - 1];

	//	if (startSegment.IsEndPoint && endSegment.IsEndPoint)
	//	{

	//	}

	//	float totalLength = 0.0f;

	//	for (int i = 0; i < m_RopeSegments.Count - 1; i++)
	//	{
	//		totalLength += (m_RopeSegments[i].GetCurrentPosition() - m_RopeSegments[i + 1].GetCurrentPosition()).magnitude;
	//	}

	//	float offsetLength = totalLength - m_fLength;

	//	if (offsetLength > 0)
	//	{

	//		//float startSegmentOffsetMult = startSegment.GetMass() / (startSegment.GetMass() + endSegment.GetMass());
	//		//float endSegmentOffsetMult = 1 - startSegmentOffsetMult;

	//		Vector3 startSegmentOffset = m_RopeSegments[1].GetCurrentPosition() - startSegment.GetCurrentPosition();
	//		Vector3 endSegmentOffset = m_RopeSegments[m_RopeSegments.Count - 2].GetCurrentPosition() - endSegment.GetCurrentPosition();

	//		// set positions and change velocity of start/end component to account for 
	//	}
	//}


	void ApplyVerletIntegration()
	{
		Vector3 gravityDisplacement = Physics.gravity * m_fGravityMultiplier;

		RopeSegmentComponent currentSegment;
		Vector3 newPos;
		// Loop rope nodes and check if currently colliding
		for (int i = 0; i < m_RopeSegments.Count - 1; i++)
		{
			currentSegment = m_RopeSegments[i];

			// run dynamics-based verlet integration
			Vector3 newDist = currentSegment.CurrentPosition - currentSegment.LastPosition + gravityDisplacement * Time.fixedDeltaTime * Time.fixedDeltaTime;

			newPos = currentSegment.CurrentPosition + newDist;

			// but make sure we're not colliding with anything (bad)
			if (Physics.SphereCastNonAlloc(currentSegment.CurrentPosition, m_RopeRadius, newDist, m_ColliderHitBuffer, newDist.magnitude, m_RopeCollisionMask, QueryTriggerInteraction.Ignore) > 0)
			{
				newPos = m_ColliderHitBuffer[0].point + m_ColliderHitBuffer[0].normal * m_RopeRadius;
			}

			currentSegment.SetNewPosition(newPos);
		}
	}

	void Jakobsen()
	{
		if (m_RopeSegments.Count > 1)
		{
			for (uint i = 0; i < m_ropeIterations; i++)
			{
				RelaxConstraint(m_RopeSegments[0], m_RopeSegments[1], m_fDistBetweenFirstSegments);
				// since we're using j+1, only go up to m_RopeSegments.Count - 1
				for (int j = 1; j < m_RopeSegments.Count - 1; j++)
				{
					RelaxConstraint(m_RopeSegments[j], m_RopeSegments[j + 1], m_fDistBetweenSegments);
				}
			}
		}

		m_fCurrentColliderUpdateVal++;
		if (m_fCurrentColliderUpdateVal >= 1.0f)
		{
			AdjustCollisions();
			m_fCurrentColliderUpdateVal--;
		}

		if (m_RopeAttachmentPoint != null)
			m_RopeSegments[m_RopeSegments.Count - 1].AnchorNewPosition(m_RopeAttachmentPoint.position);
	}
	RaycastHit[] m_ColliderHitBuffer = new RaycastHit[1];

	private void AdjustCollisions()
	{
		RopeSegmentComponent currentSegment;
		RopeSegmentComponent nextSegment;
		Vector3 offset;
		// Loop rope nodes and check if currently colliding
		for (int i = 0; i < m_RopeSegments.Count - 1; i++)
		{
			currentSegment = m_RopeSegments[i];
			nextSegment = m_RopeSegments[i];
			offset = nextSegment.CurrentPosition - currentSegment.CurrentPosition;

			if (Physics.SphereCastNonAlloc(currentSegment.CurrentPosition, m_RopeRadius, offset, m_ColliderHitBuffer, offset.magnitude, m_RopeCollisionMask, QueryTriggerInteraction.Ignore) > 0)
				currentSegment.SetNewPosition(m_ColliderHitBuffer[0].point + m_ColliderHitBuffer[0].normal * m_RopeRadius);
		}
	}

	void RelaxConstraint(in RopeSegmentComponent segmentA, in RopeSegmentComponent segmentB, float desiredDistance)
	{
		//offset is from B to A: so apply this positively to B, negatively to A
		// consider B > A in position X: offset -ve
		Vector3 offset = segmentA.CurrentPosition - segmentB.CurrentPosition;
		float distance = offset.magnitude;

		// in case the distance between the two is too small to be valid, we'll define a small distance at a random direction instead
		if (distance < 0.00000001f)
		{
			distance = float.Epsilon;
			offset = Random.insideUnitSphere * distance;
		}

		// consider B > A by enough such that distance - desiredDistance > 0
		// such that offsetToAdd is also negative
		Vector3 offsetToAdd = (offset / distance) * ((distance - desiredDistance))/2;
		// hence we want to add offsetToAdd positively to B
		// and negatively to A
		segmentB.AddToPosition(offsetToAdd);
		segmentA.AddToPosition(-offsetToAdd);

		// if I want to not be touching any object, apply that constraint here, too

	}

	private void OnDrawGizmos()
	{
		for (int i = 0; i < m_RopeSegments.Count-1; i++)
		{
			Gizmos.DrawLine(m_RopeSegments[i].CurrentPosition, m_RopeSegments[i + 1].CurrentPosition);
		}
	}

	void RenderRope() 
    {
        //m_RopeLineRenderer.positionCount = m_RopeSegments.Count;
        //for (int i = 0; i < m_RopeSegments.Count; i++) 
        //{
        //    m_RopeLineRenderer.SetPosition(i, m_RopeSegments[i].CurrentPosition);
        //}
    }

    void FixedUpdate() 
    {
        ApplyVerletIntegration();
        Jakobsen();
        RenderRope();
    }
}
