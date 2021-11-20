using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ExportableTerrainData : ScriptableObject
{
	public Vector3Int m_TerrainExtent = Vector3Int.zero;

	public Vector3Int m_RenderedExtent = Vector3Int.zero;

	public float isoLevel = 0.5f;

	public int extent = 16;

	public float scale = 10.0f;

	public List<ExportableChunkData> chunkData = new List<ExportableChunkData>();
}
