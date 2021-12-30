using UnityEngine;
using System.Collections.Generic;

public class OverlaySelectable : MonoBehaviour
{
    private static HashSet<OverlaySelectable> _instances = new HashSet<OverlaySelectable>();
    public static IReadOnlyCollection<OverlaySelectable> Instances { get { return _instances; } }

	[SerializeField] private bool _highlightAlways = true;
	[SerializeField] private int _overlayGroupID = 1;
	[SerializeField] private Color _highlightColor = Color.white;
	[SerializeField] private bool _ZTestAlways = false;

	public Color GetHighlightColor => _highlightColor;
	public bool HighlightAlways => _highlightAlways;
	public int GroupId => _overlayGroupID;
	public bool VisibleNotBehindTerrain => _ZTestAlways;

	private void OnEnable()
    {
        _instances.Add(this);
    }

    private void OnDisable()
    {
        _instances.Remove(this);
    }

	public void EnableOutline(bool enable)
	{
		_highlightAlways = enable;
	}

	public void ZTestAll()
	{
		_ZTestAlways = true;
	}

	public void SetOutlineColour(Color color)
	{
		_highlightColor = color;
	}

	public void ZTestBehindTerrain()
	{
		_ZTestAlways = false;
	}
}