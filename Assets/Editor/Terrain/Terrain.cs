using UnityEngine;
using System;
using System.Collections.Generic;
public class Terrain : MonoBehaviour
{
	public Material m_MeshMaterial;
	public ExportableTerrainData GetExportableData() 
	{
		ExportableTerrainData data = ScriptableObject.CreateInstance(typeof(ExportableTerrainData)) as ExportableTerrainData;
		data.extent = chunkSize;
		data.isoLevel = isoLevel;
		data.scale = scale;
		data.m_RenderedExtent = new Vector3Int(extentX, extentY, extentZ);
		data.m_TerrainExtent = m_TerrainExtent;
		return data;
	}

	public void LoadImportedData(in ExportableTerrainData data) 
	{
		isoLevel = data.isoLevel;
		scale = data.scale;
		extentX = data.m_RenderedExtent.x;
		extentY = data.m_RenderedExtent.y;
		extentZ = data.m_RenderedExtent.z;

		m_TerrainExtent = data.m_TerrainExtent;
	}

	[Header("Position Settings")]
	public Vector3 originPosition;

	[Header("Extent Settings")]
	[Range(1, 100)]
	public int extentX = 15;

	[Range(1, 100)]
	public int extentY = 15;

	[Range(1, 100)]
	public int extentZ = 15;

	public float isoLevel = 0.5f;

	public int chunkSize => chunksExtent;

	public float scale = 10;

	public Chunk[] m_TerrainChunks = new Chunk[0];
	public Vector3Int m_TerrainExtent = Vector3Int.zero;

	[Header("Global Settings")]
	public static int chunksExtent = 16;
	public static float[] emptyIsoArray = new float[chunksExtent * chunksExtent * chunksExtent];
	public static Color[] emptyColorArray = new Color[chunksExtent * chunksExtent * chunksExtent];

	[SerializeField]
	public Dictionary<string, object> m_BrushData = new Dictionary<string, object>();

	public bool IsPosInTerrain(in int x, in int y, in int z)
	{
		return (!(x >= m_TerrainExtent.x || y >= m_TerrainExtent.y || z >= m_TerrainExtent.z || x < 0 || y < 0 || z < 0));
	}

	public int ChunkIndex(in int x, in int y, in int z)
	{
		return z * m_TerrainExtent.x * m_TerrainExtent.y + y * m_TerrainExtent.x + x;
	}

	public ref float[] GetIsoDataFromCoord(in int x, in int y, in int z)
	{
		if (IsPosInTerrain(x, y, z))
		{
			return ref Grid.GetValueFromGrid(x, y, z, m_TerrainChunks, m_TerrainExtent).isoData;
		}
		else
		{
			return ref emptyIsoArray;
		}
	}

	public ref Color[] GetColorDataFromCoord(in int x, in int y, in int z)
	{
		if (IsPosInTerrain(x, y, z))
		{
			return ref Grid.GetValueFromGrid(x, y, z, m_TerrainChunks, m_TerrainExtent).colourData;
		}
		else
		{
			return ref emptyColorArray;
		}
	}

	private static void IterateExtent(in Vector3Int lowExtent, in Vector3Int highExtent, in Action<int, int, int> func) 
	{
		for (int x1 = lowExtent.x; x1 < highExtent.x; x1++)
		{
			for (int y1 = lowExtent.y; y1 < highExtent.y; y1++)
			{
				for (int z1 = lowExtent.z; z1 < highExtent.z; z1++)
				{
					func(x1, y1, z1);
				}
			}
		}
	}

	public void RecalcNumChunks()
	{
		Vector3Int oldRenderedChunks = m_TerrainExtent;
		Vector3Int newRenderedChunks = new Vector3Int(1 + Mathf.FloorToInt((float)(extentX) / (chunkSize)), 1 + Mathf.FloorToInt((float)(extentY) / (chunkSize)), 1 + Mathf.FloorToInt((float)(extentZ) / (chunkSize)));

		Vector3Int chunksToUpdateFrom = Vector3Int.Min(oldRenderedChunks, newRenderedChunks);
		Vector3Int chunksToUpdateTo = Vector3Int.Max(oldRenderedChunks, newRenderedChunks);

		Grid.ResizeKeepMax(newRenderedChunks, ref m_TerrainExtent, ref m_TerrainChunks);
		// Resize createdChunks to include new chunks, keep old chunks that are to be deleted for now so that we can access them

		//IterateExtent(Vector3Int.Max(chunksToUpdateFrom-Vector3Int.one, Vector3Int.zero), m_TerrainExtent, (int x, int y, int z) =>
		//{
		//	if (x >= newRenderedChunks.x|| y >= newRenderedChunks.y || z >= newRenderedChunks.z) 
		//	{
		//		Chunk chunk = Grid.GetValueFromGrid(x, y, z, m_TerrainChunks, m_TerrainExtent);
		//		if (chunk != null)
		//		{
		//			DestroyImmediate(chunk.gameObject);
		//			return;
		//		}
		//	}

		//	Vector3Int newRenderTo = new Vector3Int(Mathf.Min(chunkSize + 1, extentX - x * chunkSize), Mathf.Min(chunkSize + 1, extentY - y * chunkSize), Mathf.Min(chunkSize + 1, extentZ - z * chunkSize));
		//	Vector3Int newRenderFrom = new Vector3Int(Mathf.Max(1 - x, 0), Mathf.Max(1 - y, 0), Mathf.Max(1 - z, 0));

		//	Chunk chunkOfInterest = Grid.GetValueFromGrid(x, y, z, m_TerrainChunks, m_TerrainExtent);

		//	if (chunkOfInterest == null)
		//	{
		//		chunkOfInterest = CreateChunk(x, y, z);
		//		Grid.SetGridValue(x, y, z, chunkOfInterest, m_TerrainChunks, m_TerrainExtent);
		//		chunkOfInterest.renderTo = newRenderTo;
		//		chunkOfInterest.renderFrom = newRenderFrom;
		//		return;
		//	}

		//	//if the render to value is different, change it and flag the chunk for updates. only flag if chunk already existed.
		//	if (chunkOfInterest.renderTo != newRenderTo)
		//	{
		//		chunkOfInterest.renderTo = newRenderTo;
		//		chunkOfInterest.shouldRerender = true;
		//	}
		//});



		for (int x = 0; x < m_TerrainExtent.x; x++)
		{
			for (int y = 0; y < m_TerrainExtent.y; y++)
			{
				for (int z = 0; z < m_TerrainExtent.z; z++)
				{
					// if the chunk was present in the old set, and is outside new rendered range, destroy it.
					if (x >= newRenderedChunks.x || y >= newRenderedChunks.y || z >= newRenderedChunks.z)
					{
						if (Grid.GetValueFromGrid(x, y, z, m_TerrainChunks, m_TerrainExtent))
						{
							DestroyImmediate(Grid.GetValueFromGrid(x, y, z, m_TerrainChunks, m_TerrainExtent));
							continue;
						}
					}

					Vector3Int newRenderTo = new Vector3Int(Mathf.Min(chunkSize + 1, extentX - x * chunkSize), Mathf.Min(chunkSize + 1, extentY - y * chunkSize), Mathf.Min(chunkSize + 1, extentZ - z * chunkSize));
					Vector3Int newRenderFrom = new Vector3Int(Mathf.Max(1 - x, 0), Mathf.Max(1 - y, 0), Mathf.Max(1 - z, 0));

					Chunk chunkOfInterest = Grid.GetValueFromGrid(x, y, z, m_TerrainChunks, m_TerrainExtent);

					if (chunkOfInterest == null)
					{
						chunkOfInterest = CreateChunk(x, y, z);
						Grid.SetGridValue(x, y, z, chunkOfInterest, m_TerrainChunks, m_TerrainExtent);
						chunkOfInterest.renderTo = newRenderTo;
						chunkOfInterest.renderFrom = newRenderFrom;
						continue;
					}

					//if the render to value is different, change it and flag the chunk for updates. only flag if chunk already existed.
					if (chunkOfInterest.renderTo != newRenderTo)
					{
						chunkOfInterest.renderTo = newRenderTo;
						chunkOfInterest.shouldRerender = true;
					}
				}
			}
		}

		Grid.Resize(newRenderedChunks, ref m_TerrainExtent, ref m_TerrainChunks);
	}


	public Chunk CreateChunk(in int x, in int y, in int z)
	{
		//instantiate the chunk gameobject
		GameObject chunk = new GameObject($"Chunk ({x}, {y}, {z})");
		chunk.transform.parent = gameObject.transform;
		// add the chunk component to the chunk gameobject

		Chunk newChunk = chunk.AddComponent<Chunk>();
		newChunk.gameObject.SetActive(true);
		newChunk.SetMaterial(m_MeshMaterial);
		newChunk.gameObject.layer = 9;
		Grid.SetGridValue(x, y, z, newChunk, m_TerrainChunks, m_TerrainExtent);
		newChunk.shouldRerender = true;

		// if the chunk already exists in terraindata, get the data, and transfer it to the new chunk
		// else, we make new chunk data, and add it to terraindata
		if (!newChunk.isInitialized)
		{
			InitializeChunkDataGrid(newChunk, x, y, z);
		}
		return newChunk;
	}
	public void InitializeChunkDataGrid(in Chunk chunk, in int xPos, in int yPos, in int zPos)
	{
		chunk.isoData = new float[(chunkSize) * (chunkSize) * (chunkSize)];
		chunk.colourData = new Color[(chunkSize) * (chunkSize) * (chunkSize)];
		int chunkIndex;

		for (int z = 0; z < chunkSize; z++)
		{
			for (int y = 0; y < chunkSize; y++)
			{
				for (int x = 0; x < chunkSize; x++)
				{
					chunkIndex = z * (chunkSize) * (chunkSize) + y * (chunkSize) + x;
					chunk.isoData[chunkIndex] = AssignGridIsoValue(x + xPos * chunkSize, y + yPos * chunkSize, z + zPos * chunkSize);
					chunk.colourData[chunkIndex] = AssignGridColourValue(x + xPos * chunkSize, y + yPos * chunkSize, z + zPos * chunkSize);
				}
			}
		}
	}

	public event Action OnTerrainSettingsChanged;

	public static float AssignGridIsoValue(int x, int y, int z)
	{
		return Mathf.Clamp(1.0f - 0.04f * (y), 0, 1);
	}

	public static Color AssignGridColourValue(int x, int y, int z) 
	{
		return Color.grey;
	}

	// delete all chunk gameobjects
	public void DeleteTerrain()
	{
		for (int x = 0; x < m_TerrainExtent.x; x++)
		{
			for (int y = 0; y < m_TerrainExtent.y; y++)
			{
				for (int z = 0; z < m_TerrainExtent.z; z++)
				{
					Chunk chunk = Grid.GetValueFromGrid(x, y, z, m_TerrainChunks, m_TerrainExtent);
					if (chunk)
						chunk.DestroyChunkAndData();
				}
			}
		}
		DestroyImmediate(gameObject);
	}

	public void ResetTerrain()
	{
		for (int x = 0; x < m_TerrainExtent.x; x++)
		{
			for (int y = 0; y < m_TerrainExtent.y; y++)
			{
				for (int z = 0; z < m_TerrainExtent.z; z++)
				{
					InitializeChunkDataGrid(Grid.GetValueFromGrid(x, y, z, m_TerrainChunks, m_TerrainExtent), x, y, z);
				}
			}
		}
	}
}
