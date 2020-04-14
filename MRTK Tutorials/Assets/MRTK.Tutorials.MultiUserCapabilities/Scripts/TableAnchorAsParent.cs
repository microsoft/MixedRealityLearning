using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableAnchorAsParent : MonoBehaviour
{
    void Start()
    {
        if (TableAnchor.instance != null)
        {
            transform.parent = TableAnchor.instance.transform;
        }
    }
}
