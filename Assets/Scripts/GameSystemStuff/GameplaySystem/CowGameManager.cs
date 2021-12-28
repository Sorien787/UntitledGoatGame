using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "GameManager")]
public class CowGameManager : ScriptableObject, IObjectiveListener
{
	[SerializeField] private EntityInformation m_PlayerEntityInformation;
	[SerializeField] private LayerMask m_TerrainLayerMask;
	[SerializeField] private EntityInformation m_HazardType;
	[SerializeField] private RestartState m_RestartState;

	[SerializeField] private List<LevelData> m_LevelData = new List<LevelData>();

	private readonly Dictionary<EntityInformation, List<EntityToken>> m_EntityCache = new Dictionary<EntityInformation, List<EntityToken>>();
	private readonly List<LevelObjective> m_ObjectiveDict = new List<LevelObjective>();
	private readonly Dictionary<UIObjectReference, GameObject> m_UICache = new Dictionary<UIObjectReference, GameObject>();

	private Transform m_PlayerCameraTransform;
	private Transform m_PlayerCameraContainerTransform;

	// Enums for defining entity state, restart state, etc.
	#region EnumDefinitions
	public enum EntityState
	{
		Free,
		Hunted,
		Abducted
	}

	public enum RestartState
	{
		Default,
		Quick,
		Debug
	}
	#endregion

	// Properties for variable access outside the game manager
	#region Properties
	public int GetCurrentLevelIndex { get; private set; } = 0;

	public EntityInformation GetHazardType { get => m_HazardType; }

	public Transform GetPlayerCameraContainerTransform => m_PlayerCameraContainerTransform;

	public RestartState GetRestartState { get => m_RestartState; }

	public EntityTypeComponent GetPlayer => m_EntityCache[m_PlayerEntityInformation][0].GetEntityType;

	public LevelManager GetCurrentLevel { get; private set; } = null;

	public LevelData GetLevelDataByLevelIndex(in int index) => m_LevelData[index];

	public int GetNumLevels => m_LevelData.Count;
	#endregion

	// Called by LevelManager mostly, for scene transitions, etc
	#region LevelTransitionFunctions
	public void MoveToNextLevel()
	{
		SceneManager.LoadScene(GetCurrentLevelIndex++);
	}

	public bool HasLevelStarted()
	{
		return m_bHasStarted;
	}

	public void SetDefaultEntry()
	{
		if (m_RestartState != RestartState.Debug)
			m_RestartState = RestartState.Default;
	}

	public void SetQuickEntry()
	{
		if (m_RestartState != RestartState.Debug)
			m_RestartState = RestartState.Quick;
	}

	public void OnHazardSpawn(HazardComponent hazard) 
	{
		foreach(KeyValuePair<EntityInformation, List<EntityToken>> thing in m_EntityCache) 
		{
			if (!hazard.Affects(thing.Key))
				continue;

			foreach(EntityToken token in thing.Value) 
			{
				if ((token.GetEntityTransform.position - hazard.GetPosition).sqrMagnitude > hazard.GetHazardRadius)
					continue;

				AnimalComponent animalComponent = token.GetEntityTransform.GetComponent<AnimalComponent>();

				if (!animalComponent)
					return;

				animalComponent.OnScaredByHazard(hazard);
			}
		}
	}

	public void ResetAllLevels()
	{
		foreach(LevelData levelData in m_LevelData)
		{
			levelData.Reset();
		}
	}

	public void MoveToSceneWithSceneId(in int levelIndex)
	{
		GetCurrentLevelIndex = levelIndex;
		SceneManager.LoadScene(GetCurrentLevelIndex);
	}

	public void RestartCurrentLevel() 
	{
		m_RestartState = RestartState.Quick;
		SceneManager.LoadScene(GetCurrentLevelIndex);
	}

	public void MoveToMenu() 
	{
		GetCurrentLevelIndex = 0;
		SceneManager.LoadScene(GetCurrentLevelIndex);
	}

	// called when new scene is loaded
	public void NewLevelLoaded(LevelManager newLevel)
	{
		m_bHasStarted = false;
		m_bIsInPlayerPerspective = false;
		GetCurrentLevel = newLevel;
		GetCurrentLevelIndex = newLevel.GetLevelNumber;

		ClearLevelData();
		LevelData levelData = m_LevelData[GetCurrentLevelIndex - 1];
		m_NumObjectivesToComplete = levelData.GetObjectiveCount;
		newLevel.InitializeLevel(levelData, m_PlayerCameraTransform);
		levelData.ForEachObjective(AddNewObjective);
	}

	private void AddNewObjective(LevelObjective objective) 
	{
		m_ObjectiveDict.Add(objective); 
		objective.AddObjectiveListener(this);
		if (objective.GetObjectiveType == ObjectiveType.Population && m_EntityCache.TryGetValue(objective.GetEntityInformation, out List<EntityToken> tokens)) 
		{
			objective.IncrementCounter(tokens.Count);
		}
	}

	// called when new scene is beginning to load
	public void ClearLevelData()
	{
		m_LevelData[GetCurrentLevelIndex-1].ForEachObjective((LevelObjective objective) =>
		{
			objective.ClearListeners();
		});
		m_NumObjectivesToComplete = 0;
		m_NumObjectivesCompleted = 0;
		m_EntityCache.Clear();
		m_ObjectiveDict.Clear();
		m_LevelListeners.Clear();
		m_PauseListeners.Clear();
		m_UICache.Clear();
	}
	#endregion

	// Functions relating to pause/unpause functionality
	#region PauseUnpause
	public event Action OnPaused;
	public event Action OnUnpaused;
	private bool m_bHasStarted = false;
	private bool m_bIsPaused = false;

	private UnityUtils.ListenerSet<ILevelListener> m_LevelListeners = new UnityUtils.ListenerSet<ILevelListener>();
	private UnityUtils.ListenerSet<IPauseListener> m_PauseListeners = new UnityUtils.ListenerSet<IPauseListener>();
	public void AddToPauseUnpause(IPauseListener pausable) 
	{
		if (m_bIsPaused) pausable.Pause();
		else pausable.Unpause();
		m_PauseListeners.Add(pausable);

	}
	public void RemoveFromPauseUnpause(IPauseListener pausable)
	{
		m_PauseListeners.Remove(pausable);
	}

	public void AddToLevelStarted(ILevelListener listener) 
	{
		m_LevelListeners.Add(listener);
		if (m_bHasStarted) 
			listener.LevelStarted();
		if (m_bIsInPlayerPerspective)
			listener.PlayerPerspectiveBegin();
	}

	public void SetPausedState(bool pauseState)
	{
		m_bIsPaused = pauseState;
		if (pauseState)
		{
			Cursor.lockState = CursorLockMode.None;
			m_PauseListeners.ForEachListener((IPauseListener listener) => listener.Pause());
		}
		else
		{
			Cursor.lockState = CursorLockMode.Locked;
			m_PauseListeners.ForEachListener((IPauseListener listener) => listener.Unpause());
		}
	}
	#endregion

	// Called when level loads up for initialization purposes
	#region LevelInitializationFunctions
	public void RegisterCamera(Transform camTransform)
	{
		m_PlayerCameraTransform = camTransform;
	}

	public void RegisterInitialCameraContainerTransform(Transform containerTransform)
	{
		m_PlayerCameraContainerTransform = containerTransform;
	}
	#endregion

	// Called due to objectives completing/uncompleting, animals/players dying, etc.
	#region LevelEventFunctions

	public void OnPlayerKilled()
	{
		GetCurrentLevel.RestartLevel();
		m_RestartState = RestartState.Quick;
	}
	private bool m_bIsInPlayerPerspective = false;
	public void EnterPlayerPerspective()
	{
		m_bIsInPlayerPerspective = true;
		m_LevelListeners.ForEachListener((ILevelListener listener) => listener.PlayerPerspectiveBegin());
	}

	public void StartLevel() 
	{
		m_bHasStarted = true;

		TryBeginSuccessCountdown();
		m_LevelListeners.ForEachListener((ILevelListener listener) => listener.LevelStarted()); 
		m_LevelData[GetCurrentLevelIndex - 1].ForEachObjective((LevelObjective objective) => {
			objective.StartLevel();
		});
	}

	private int m_NumObjectivesToComplete = 0;
	private int m_NumObjectivesCompleted = 0;

	public void OnCounterChanged(in int val){}
	public void OnTimerTriggered(in Action totalTime, in int time){}
	public void OnTimerRemoved(){}
	public void InitializeData(LevelObjective objective) { }

	public void OnObjectiveFailed()
	{
		GetCurrentLevel.OnLevelFailed();
		m_RestartState = RestartState.Quick;
	}

	public void OnObjectiveEnteredGoal()
	{
		m_NumObjectivesCompleted++;
		TryBeginSuccessCountdown();
	}

	private void TryBeginSuccessCountdown() 
	{
		if (m_NumObjectivesCompleted == m_NumObjectivesToComplete && m_bHasStarted && m_NumObjectivesCompleted != 0)
		{
			GetCurrentLevel.StartSucceedCountdown();
		}
	}

	public void OnObjectiveLeftGoal()
	{
		if(m_NumObjectivesCompleted == m_NumObjectivesToComplete)
		{
			GetCurrentLevel.EndSucceedCountdown();
		}
		m_NumObjectivesCompleted--;
	}

	public void OnEntityEnterPen(GameObject go)
	{
		EntityInformation inf = go.GetComponent<EntityTypeComponent>().GetEntityInformation;
		for (int i = 0; i < m_ObjectiveDict.Count; i++)
		{
			if (m_ObjectiveDict[i].GetEntityInformation == inf && m_ObjectiveDict[i].GetObjectiveType == ObjectiveType.Capturing)
			{
				m_ObjectiveDict[i].IncrementCounter();
			}
		}
	}

	public void OnEntityLeavePen(GameObject go)
	{
		EntityInformation inf = go.GetComponent<EntityTypeComponent>().GetEntityInformation;
		for (int i = 0; i < m_ObjectiveDict.Count; i++)
		{
			if (m_ObjectiveDict[i].GetEntityInformation == inf && m_ObjectiveDict[i].GetObjectiveType == ObjectiveType.Capturing)
			{
				m_ObjectiveDict[i].IncrementCounter();
			}
		}
	}

	public void OnEntitySpawned(EntityTypeComponent entity)
	{
		if (!m_EntityCache.TryGetValue(entity.GetEntityInformation, out List<EntityToken> entities))
		{
			entities = new List<EntityToken>();
			m_EntityCache.Add(entity.GetEntityInformation, entities);
		}
		for (int i = 0; i < m_ObjectiveDict.Count; i++)
		{
			if (m_ObjectiveDict[i].GetEntityInformation == entity.GetEntityInformation && m_ObjectiveDict[i].GetObjectiveType == ObjectiveType.Population)
			{
				m_ObjectiveDict[i].IncrementCounter();
			}
		}
		EntityToken newToken = new EntityToken(entity);
		entities.Add(newToken);		
	}

	public void OnEntityStartTracking(EntityTypeComponent entity) 
	{
		for (int i = 0; i < m_EntityCache[entity.GetEntityInformation].Count; i++)
		{
			if (m_EntityCache[entity.GetEntityInformation][i].GetEntityType == entity)
			{
				m_EntityCache[entity.GetEntityInformation][i].SetTrackable();
			}
		}
	}

	public void OnEntityKilled(EntityTypeComponent entity)
	{
		for (int i = 0; i < m_EntityCache[entity.GetEntityInformation].Count; i++)
		{
			if (m_EntityCache[entity.GetEntityInformation][i].GetEntityType == entity)
			{
				m_EntityCache[entity.GetEntityInformation].RemoveAt(i);
				if (m_EntityCache[entity.GetEntityInformation].Count == 0)
				{
					m_EntityCache.Remove(entity.GetEntityInformation);
				}
				break;
			}
		}

		for (int i = 0; i < m_ObjectiveDict.Count; i++)
		{
			if (m_ObjectiveDict[i].GetEntityInformation == entity.GetEntityInformation && m_ObjectiveDict[i].GetObjectiveType == ObjectiveType.Population)
			{
				m_ObjectiveDict[i].DecrementCounter();
			}
		}
	}

	public void OnEntityStopTracking(EntityTypeComponent entity)
	{
		for (int i = 0; i < m_EntityCache[entity.GetEntityInformation].Count; i++)
		{
			if (m_EntityCache[entity.GetEntityInformation][i].GetEntityType == entity)
			{
				m_EntityCache[entity.GetEntityInformation][i].SetUntrackable();
			}
		}
	}
	#endregion

	// Miscellaneous used helper functions
	#region MiscFunctions

	public bool IsGroundLayer(in int layer)
	{
		return UnityUtils.UnityUtils.IsLayerInMask(m_TerrainLayerMask, layer);
	}

	public int GetGroundLayer() 
	{
		return m_TerrainLayerMask;
	}

	public EntityToken GetTokenForEntity(in EntityTypeComponent gameObject, in EntityInformation entityType)
	{
		if (m_EntityCache.TryGetValue(entityType, out List<EntityToken> value))
		{
			for (int i = 0; i < value.Count; i++)
			{
				if (value[i].GetEntityType == gameObject)
				{
					return value[i];
				}
			}
		}
		return null;
	}

	public void ForEachAnimal(in Action<EntityToken> forEachDelegate) 
	{
		
		foreach(List<EntityToken> tokens in m_EntityCache.Values) 
		{
			foreach(EntityToken token in tokens) 
			{
				forEachDelegate(token);
			}
		}
	}

	public void GetTransformsMatchingType(in EntityInformation entity, out List<EntityToken> outEntityToken)
	{
		outEntityToken = m_EntityCache[entity];
	}

	public bool GetClosestTransformMatchingList(in Vector3 currentPos, out EntityToken outEntityToken, List<EntityState> validEntities, bool allowDeadEntities = false, params EntityInformation[] entities)
	{
		if (validEntities == null)
		{
			validEntities = new List<EntityState> { EntityState.Abducted, EntityState.Free, EntityState.Hunted };
		}
		outEntityToken = null;
		float cachedSqDist = Mathf.Infinity;
		foreach (EntityInformation entityInformation in entities)
		{
			if (m_EntityCache.ContainsKey(entityInformation))
			{
				// for all entities in the cache
				foreach (EntityToken token in m_EntityCache[entityInformation])
				{
					// if the entity is in a valid state for the purposes of this request
					if (validEntities != null)
					{
						foreach (EntityState data in validEntities)
						{
							if (token.GetEntityState == data && token.IsTrackable && (allowDeadEntities || !token.IsDead))
							{
								float sqDist = Vector3.SqrMagnitude(token.GetEntityType.GetTrackingTransform.position - currentPos);
								if (sqDist < cachedSqDist)
								{
									cachedSqDist = sqDist;
									outEntityToken = token;
								}
							}
						}
					}
					else
					{
						float sqDist = Vector3.SqrMagnitude(token.GetEntityType.GetTrackingTransform.position - currentPos);
						if (sqDist < cachedSqDist)
						{
							cachedSqDist = sqDist;
							outEntityToken = token;
						}
					}
				}
			}
		}
		return outEntityToken != null;
	}

	public void OnUIElementSpawned(UIObjectElement elem, UIObjectReference refe)
	{
		m_UICache.Add(refe, elem.gameObject);
	}

	public GameObject GetUIElementFromReference(UIObjectReference refe)
	{
		return m_UICache[refe];
	}

	public void OnObjectiveValidated(){}

	#endregion

}

// Interfaces for use for listeners of pausing/starting/finishing levels
#region ListenersInterfaces
public interface IPauseListener 
{
	void Pause();
	void Unpause();
}

public interface ILevelListener 
{
	void LevelStarted();
	void LevelFinished();
	void PlayerPerspectiveBegin();
}
#endregion

public class EntityToken 
{
	public EntityToken(in EntityTypeComponent go) 
	{
		GetEntityTransform = go.transform;
		GetEntityType = go;
		GetEntityState = CowGameManager.EntityState.Free;
	}
	public void SetAbductionState(in CowGameManager.EntityState reservationType) 
	{
		GetEntityState = reservationType;
	}

	private bool m_bTrackable = true;

	public void SetTrackable() { m_bTrackable = true; }

	public void SetUntrackable() { m_bTrackable = false; }

	public CowGameManager.EntityState GetEntityState { get; private set; }

	public Transform GetEntityTransform { get; private set; }

	public EntityTypeComponent GetEntityType { get; private set; }

	public bool IsDead => GetEntityType.IsDead;

	public bool IsTrackable => m_bTrackable;
}
