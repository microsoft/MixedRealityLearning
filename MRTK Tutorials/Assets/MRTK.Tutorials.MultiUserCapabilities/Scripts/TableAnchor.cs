using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableAnchor : MonoBehaviour
{
    public static TableAnchor instance;
    // Start is called before the first frame update
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
        Debug.Log("Table Created");
        DontDestroyOnLoad(this.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
