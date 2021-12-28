using UnityEngine;

[CreateAssetMenu(menuName = "Systems/ToolSystem/TerrainPainterTool/TerrainBrushIsoWithNormals")]
public class TerrainBrushIsoWithNormals : ITerrainBrush
{
	[SerializeField] private BrushProperty m_NormalProperty = null;
	[SerializeField] private BrushProperty m_OrientUpProperty = null;
	private bool m_bStartedWithNormal = false;

	public override bool AffectsGeometry => true;

	public override void OnStartApplyingBrush()
	{
		if (!TryRaycastHit(out RaycastHit hit, m_TerrainGenerator.GetTerrainLayer) || m_BrushData.GetValue<IBrushShaderProperty<bool>>(m_OrientUpProperty.GetIdentifier).GetProperty())
		{
			Debug.Log("No hit or orient up : take normal up");
			m_BrushShader.SetFloat("brushNormalX", 0);
			m_BrushShader.SetFloat("brushNormalY", 1);
			m_BrushShader.SetFloat("brushNormalZ", 0);
		}
		else
		{
			Debug.Log("hit normal");
			m_BrushShader.SetFloat("brushNormalX", hit.normal.x);
			m_BrushShader.SetFloat("brushNormalY", hit.normal.y);
			m_BrushShader.SetFloat("brushNormalZ", hit.normal.z);
			m_bStartedWithNormal = true;
		}
	}

	public override void OnApplyBrush()
	{
		Debug.Log("Applying Brush!");
		if (TryRaycastHit(out RaycastHit hit, m_TerrainGenerator.GetTerrainLayer))
		{
			if (!m_BrushData.GetValue<IBrushShaderProperty<bool>>(m_OrientUpProperty.GetIdentifier).GetProperty())
			{
				if (m_BrushData.GetValue<IBrushShaderProperty<bool>>(m_NormalProperty.GetIdentifier).GetProperty())
				{
					Debug.Log("Apply Brush Cached Normal");
					if (m_bStartedWithNormal)
					{
						m_TerrainGenerator.ApplyBrushToTerrain(hit, this, m_BrushData.GetValue<IBrushShaderProperty<float>>(m_SizeBrushProperty.GetIdentifier).GetProperty());
					}
				}
				else
				{
					Debug.Log("Apply Bruh No Cache Normal");
					m_BrushShader.SetFloat("brushNormalX", hit.normal.x);
					m_BrushShader.SetFloat("brushNormalY", hit.normal.y);
					m_BrushShader.SetFloat("brushNormalZ", hit.normal.z);
					m_TerrainGenerator.ApplyBrushToTerrain(hit, this, m_BrushData.GetValue<IBrushShaderProperty<float>>(m_SizeBrushProperty.GetIdentifier).GetProperty());
				}
			}
			else 
			{
				Debug.Log("Apply Brush Orient Up");
				m_TerrainGenerator.ApplyBrushToTerrain(hit, this, m_BrushData.GetValue<IBrushShaderProperty<float>>(m_SizeBrushProperty.GetIdentifier).GetProperty());
			}
		}
	}

	public override void OnLeaveBrush()
	{
		base.OnLeaveBrush();
		m_bStartedWithNormal = false;
	}

	public override void SetBuffer(in Terrain terrain)
	{
		m_BrushDataBuffer = new ComputeBuffer(terrain.chunkSize * terrain.chunkSize * terrain.chunkSize, sizeof(float));
	}

	public override void BeginDispatchShader(in Terrain terrain, in RaycastHit hitPoint, in int xPos, in int yPos, in int zPos)
	{
		m_BrushShader.SetFloats("brushNormal", new float[] { hitPoint.normal.x, hitPoint.normal.y, hitPoint.normal.z });
		m_BrushDataBuffer.SetData(terrain.GetIsoDataFromCoord(xPos, yPos, zPos));
		DispatchShader(terrain, hitPoint.point, xPos, yPos, zPos);
		m_BrushDataBuffer.GetData(terrain.GetIsoDataFromCoord(xPos, yPos, zPos));
	}
}
