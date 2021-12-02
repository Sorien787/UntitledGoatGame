using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class TerrainEditor : EditorWindow
{
    [MenuItem("Window/Terrain Editor")]
    public static void ShowWindow()
    {
        GetWindow(typeof(TerrainEditor));
    }


    [SerializeField] [HideInInspector] TerrainGenerator m_TerrainGenerator;
    Editor terrainGeneratorEditor;
    [SerializeField] [HideInInspector] private IBrush m_SelectedBrush;

    bool autoUpdate = true;
    int _choiceIndex = 0;
    int _brushChoiceIndex = 0;


    private int cachedIndex = -1;
    private bool m_bMouseDown = false;
    private string newTerrainDataName = "New TerrainData";

    void LocateTerrainInLevel()
    {
        Terrain[] terrains = FindObjectsOfType(typeof(Terrain)) as Terrain[];
        GetTerrain.Clear();
        foreach(Terrain terrain in terrains) 
        {
            GetTerrain.AddRange(terrains);
        }

        m_TerrainGenerator.LoadTerrainForEditing(terrains[0]);
    }

    void CreateNewTerrainData(in string terrainDataName)
    {

        _choiceIndex = GetTerrain.Count - 1;
        Terrain newTerrain = m_TerrainGenerator.CreateTerrain(terrainDataName, m_scale);
        GetTerrain.Add(newTerrain);
        m_TerrainGenerator.ActiveTerrainDataSettingsChanged();
    }


    void DeleteTerrainData(in int i)
    {
        if (GetTerrain.Count > 0)
        {
            GetTerrain[i].DeleteTerrain();
            GetTerrain.RemoveAt(i);
            if (i > GetTerrain.Count - 1)
            {
                _choiceIndex = GetTerrain.Count - 1;
            }
        }

    }

    void ClearTerrain(in int i)
    {
        if (GetTerrain.Count > 0)
        {
            GetTerrain[i].ResetTerrain();
        }

    }

    void DrawBrushChoices()
    {
        BrushPropertyAssociator associator = m_TerrainGenerator.GetBrushPropertyAssociator;

        int numBrushes = m_TerrainGenerator.GetTerrainBrushes().Count;
        if (numBrushes < 1)
            return;
        if (_brushChoiceIndex > numBrushes)
            _brushChoiceIndex = numBrushes;

        string[] brushNames = new string[m_TerrainGenerator.GetTerrainBrushes().Count + 1];
        brushNames[0] = "No Brush Selected";
        for (int i = 1; i < brushNames.Length; i++)
        {
            brushNames[i] = m_TerrainGenerator.GetTerrainBrushes()[i - 1].name;
            m_TerrainGenerator.GetTerrainBrushes()[i - 1].SetTerrainGenerator(m_TerrainGenerator);
        }

        _brushChoiceIndex = EditorGUILayout.Popup(_brushChoiceIndex, brushNames);

        if (_brushChoiceIndex == 0)
            m_SelectedBrush = null;
        else
        {
            IBrush brushToSelect = m_TerrainGenerator.GetTerrainBrushes()[_brushChoiceIndex - 1];
            if (brushToSelect != m_SelectedBrush)
                brushToSelect.OnChooseBrush();

            m_SelectedBrush = brushToSelect;
        }
        if (m_SelectedBrush == null)
            return;

        if (m_SelectedBrush.ExtendsProperty(associator.GetSizeProperty))
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(associator.GetSizeProperty.ToUpper(), GUILayout.Width(100f));

            float val = m_SelectedBrush.GetData<float>(associator.GetSizeProperty);
            val = EditorGUILayout.Slider(val, 0f, 20f);
            m_SelectedBrush.SetData(associator.GetSizeProperty, val);

            GUILayout.EndHorizontal();
        }
        if (m_SelectedBrush.ExtendsProperty(associator.GetStrengthProperty))
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(associator.GetStrengthProperty.ToUpper(), GUILayout.Width(100f));

            float val = m_SelectedBrush.GetData<float>(associator.GetStrengthProperty);
            val = EditorGUILayout.Slider(val, 0f, 1f);
            m_SelectedBrush.SetData(associator.GetStrengthProperty, val);
            GUILayout.EndHorizontal();
        }
        if (m_SelectedBrush.ExtendsProperty(associator.GetHardnessProperty))
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(associator.GetHardnessProperty.ToUpper(), GUILayout.Width(100f));
            float val = m_SelectedBrush.GetData<float>(associator.GetHardnessProperty);
            val = EditorGUILayout.Slider(val, 0f, 1f);
            m_SelectedBrush.SetData(associator.GetHardnessProperty, val);

            GUILayout.EndHorizontal();
        }
        if (m_SelectedBrush.ExtendsProperty(associator.GetColourProperty))
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(associator.GetColourProperty.ToUpper(), GUILayout.Width(100f));

            Color val = m_SelectedBrush.GetData<Color>(associator.GetColourProperty);
            val = EditorGUILayout.ColorField(val);
            m_SelectedBrush.SetData(associator.GetColourProperty, val);

            GUILayout.EndHorizontal();
        }
        if (m_SelectedBrush.ExtendsProperty(associator.GetCacheNormalsProperty))
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(associator.GetCacheNormalsProperty.ToUpper(), GUILayout.Width(100f));

            bool val = m_SelectedBrush.GetData<bool>(associator.GetCacheNormalsProperty);
            val = EditorGUILayout.Toggle(val);
            m_SelectedBrush.SetData(associator.GetCacheNormalsProperty, val);

            GUILayout.EndHorizontal();
        }
        if (m_SelectedBrush.ExtendsProperty(associator.GetUseVerticalProperty)) 
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(associator.GetUseVerticalProperty.ToUpper(), GUILayout.Width(100f));

            bool val = m_SelectedBrush.GetData<bool>(associator.GetUseVerticalProperty);
            val = EditorGUILayout.Toggle(val);
            m_SelectedBrush.SetData(associator.GetUseVerticalProperty, val);

            GUILayout.EndHorizontal();
        }
    }

    private void RunBrushApplication()
    {
        if (m_SelectedBrush == null)
            return;
        if (m_bMouseDown)
        {
            //Vector3 mousePos = Event.current.mousePosition;
            //mousePos.y = SceneView.lastActiveSceneView.camera.pixelHeight - mousePos.y;
            //Ray ray = SceneView.lastActiveSceneView.camera.ScreenPointToRay(mousePos);
            //bool hashit = Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity);

            //if (hashit)
            //    Debug.Log("!");


            m_SelectedBrush.OnApplyBrush();
        }
    }

	private void OnEnable()
	{
        SceneView.beforeSceneGui += SceneGUI;
	}

	private void OnDisable()
	{
        SceneView.beforeSceneGui -= SceneGUI;
	}

	void SceneGUI(SceneView sceneView) 
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && !m_bMouseDown)
        {
            if (m_SelectedBrush)
                 m_SelectedBrush.OnStartApplyingBrush();
            m_bMouseDown = true;
            e.Use();
        }
        if (e.type == EventType.MouseUp && e.button == 0 && m_bMouseDown)
        {
            m_bMouseDown = false;
            e.Use();
        }


        if (m_bMouseDown)
            RunBrushApplication();
    }
    private float m_scale = 1.0f;
    private void OnGUI()
    {
        if (!m_TerrainGenerator)
		{
            if (m_SelectedBrush)
			{
                m_SelectedBrush.OnLeaveBrush();
                m_SelectedBrush = null;
                _brushChoiceIndex = 0;
			}
            List<TerrainGenerator> assets = new List<TerrainGenerator>();
            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(TerrainGenerator)));
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                TerrainGenerator asset = AssetDatabase.LoadAssetAtPath<TerrainGenerator>(assetPath);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            if (assets.Count > 0)
                m_TerrainGenerator = assets[0];
			else 
            {
                GUILayout.Label("No asset of type TerrainGenerator found in assets. Please make sure that one is present before continuing.");
                return;
            }
        }


        for(int i = 0; i < _terrainData.Count; i++) 
        {
            if (!_terrainData[i]) 
            {
                _terrainData.RemoveAt(i);
                i--;
            }
        }

        cachedIndex = _choiceIndex;
       // DrawDefaultInspector();
        if (_choiceIndex > GetTerrain.Count - 1 || _choiceIndex < 0)
        {
            _choiceIndex = GetTerrain.Count - 1;
        }
        GUILayout.Label("Load Terrain From Level");
        if (GUILayout.Button("Find Terrain"))
        {
            LocateTerrainInLevel();
        }
        GUILayout.Label("Choose or Delete Terrain");
        GUILayout.BeginHorizontal("box");

        _choiceIndex = EditorGUILayout.Popup("Current Terrain", _choiceIndex, _terrainData.Select(x => x.name).ToArray());

        if (cachedIndex != _choiceIndex && GetTerrain.Count > 0)
        {
            m_TerrainGenerator.LoadTerrainForEditing(GetTerrain[_choiceIndex]);
        }

        if (GUILayout.Button("Clear Terrain"))
        {
            ClearTerrain(_choiceIndex);
        }

        if (GUILayout.Button("Delete Terrain"))
        {
            DeleteTerrainData(_choiceIndex);
        }

        GUILayout.EndHorizontal();
        GUILayout.Label("Create New Terrain");

        GUILayout.BeginHorizontal("box");
        newTerrainDataName = GUILayout.TextField(newTerrainDataName, 25, GUILayout.Width(200.0f));

        if (GUILayout.Button("Create New Terrain"))
        {
            if (!newTerrainDataName.Equals(""))
            {
                CreateNewTerrainData(newTerrainDataName);
                newTerrainDataName = "";
            }
        }
        if (_choiceIndex > GetTerrain.Count - 1 || _choiceIndex < 0)
        {
            _choiceIndex = GetTerrain.Count - 1;
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal("box");

        GUILayout.Label("Terrain Scale");
        m_scale = EditorGUILayout.FloatField(m_scale);

        GUILayout.EndHorizontal();


        if (GetTerrain.Count == 0)
        {
            if (m_SelectedBrush)
            {
                m_SelectedBrush.OnLeaveBrush();
                m_SelectedBrush = null;
                _brushChoiceIndex = 0;
            }
            GUILayout.Label("No asset of type Terrain found in gameworld. Please find one, or create one to continue.");
            return;
        }
        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Update Terrain");

        if (GUILayout.Button("Update"))
        {
            m_TerrainGenerator.ActiveTerrainDataSettingsChanged();
        }
        autoUpdate = GUILayout.Toggle(autoUpdate, "Auto-Update Terrain");
        GUILayout.EndHorizontal();

        if (GetTerrain.Count > 0)
        {
            DrawSettingsEditor(GetTerrain[_choiceIndex]);
        }
        GUILayout.Label("Terrain Brushes");

        DrawBrushChoices();

    }

    void DrawSettingsEditor(Object settings)
    {
        if (settings != null)
        {
            bool foldout = EditorGUILayout.InspectorTitlebar(true, settings);
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                if (foldout)
                {
                    Editor editor = Editor.CreateEditor(settings);
                    editor.DrawDefaultInspector();
                }
                if (check.changed && autoUpdate)
                {
                    m_TerrainGenerator.ActiveTerrainDataSettingsChanged();
                }
            }
        }
    }

    private List<Terrain> GetTerrain
    {
        get => _terrainData;
    }
    private List<Terrain> _terrainData = new List<Terrain>();
}
