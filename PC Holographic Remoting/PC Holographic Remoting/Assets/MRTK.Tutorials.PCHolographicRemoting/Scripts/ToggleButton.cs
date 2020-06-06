using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleButton : MonoBehaviour
{
    public GameObject ClipingObject;
    public void ToggleCliping()
    {
        ClipingObject.SetActive(!ClipingObject.activeInHierarchy);
    }
}
