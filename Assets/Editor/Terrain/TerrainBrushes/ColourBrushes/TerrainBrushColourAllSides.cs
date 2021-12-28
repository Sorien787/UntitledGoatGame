using UnityEngine;
[CreateAssetMenu(menuName = "Systems/ToolSystem/TerrainPainterTool/TerrainBrushColourAllSides")]
public class TerrainBrushColourAllSides : ITerrainBrush
{
	public override bool AffectsGeometry => false;
	public override void SetBuffer(in Terrain terrain)
	{
		m_BrushDataBuffer = new ComputeBuffer(7 * terrain.chunkSize * terrain.chunkSize * terrain.chunkSize, 4 * sizeof(float));
	}
	public override void BeginDispatchShader(in Terrain terrain, in RaycastHit hitPoint, in int xPos, in int yPos, in int zPos)
	{
		int stride = terrain.chunkSize * terrain.chunkSize * terrain.chunkSize;
		int coord = 0;

		for (int i = 0; i < 7; i++)
		{
			ref Color[] isoPoints = ref terrain.GetColorDataFromCoord(xPos + chunksToSet[i].x, yPos + chunksToSet[i].y, zPos + chunksToSet[i].z);
			m_BrushDataBuffer.SetData(isoPoints, 0, coord, stride);
			coord += stride;
		}
		DispatchShader(terrain, hitPoint.point, xPos, yPos, zPos);
		m_BrushDataBuffer.GetData(terrain.GetColorDataFromCoord(xPos, yPos, zPos), 0, 3 * stride, stride);
	}
	readonly static Vector3Int[] chunksToSet = new Vector3Int[]
	{
		new Vector3Int (-1, 0, 0 ),
		new Vector3Int (0, -1, 0 ),
		new Vector3Int (0, 0, -1 ),

		new Vector3Int (0, 0, 0 ),

		new Vector3Int (0, 0, 1 ),
		new Vector3Int (0, 1 , 0 ),
		new Vector3Int (1, 0, 0 )
	};
}
