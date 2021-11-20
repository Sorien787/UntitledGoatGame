using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    // Start is called before the first frame update
    TerrainGenerator terrainGenerator;
    Editor terrainGeneratorEditor;
    private IBrush m_SelectedBrush;

    bool autoUpdate = true;
    int _choiceIndex = 0;
    int _brushChoiceIndex = 0;


    private int cachedIndex = -1;
    private bool m_bMouseDown = false;
    private string newTerrainDataName = "New TerrainData";

    void LocateTerrainInLevel() 
    {
        Terrain[] terrains = FindObjectsOfType(typeof(Terrain)) as Terrain[];
        if (terrains.Length > 0) 
        {
            GetTerrain.Add(terrains[0]);
            terrainGenerator.LoadTerrainForEditing(terrains[0]);
        }
    }

    void CreateNewTerrainData(in string terrainDataName)
    {
        
        _choiceIndex = GetTerrain.Count()-1;
        Terrain newTerrain = terrainGenerator.CreateTerrain(terrainDataName, m_scale);
        GetTerrain.Add(newTerrain);
        terrainGenerator.ActiveTerrainDataSettingsChanged();
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
        BrushPropertyAssociator associator = terrainGenerator.GetBrushPropertyAssociator;

        int numBrushes = terrainGenerator.GetTerrainBrushes().Count;
        if (numBrushes < 1)
            return;
        if (_brushChoiceIndex >= numBrushes)
            _brushChoiceIndex = numBrushes - 1;

        string[] brushNames = new string[terrainGenerator.GetTerrainBrushes().Count + 1];
        brushNames[0] = "No Brush Selected";
        for (int i = 1; i < brushNames.Length; i++)
        {
            brushNames[i] = terrainGenerator.GetTerrainBrushes()[i - 1].name;
        }

        _brushChoiceIndex = EditorGUILayout.Popup(_brushChoiceIndex, brushNames);

        if (_brushChoiceIndex == 0)
            m_SelectedBrush = null;
        else
        {
            m_SelectedBrush = terrainGenerator.GetTerrainBrushes()[_brushChoiceIndex-1];
        }
        if (m_SelectedBrush == null)
            return;

        m_SelectedBrush.InitializeBrush();

        if (m_SelectedBrush.ExtendsProperty(associator.GetSizeProperty))
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(associator.GetSizeProperty.ToUpper());

            float val = m_SelectedBrush.GetData<float>(associator.GetSizeProperty);
            val = EditorGUILayout.Slider(val, 0f, 1f);
            m_SelectedBrush.SetData(associator.GetSizeProperty, val);

            GUILayout.EndHorizontal();
        }
        if (m_SelectedBrush.ExtendsProperty(associator.GetStrengthProperty))
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(associator.GetStrengthProperty.ToUpper());

            float val = m_SelectedBrush.GetData<float>(associator.GetStrengthProperty);
            val = EditorGUILayout.Slider(val, 0f, 1f);
            m_SelectedBrush.SetData(associator.GetStrengthProperty, val);
            GUILayout.EndHorizontal();
        }
        if (m_SelectedBrush.ExtendsProperty(associator.GetHardnessProperty))
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(associator.GetHardnessProperty.ToUpper());
            float val = m_SelectedBrush.GetData<float>(associator.GetHardnessProperty);
            val = EditorGUILayout.Slider(val, 0f, 1f);
            m_SelectedBrush.SetData(associator.GetHardnessProperty, val);

            GUILayout.EndHorizontal();
        }
        if (m_SelectedBrush.ExtendsProperty(associator.GetColourProperty))
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(associator.GetColourProperty.ToUpper());

            Color val = m_SelectedBrush.GetData<Color>(associator.GetColourProperty);
            val = EditorGUILayout.ColorField(val);
            m_SelectedBrush.SetData(associator.GetColourProperty, val);

            GUILayout.EndHorizontal();
        }
        if (m_SelectedBrush.ExtendsProperty(associator.GetUseVerticalProperty))
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(associator.GetUseVerticalProperty.ToUpper());

            bool val = m_SelectedBrush.GetData<bool>(associator.GetUseVerticalProperty);
            val = EditorGUILayout.Toggle(val);
            m_SelectedBrush.SetData(associator.GetUseVerticalProperty, val);

            if (!val)          
            {
                if (m_SelectedBrush.ExtendsProperty(associator.GetCacheNormalsProperty))
                {
                    GUILayout.BeginHorizontal("box");
                    GUILayout.Label(associator.GetCacheNormalsProperty.ToUpper());

                    bool val2 = m_SelectedBrush.GetData<bool>(associator.GetCacheNormalsProperty);
                    val2 = EditorGUILayout.Toggle(val2);
                    m_SelectedBrush.SetData(associator.GetCacheNormalsProperty, val2);

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndHorizontal();
        }
    }

    private void RunBrushApplication()
	{
        if (m_SelectedBrush == null)
            return;
            
        Event e = Event.current;
        if (e.type == EventType.MouseDown)
        {
            m_SelectedBrush.OnStartApplyingBrush();
            m_bMouseDown = true;
        }
        if (e.type == EventType.MouseUp)
        {
            m_SelectedBrush.OnLeaveBrush();
            m_bMouseDown = false;
        }
        if (m_bMouseDown)
        {
            Physics.SyncTransforms();

            Physics.autoSimulation = false;
            Physics.Simulate(Time.deltaTime);
            Physics.autoSimulation = true;

            m_SelectedBrush.OnApplyBrush();
        }
    }
    private float m_scale = 1.0f;
    public override void OnInspectorGUI()
    {
        cachedIndex = _choiceIndex;
        DrawDefaultInspector();
        if (_choiceIndex > GetTerrain.Count-1 || _choiceIndex < 0)
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

        _choiceIndex = EditorGUILayout.Popup("Current Terrain", _choiceIndex, _terrainData.Select(x=>x.name).ToArray());

        if (cachedIndex != _choiceIndex && GetTerrain.Count > 0) 
        {
            terrainGenerator.LoadTerrainForEditing(GetTerrain[_choiceIndex]);
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

        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Terrain Scale");
        m_scale = EditorGUILayout.FloatField(m_scale);
        GUILayout.EndHorizontal();


        GUILayout.Label("Create New Terrain");

        GUILayout.BeginHorizontal("box");
        newTerrainDataName = GUILayout.TextField(newTerrainDataName, 25, GUILayout.Width(200.0f));

        if (GUILayout.Button("Create Neww Terrain"))
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
        GUILayout.Label("Update Terrain");

        if (GUILayout.Button("Update"))
        {
            terrainGenerator.ActiveTerrainDataSettingsChanged();
        }
        autoUpdate = GUILayout.Toggle(autoUpdate, "Auto-Update Terrain");
        GUILayout.EndHorizontal();

        if (GetTerrain.Count > 0)
        {  
            DrawSettingsEditor(GetTerrain[_choiceIndex], ref terrainGeneratorEditor);
        }
        GUILayout.Label("Terrain Brushes");

        DrawBrushChoices();

        RunBrushApplication();
    }

    void DrawSettingsEditor(Object settings, ref Editor editor) 
    {
        if (settings != null) 
        {
            //_choiceIndex = EditorGUILayout.Popup("Label", _choiceIndex, _choices);
            //terrain.selectedTerrainData = terrain.terrainData[_choiceIndex];
            bool foldout = EditorGUILayout.InspectorTitlebar(true, settings);
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                if (foldout)
                {
                    CreateCachedEditor(settings, null, ref editor);
                    editor.OnInspectorGUI();
                }
                if (check.changed && autoUpdate)
                {
                    terrainGenerator.ActiveTerrainDataSettingsChanged();
                }
            }
        }
    }

    bool HasValidTerrainSelected() 
    {
        return _choiceIndex > -1 && _choiceIndex < GetTerrain.Count && GetTerrain.Count > 0;
    }

    public static object GetObjectsAtPath (string path) 
    {
        ArrayList al = new ArrayList();
        string pathData = Application.dataPath + "/Resources/" + path;
        string[] fileEntries = Directory.GetFiles(pathData);
        string[] assetNames = fileEntries.Select(x => x.Remove(0, pathData.Length)).ToArray();
        for(int i = 0; i < assetNames.Length; i++)
        {

            string assetPath = path + assetNames[i];
            Object[] t = Resources.LoadAll(assetPath);

            if (t != null)
                al.Add(t);
        }
        object[] result = new object[al.Count];
        for (int i = 0; i < al.Count; i++)
            result[i] = al[i];

        return result;
    }


    private void OnEnable()
    {
        terrainGenerator = (TerrainGenerator)target;
        _terrainData.Clear();
        for (int i = GetTerrain.Count-1; i > 0; i--)
        {
            if (GetTerrain[i] == null)
            {
                GetTerrain.RemoveAt(i);
            }
        }
        foreach (Terrain terrain in FindObjectsOfType<Terrain>())
        {
            _terrainData.Add(terrain);
        }
        if (GetTerrain.Count > 0) 
        {
            terrainGenerator.LoadTerrainForEditing(GetTerrain[_choiceIndex]);
        }
    }

    private List<Terrain> GetTerrain
    {
        get => _terrainData;
    }
    private List<Terrain> _terrainData = new List<Terrain>();
}
