using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewButtonControl : MonoBehaviour
{
    public GameObject[] models;
    int modelIndex = 0;
    
    void Start()
    {
      models[0].SetActive(true);
    }

    public void NextModel()
    {
        foreach (GameObject model in models) 
        {
            model.SetActive(false);
        }
        modelIndex = modelIndex + 1;
        if(modelIndex < models.Length)
        {
            models[modelIndex].SetActive(true);
        }
        else
        {
            modelIndex = 0;
            models[modelIndex].SetActive(true);
        }

    }

    public void PreviousModel()
    {
        foreach (GameObject model in models)
        {
            model.SetActive(false);
        }
        modelIndex = modelIndex - 1;
        if(modelIndex >= 0)
        {
            models[modelIndex].SetActive(true);
        }
        else
        {
            modelIndex = models.Length-1;
            models[modelIndex].SetActive(true);

        }
        
    }
}
