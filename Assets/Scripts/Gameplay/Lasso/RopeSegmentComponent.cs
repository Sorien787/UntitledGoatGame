using UnityEngine;
using System;

[Serializable]
public class RopeSegmentComponent
{
	#region Properties

    public Vector3 Velocity { get { return CurrentPosition - LastPosition; } }

	public Vector3 CurrentPosition { get; private set; } = Vector3.zero;

	public Vector3 LastPosition { get; private set; } = Vector3.zero;

	#endregion

	#region Public Methods

	public RopeSegmentComponent(in Vector3 position)
	{
		CurrentPosition = position;
		LastPosition = position;
	}

	RaycastHit[] results;
	public void UpdateVerlet(in Vector3 gravityVector,in  float radius, in LayerMask layerMask) 
    {



	}

	public void SetNewPosition(in Vector3 position)
	{
		LastPosition = CurrentPosition;
		CurrentPosition = position;
	}

	public void AnchorNewPosition(in Vector3 position)
	{
		LastPosition = position;
		CurrentPosition = position;
	}

	public void AddToPosition(Vector3 additionalPosition) 
    {
		CurrentPosition += additionalPosition;
    }

	#endregion
}
