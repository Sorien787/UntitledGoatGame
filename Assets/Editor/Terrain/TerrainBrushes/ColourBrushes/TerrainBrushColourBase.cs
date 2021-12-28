using UnityEngine;

[CreateAssetMenu(menuName = "Systems/ToolSystem/TerrainPainterTool/TerrainBrushColourBase")]
public class TerrainBrushColourBase : ITerrainBrush
{
	public override bool AffectsGeometry => false;
	public override void BeginDispatchShader(in Terrain terrain, in RaycastHit hitPoint, in int xPos, in int yPos, in int zPos)
	{
		m_BrushDataBuffer.SetData(Grid.GetValueFromGrid(xPos, yPos, zPos, terrain.m_TerrainChunks, terrain.m_TerrainExtent).colourData);
		DispatchShader(terrain, hitPoint.point, xPos, yPos, zPos);
		m_BrushDataBuffer.GetData(Grid.GetValueFromGrid(xPos, yPos, zPos, terrain.m_TerrainChunks, terrain.m_TerrainExtent).colourData);
	}

	public override void SetBuffer(in Terrain terrain)
	{
		m_BrushDataBuffer = new ComputeBuffer(terrain.chunkSize * terrain.chunkSize * terrain.chunkSize, 4 * sizeof(float));
	}
}
