using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleButton : MonoBehaviour
{
    public GameObject ClipingObject;
    public void ToggleClipping()
    {
        ClipingObject.SetActive(!ClipingObject.activeInHierarchy);
    }
}
