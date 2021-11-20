using UnityEngine;

[CreateAssetMenu(menuName = "Systems/ToolSystem/TerrainPainterTool/TerrainBrushIsoAllSides")]
public class TerrainBrushIsoAllSides : ITerrainBrush
{
	public override bool AffectsGeometry => true;
	public override void SetBuffer(in Terrain terrain)
	{
		m_BrushDataBuffer = new ComputeBuffer(7 * terrain.chunkSize * terrain.chunkSize * terrain.chunkSize, sizeof(float));
	}
	public override void BeginDispatchShader(in Terrain terrain, in RaycastHit hitPoint, in int xPos, in int yPos, in int zPos)
	{
		int stride = terrain.chunkSize * terrain.chunkSize * terrain.chunkSize;
		int coord = 0;

		for (int i = 0; i < 7; i++)
		{
			ref float[] isoPoints = ref terrain.GetIsoDataFromCoord(xPos + chunksToSet[i].x, yPos + chunksToSet[i].y, zPos + chunksToSet[i].z);
			m_BrushDataBuffer.SetData(isoPoints, 0, coord, stride);
			coord += stride;
		}
		DispatchShader(terrain, hitPoint.point, xPos, yPos, zPos);
		// get data from the centre chunk, I.E the one with an offset of 3*stride
		m_BrushDataBuffer.GetData(terrain.GetIsoDataFromCoord(xPos, yPos, zPos), 0, 3 * stride, stride);
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
