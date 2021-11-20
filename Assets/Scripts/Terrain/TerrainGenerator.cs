 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class MeshData
{
	public Vector3Int meshPosition = Vector3Int.zero;
	public float gridValue = 0;
}

[Serializable]
public static class Grid
{
	public static void Resize<G>(in Vector3Int _newExtent, ref Vector3Int extent, ref G[] arr) 
	{
		Vector3Int maintainedMaximum = new Vector3Int(Mathf.Max(extent.x, _newExtent.x), Mathf.Max(extent.y, _newExtent.y), Mathf.Max(extent.z, _newExtent.z));

		G[] newArray = new G[_newExtent.x * _newExtent.y * _newExtent.z];
		for (int x = 0; x < maintainedMaximum.x; x++)
		{
			for (int y = 0; y < maintainedMaximum.y; y++)
			{
				for (int z = 0; z < maintainedMaximum.z; z++)
				{
					//element is present and should be (within both old and new extents)
					if (x < extent.x && x < _newExtent.x && y < _newExtent.y && y < extent.y && z < extent.z && z < _newExtent.z)
					{
						newArray[z * _newExtent.x * _newExtent.y + y * _newExtent.x + x] = arr[z * extent.x * extent.y + y * extent.x + x];
					}
				}
			}
		}
		arr = newArray;
		extent = _newExtent;
	}

	public static void ResizeKeepMax<G>(in Vector3Int _newExtent, ref Vector3Int extent, ref G[] arr)
	{
		Vector3Int maintainedMaximum = new Vector3Int(Mathf.Max(extent.x, _newExtent.x), Mathf.Max(extent.y, _newExtent.y), Mathf.Max(extent.z, _newExtent.z));

		G[] newArray = new G[maintainedMaximum.x * maintainedMaximum.y * maintainedMaximum.z];
		for (int x = 0; x < maintainedMaximum.x; x++)
		{
			for (int y = 0; y < maintainedMaximum.y; y++)
			{
				for (int z = 0; z < maintainedMaximum.z; z++)
				{
					if (x < extent.x && y < extent.y && z < extent.z)
					{
						newArray[z * maintainedMaximum.x * maintainedMaximum.y + y * maintainedMaximum.x + x] = arr[z * extent.x * extent.y + y * extent.x + x];
					}
				}
			}
		}
		arr = newArray;
		extent = maintainedMaximum;
	}

	public static ref T GetValueFromGrid<T>(in int x, in int y, in int z, in T[] arr, in Vector3Int extent)
	{
		return ref arr[z * extent.x * extent.y + y * extent.x + x];
	}

	public static void SetGridValue<T>(in int x, in int y, in int z, in T value, in T[] arr, in Vector3Int extent)
	{
		arr[z * extent.y * extent.x + y * extent.x + x] = value;
	}
}
[CreateAssetMenu(menuName = "Systems/TerrainGenerator")]
public class TerrainGenerator : ScriptableObject
{
	[HideInInspector]
	public Terrain workingTerrain;
	[Header("Shaders")]
	[SerializeField]
	private ComputeShader marchShader = null;

	[SerializeField]
	private Material meshMaterial = null;

	[Space]
	[Header("Brushes")]
	[SerializeField]
	private List<ITerrainBrush> m_TerrainBrushes;

	[Space]
	[Header("Brush Associators")]
	[SerializeField]
	private BrushPropertyAssociator m_BrushPropertyAssociator;
	[Space]
	[Header("Terrain Layers")]
	[SerializeField]
	private int m_TerrainLayer;
	[SerializeField] [Range(1, 10)] private uint m_DelayedColliderUpdateFrames = 1;

	public Material GetMeshMaterial => meshMaterial;

	public LayerMask GetTerrainLayer => 1 << m_TerrainLayer;

	public BrushPropertyAssociator GetBrushPropertyAssociator => m_BrushPropertyAssociator;

	private Queue<Chunk> m_ColliderUpdateChunks = new Queue<Chunk>();

	[SerializeField] [HideInInspector] private ComputeBuffer triangleBuffer;
	[SerializeField] [HideInInspector] private ComputeBuffer triCountBuffer;
	[SerializeField] [HideInInspector] private ComputeBuffer chunkColoursBuffer;
	[SerializeField] [HideInInspector] private ComputeBuffer isoPointsBuffer;

	private bool waitingForColliderUpdate = false;


	public void Awake()
	{
		ReleaseBuffers();
	}

	public ref List<ITerrainBrush> GetTerrainBrushes() => ref m_TerrainBrushes;

	public Terrain CreateTerrain(in string name, in float terrainScale)
	{
		Terrain terrain = new GameObject { name = name, layer = m_TerrainLayer }.AddComponent<Terrain>();
		terrain.scale = terrainScale;
		terrain.m_MeshMaterial = meshMaterial;
		LoadTerrainForEditing(terrain);
		return terrain;
	}

	public void LoadTerrainForEditing(in Terrain terrain)
	{
		if (workingTerrain) workingTerrain.OnTerrainSettingsChanged -= ActiveTerrainDataSettingsChanged;
		workingTerrain = terrain;
		if (workingTerrain) workingTerrain.OnTerrainSettingsChanged += ActiveTerrainDataSettingsChanged; 
		ReleaseBuffers();
		CreateBuffers();
	}

	/// Creates the buffer for use in mesh builder and brush editing
	void CreateBuffers()
	{
		if (triangleBuffer == null)
		{
			int numPoints = (workingTerrain.chunkSize) * (workingTerrain.chunkSize) * (workingTerrain.chunkSize);
			int numVoxels = (workingTerrain.chunkSize - 1) * (workingTerrain.chunkSize - 1) * (workingTerrain.chunkSize - 1);
			int maxTriangleCount = numVoxels * 5;
			//int textureLength = terrainTexture.GetTextureSize * terrainTexture.GetTextureSize;

			triangleBuffer = new ComputeBuffer(maxTriangleCount, 72, ComputeBufferType.Append);
			marchShader.SetBuffer(0, "triangles", triangleBuffer);

			//angleColoursBuffer = new ComputeBuffer(textureLength, sizeof(float) * 4);
			//marchShader.SetBuffer(0, "angleColours", angleColoursBuffer);

			isoPointsBuffer = new ComputeBuffer(8 * numPoints, sizeof(float));
			marchShader.SetBuffer(0, "isoBuffer", isoPointsBuffer);

			chunkColoursBuffer = new ComputeBuffer(8 * numPoints, 4 * sizeof(float));
			marchShader.SetBuffer(0, "colourBuffer", chunkColoursBuffer);


			//marchShader.SetInt("textureSize", terrainTexture.GetTextureSize);
			marchShader.SetFloat("scale", workingTerrain.scale);
			marchShader.SetFloat("isoLevel", workingTerrain.isoLevel);
			marchShader.SetInt("extent", workingTerrain.chunkSize);

			triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
		}
	}

	int k = 0;


	void ReleaseBuffersOnRecompile()
	{
		ReleaseBuffers();
	}

	/// Releases buffers memory after we're finished with them
	void ReleaseBuffers()
	{
		if (triangleBuffer != null)
		{
			triangleBuffer.Release();
			triangleBuffer = null;
			triCountBuffer.Release();
			triCountBuffer = null;

			chunkColoursBuffer.Release();
			chunkColoursBuffer = null;
			isoPointsBuffer.Release();
			isoPointsBuffer = null;
		}
	}

	public void ActiveTerrainDataSettingsChanged()
	{
		CreateBuffers();
		workingTerrain.RecalcNumChunks();
		UpdateAllChunks(true);
	}

	private int m_NumToUpdateEachFrame = 0;
	/// Update all chunks if their variables within call for such, build the mesh, update the colliders
	void UpdateAllChunks(bool updateColliders)
	{
		int num = 0;
		for (int x = 0; x < workingTerrain.m_TerrainExtent.x; x++)
		{
			for (int y = 0; y < workingTerrain.m_TerrainExtent.y; y++)
			{
				for (int z = 0; z < workingTerrain.m_TerrainExtent.z; z++)
				{
					UpdateChunkMesh(x,y,z, updateColliders, ref num);
				}
			}
		}
		if (num != 0)
		{
			if (m_DelayedColliderUpdateFrames == 0)
				m_NumToUpdateEachFrame = 1;
			m_NumToUpdateEachFrame = Mathf.CeilToInt((float)m_ColliderUpdateChunks.Count / m_DelayedColliderUpdateFrames);
		}
		ColliderUpdate();
	}

	/// Get chunks within radius of brush click and brush size
	public void ApplyBrushToTerrain(in RaycastHit hit, in ITerrainBrush brush, in float brushSize)
	{
		Vector3 hitPoint = hit.point;
		hitPoint /= workingTerrain.scale;
		Vector2 xChunkRange = new Vector2(hitPoint.x - brushSize, hitPoint.x + brushSize) / workingTerrain.chunkSize;
		Vector2 yChunkRange = new Vector2(hitPoint.y - brushSize, hitPoint.y + brushSize) / workingTerrain.chunkSize;
		Vector2 zChunkRange = new Vector2(hitPoint.z - brushSize, hitPoint.z + brushSize) / workingTerrain.chunkSize;

		xChunkRange.x = Mathf.Clamp(Mathf.FloorToInt(xChunkRange.x), 0, workingTerrain.m_TerrainExtent.x - 1);
		xChunkRange.y = Mathf.Clamp(Mathf.FloorToInt(xChunkRange.y), 0, workingTerrain.m_TerrainExtent.x - 1);

		yChunkRange.x = Mathf.Clamp(Mathf.FloorToInt(yChunkRange.x), 0, workingTerrain.m_TerrainExtent.y - 1);
		yChunkRange.y = Mathf.Clamp(Mathf.FloorToInt(yChunkRange.y), 0, workingTerrain.m_TerrainExtent.y - 1);

		zChunkRange.x = Mathf.Clamp(Mathf.FloorToInt(zChunkRange.x), 0, workingTerrain.m_TerrainExtent.z - 1);
		zChunkRange.y = Mathf.Clamp(Mathf.FloorToInt(zChunkRange.y), 0, workingTerrain.m_TerrainExtent.z - 1);

		bool brushHitTerrain = false;
		for (int x = (int)xChunkRange.x; x <= (int)xChunkRange.y; x++)
		{

			for (int y = (int)yChunkRange.x; y <= (int)yChunkRange.y; y++)
			{
				for (int z = (int)zChunkRange.x; z <= (int)zChunkRange.y; z++)
				{
					brushHitTerrain = true;
					brush.BeginDispatchShader(workingTerrain, hit, x, y, z);
					Chunk chunk = Grid.GetValueFromGrid(x, y, z, workingTerrain.m_TerrainChunks, workingTerrain.m_TerrainExtent);
					chunk.shouldRerender = true;
				}
			}
		}
		if (brushHitTerrain)
		{
			UpdateAllChunks(brush.AffectsGeometry);
		}
	}

	/// Updates chunk mesh by building/rebuilding the grid, passing its points and colours to a buffer, and setting the shader to return the correct vertex/triangles data to the mesh
	public void UpdateChunkMesh(in int x, in int y, in int z, bool updateColliders, ref int numAdded)
	{
		Chunk chunkOfInterest = Grid.GetValueFromGrid(x, y, z, workingTerrain.m_TerrainChunks, workingTerrain.m_TerrainExtent);

		if (chunkOfInterest.shouldRerender)
		{
			chunkOfInterest.shouldRerender = false;
			SetBufferData(x, y, z);
			triangleBuffer.SetCounterValue(0);

			marchShader.SetInts("chunkPos", new int[] {x,y,z});
			marchShader.SetFloats("chunkOrigin", new float[] { 0, 0, 0 });
			marchShader.SetInts("renderTo", new int[] { chunkOfInterest.renderTo.x, chunkOfInterest.renderTo.y, chunkOfInterest.renderTo.z });
			marchShader.SetInts("renderFrom", new int[] { chunkOfInterest.renderFrom.x, chunkOfInterest.renderFrom.y, chunkOfInterest.renderFrom.z });

			marchShader.Dispatch(marchShader.FindKernel("March"), (workingTerrain.chunkSize) / 8, (workingTerrain.chunkSize) / 8, (workingTerrain.chunkSize) / 8);

			ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);

			int[] triCountArray = { 0 };
			triCountBuffer.GetData(triCountArray);
			int numTris = triCountArray[0];
			Triangle[] tris = new Triangle[numTris];
			triangleBuffer.GetData(tris, 0, 0, numTris);

			var vertices = new Vector3[numTris * 3];
			var meshTriangles = new int[numTris * 3];
			var meshColours = new Color[numTris * 3];

			for (int i = 0; i < numTris; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					vertices[i * 3 + j] = tris[i][j];
					meshTriangles[i * 3 + j] = i * 3 + j;
					meshColours[i * 3 + j] = tris[i][3 + j].ToVector4(1);
				}
			}

			chunkOfInterest.SetMesh(vertices, meshTriangles, meshColours);
			
			if (!m_ColliderUpdateChunks.Contains(chunkOfInterest) && updateColliders)
			{
				numAdded++;
				m_ColliderUpdateChunks.Enqueue(chunkOfInterest);            //Add this chunk to those we should re-render colliders for
			}
		}
	}

	void SetBufferData(in int x, in int y, in int z)
	{
		// use ComputeBuffer.SetData(data, arraystartInded = 0, computeBufferStartIndex = 0 || 4096|| 8192....
		int stride = (workingTerrain.chunkSize) * (workingTerrain.chunkSize) * (workingTerrain.chunkSize);
		int coord = 0;
		for (int zIndex = z; zIndex < z + 2; zIndex++)
		{
			for (int yIndex = y; yIndex < y+2; yIndex ++)
			{
				for (int xIndex = x; xIndex < x+2; xIndex++)
				{
					isoPointsBuffer.SetData(workingTerrain.GetIsoDataFromCoord(xIndex, yIndex, zIndex), 0, coord, stride);
					chunkColoursBuffer.SetData(workingTerrain.GetColorDataFromCoord(xIndex, yIndex, zIndex), 0, coord, stride);
					coord += stride;
				}
			}
		}
	}

	/// Struct for retrieving data from GPU, or else it returns in random order as threads finish different processes at different times and add to the buffers out of sequence
	public struct Triangle
	{
		public Vector3 vertexA;
		public Vector3 vertexB;
		public Vector3 vertexC;

		public Vector3 colourA;
		public Vector3 colourB;
		public Vector3 colourC;

		public Vector3 this[int i]
		{
			get
			{
				switch (i)
				{
					case 0:
						return vertexA;
					case 1:
						return vertexB;
					case 2:
						return vertexC;
					case 3:
						return colourA;
					case 4:
						return colourB;
					default:
						return colourC;
				}
			}
		}
	}

	public void ColliderUpdate() 
	{
		for (int i = 0; i < m_NumToUpdateEachFrame; i++) 
		{
			if (m_ColliderUpdateChunks.Count == 0)
				return;
			Chunk chunk = m_ColliderUpdateChunks.Dequeue();
			chunk.SetCollider(); 
		}
		//if (m_ColliderUpdateChunks.Count > 0)
		//{
		//	JobifiedBaking.BakeMeshes(m_ColliderUpdateChunks);
		//	m_ColliderUpdateChunks.Clear();
		//	waitingForColliderUpdate = false;
		//}
	}

	/// Coroutine for collider update; calls JobifiedBaking.BakeMeshes to bake each mesh asynchronously
	IEnumerator WaitForColliderUpdate()		//Not sure why it needs to be in a coroutine but it's what I've found works for now
	{
		if (!waitingForColliderUpdate && m_ColliderUpdateChunks.Count > 0)
		{
			waitingForColliderUpdate = true;
			yield return null;
			ColliderUpdate();
		}
	}
}

