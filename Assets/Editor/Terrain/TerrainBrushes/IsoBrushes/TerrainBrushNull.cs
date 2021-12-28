using UnityEngine;
[CreateAssetMenu(menuName = "Systems/ToolSystem/TerrainPainterTool/TerrainBrushNull")]
public class TerrainBrushNull : ITerrainBrush
{
	public override void BeginDispatchShader(in Terrain terrain, in RaycastHit hitPoint, in int xPos, in int yPos, in int zPos) { }

	public override void SetBuffer(in Terrain terrain) { }

	public override void OnApplyBrush() { }

	public override void OnChooseBrush() { }

	public override void OnLeaveBrush() { }
}
