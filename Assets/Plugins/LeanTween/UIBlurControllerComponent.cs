using UnityEngine;
using UnityEngine.UI;
namespace S7UI
{
	public class UIBlurControllerComponent : MonoBehaviour
	{
		[Range(1.0f, 10.0f)][SerializeField] private float m_BlurIntensityScalar;
		[Range(0.0f, 1.0f)][SerializeField] private float m_BlurDefaultValue;
		[SerializeField] private Graphic m_SpriteRenderer;
		
		private MaterialPropertyBlock m_PropertyBlock;

		private float m_BlurStrength;

		private void SetBlur(in float m_BlurStrength)
		{
			//m_SpriteRenderer.material.prop
			//m_SpriteRenderer.GetPropertyBlock(m_PropertyBlock);
			//m_PropertyBlock.SetFloat("_Blur", m_BlurStrength * m_BlurIntensityScalar);
			//m_SpriteRenderer.SetPropertyBlock(m_PropertyBlock);
		}

		public float strength
		{
			get { return m_BlurStrength; }
			set { m_BlurStrength = value; SetBlur(m_BlurStrength); }
		}

		private void Awake()
		{
			m_PropertyBlock = new MaterialPropertyBlock();
			strength = m_BlurDefaultValue;
		}
	}
}