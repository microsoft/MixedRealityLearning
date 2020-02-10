using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TogglePlacementHints : MonoBehaviour
{
    public GameObject[] gameObjectArray;

    // Start is called before the first frame update
    void Start()
    {
        //Toggle objects to start off. Objects are turned on by default - so this will toggle it off.
        ToggleGameObjects();
    }

    public void ToggleGameObjects()
    {
        foreach (GameObject obj in gameObjectArray)
        {
            //Toggle game object being active
            obj.SetActive(!obj.activeSelf);
        }
    }

}
