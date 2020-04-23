using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodeView : MonoBehaviour
{
    [Header("Game Objects to move")]
    [SerializeField] List<GameObject> gameObjects;
    [Header("Exploded view objects")]
    [SerializeField] List<GameObject> explodedGameObjects;
    [Header("Explosion settings")]
    [SerializeField] float explosionSpeed = 0.1f;

    bool isInDefaultPosition = false;
    List<Vector3> startingPos = new List<Vector3>();
    List<Vector3> explodedPos = new List<Vector3>();
    
    void Start()
    {
        // capture the starting position and exploded position of the objects
        foreach (var item in gameObjects)
        {
            startingPos.Add(item.transform.localPosition);
        }
        foreach (var item in explodedGameObjects)
        {
            explodedPos.Add(item.transform.localPosition);
        }
    }

    void Update()
    {
        // reverse position based on the position we are currently in
        if (isInDefaultPosition)
        {
            // move objects to exploded position
            for (int i = 0; i < gameObjects.Count; i++)
            {
                gameObjects[i].transform.localPosition = Vector3.Lerp(gameObjects[i].transform.localPosition, explodedPos[i], explosionSpeed);
                
            }
        }
        else
        {
            // move objects to default position
            for (int i = 0; i < gameObjects.Count; i++)
            {
                gameObjects[i].transform.localPosition = Vector3.Lerp(gameObjects[i].transform.localPosition, startingPos[i], explosionSpeed);
            }
        }
    }

    public void ToggleExplodedView()
    {
        if (!isInDefaultPosition)
        {
            isInDefaultPosition = true;
        }
        else
        {
            isInDefaultPosition = false;
        }
    }
}
