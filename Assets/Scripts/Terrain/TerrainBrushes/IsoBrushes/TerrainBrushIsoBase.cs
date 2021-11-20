using UnityEngine;

[CreateAssetMenu(menuName = "Systems/ToolSystem/TerrainPainterTool/TerrainBrushIsoBase")]
public class TerrainBrushIsoBase : ITerrainBrush
{
	public override bool AffectsGeometry => true;
	public override void SetBuffer(in Terrain terrain)
	{
		m_BrushDataBuffer = new ComputeBuffer((terrain.chunkSize) * (terrain.chunkSize) * (terrain.chunkSize), sizeof(float));
	}

	public override void BeginDispatchShader(in Terrain terrain, in RaycastHit hitPoint, in int xPos, in int yPos, in int zPos)
	{
		m_BrushDataBuffer.SetData(Grid.GetValueFromGrid(xPos, yPos, zPos, terrain.m_TerrainChunks, terrain.m_TerrainExtent).isoData);
		DispatchShader(terrain, hitPoint.point, xPos, yPos, zPos);
		m_BrushDataBuffer.GetData(Grid.GetValueFromGrid(xPos, yPos, zPos, terrain.m_TerrainChunks, terrain.m_TerrainExtent).isoData);
	}
}
