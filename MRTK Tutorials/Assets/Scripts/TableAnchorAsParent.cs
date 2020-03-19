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
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
