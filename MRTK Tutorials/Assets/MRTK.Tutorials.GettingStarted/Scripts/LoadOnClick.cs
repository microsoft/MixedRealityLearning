using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Microsoft.MixedReality.Toolkit;

public class LoadOnClick : MonoBehaviour
{

    public GameObject GameobjectToDisable;

    // Use this for initialization
    void Awake()
    {

        DontDestroyOnLoad(gameObject);

    }

    public void LoadScene(string levelname)
    {
               
        StartCoroutine(LoadSceneCoRoutine(levelname));
    }

    IEnumerator LoadSceneCoRoutine(string levelname)
    {

        SceneManager.LoadScene("BlankTransitionScene");
        yield return new WaitForSeconds(0.1f);

        var mrtkps = GameObject.Find("MixedRealityPlayspace");
        var mrtk = GameObject.Find("MixedRealityToolkit");

        //If it finds the wrong mrtk object then destroy it and look a second time
        if (mrtk.GetComponent<MixedRealityToolkit>() == null)
        {
            Destroy(mrtk);
            mrtk = GameObject.Find("MixedRealityToolkit");
        }

        GameobjectToDisable.SetActive(false);

        if (mrtkps != null)
        {
            Debug.Log("Destroying MRTK Playspace");
            Destroy(mrtkps);
        }

        if (mrtk != null)
        {
            Debug.Log("Destroying MRTK");
            Destroy(mrtk);
        }

        var objects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject o in objects)
        {
            if (o != gameObject || o.GetComponentInParent<LoadOnClick>())
                Destroy(o);
        }

        yield return new WaitForSeconds(5f);

        SceneManager.LoadScene(levelname);

        yield return new WaitForSeconds(0.1f);

        GameobjectToDisable.SetActive(true);
    }
}
