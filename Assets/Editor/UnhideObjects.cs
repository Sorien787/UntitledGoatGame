using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class UnhideObjects : EditorWindow
{
    [MenuItem("Window/Object Unhider")]
    public static void ShowWindow()
    {
        GetWindow(typeof(UnhideObjects));
    }

    private void OnGUI()
    {
        GUILayout.Label("Unhide Objects");
        if (GUILayout.Button("Unhide"))
        {
            UnhideObjectsNow();

        }   
    }
    void UnhideObjectsNow()
    {
        List<GameObject> rootObjects = new List<GameObject>();

        // get root objects in scene

        Scene scene = SceneManager.GetActiveScene();
        scene.GetRootGameObjects(rootObjects);
        for (int i = 0; i < rootObjects.Count; ++i)
        {

            GameObject gameObject = rootObjects[i];
            UnhideGoAndChildren(gameObject.transform);
        }
    }

    void UnhideGoAndChildren(Transform transform) 
    {
        for (int i = 0; i < transform.childCount; i++) 
        {
            Transform childTransform = transform.GetChild(i);
            UnhideGoAndChildren(childTransform);
            transform.gameObject.hideFlags = HideFlags.None;
        }
    }
}
