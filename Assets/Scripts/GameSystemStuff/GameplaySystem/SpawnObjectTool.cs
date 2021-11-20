using UnityEngine;

public class SpawnObjectTool : ScriptableObject, ITool
{
	private Vector3 m_AdditionalScalingVal;

	private Vector3 m_EulerAngles;

	private StateMachine<SpawnObjectTool> m_SpawnObjectStateMachine;

	public void OnInitialized() 
	{

	}
	public void OnToolActivated()
	{

	}

	public void OnToolDeactivated()
	{

	}

	public void OnUpdate()
	{
		// chosen object

		// choose object by holding shift + clicking?

		// show object by collision

		// when holding right click, go into rotation mode 

		// when holding shift, scale up and down in chosen axis (all, then x, y, z)

		// cache rotation and scaling settings

		// when left clicking, 


	}
}

public class ObjectMovementState : AStateBase<SpawnObjectTool> 
{
}

public class ObjectRotateState : AStateBase<SpawnObjectTool>
{
}

public class ObjectScaleState : AStateBase<SpawnObjectTool>
{
}

