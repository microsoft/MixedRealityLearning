using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableAnchor : MonoBehaviour
{
    public static TableAnchor instance;

    void Start()
    {
        if (TableAnchor.instance == null)
        {
            TableAnchor.instance = this;
        }
        else
        {
            if (TableAnchor.instance != this)
            {
                Destroy(TableAnchor.instance.gameObject);
                TableAnchor.instance = this;
            }
        }
    }
}
