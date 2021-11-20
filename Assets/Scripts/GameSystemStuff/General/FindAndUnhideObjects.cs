using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteAlways]
public class FindAndUnhideObjects : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var AllObjects = FindObjectsOfType<GameObject>();
        foreach(var go in AllObjects)
        {
            if ((go.hideFlags & HideFlags.HideInHierarchy) != 0) 
            {
                go.hideFlags = HideFlags.None;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
