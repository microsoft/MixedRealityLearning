using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewButtonControl : MonoBehaviour
{
    public GameObject[] Models;
    private int ModelIndex = 0;
    
    void Start()
    {
      Models[0].SetActive(true);
    }

    public void NextModel()
    {
        foreach (GameObject model in Models) 
        {
            model.SetActive(false);
        }
        ModelIndex = ModelIndex + 1;
        if(ModelIndex < Models.Length)
        {
            Models[ModelIndex].SetActive(true);
        }
        else
        {
            ModelIndex = 0;
            Models[ModelIndex].SetActive(true);
        }

    }

    public void PreviousModel()
    {
        foreach (GameObject model in Models)
        {
            model.SetActive(false);
        }
        ModelIndex = ModelIndex - 1;
        if(ModelIndex >= 0)
        {
            Models[ModelIndex].SetActive(true);
        }
        else
        {
            ModelIndex = Models.Length-1;
            Models[ModelIndex].SetActive(true);

        }
        
    }
}
