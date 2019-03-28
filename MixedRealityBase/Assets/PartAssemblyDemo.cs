using Microsoft.MixedReality.Toolkit.Examples.Demos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartAssemblyDemo : MonoBehaviour
{

    public Transform objectToPlace;
    public Transform locationToPlace;

    public float nearDistance = 0.1f;
    public float farDistance = 0.2f;

    public GameObject toolTipObject;
    public AudioSource audioSource;

    bool isSnapped;

    private Vector3 originalObjectPlacementPosition;
    private Quaternion originalObjectPlacementRotation;

    private ManipulationHandler manipulationHandler;

    // Start is called before the first frame update
    void Start()
    {
        //Get the manipulation handler that is attached to the current object
        manipulationHandler = GetComponent<ManipulationHandler>();
        audioSource = GetComponent<AudioSource>();

        //Save original placement of object
        originalObjectPlacementPosition = objectToPlace.position;
        originalObjectPlacementRotation = objectToPlace.rotation;

        //Start the coroutine to check for distance every once in a while
        StartCoroutine(checkForSnap());
    }

    public void ResetPlacement()
    {
        //reset object placement
        objectToPlace.position = originalObjectPlacementPosition;
        objectToPlace.rotation = originalObjectPlacementRotation;

        //turn on tool tips again
        toolTipObject.SetActive(true);
    }

    //Co routine to check if object is close enough to target location. If so snap to it.
    IEnumerator checkForSnap()
    {
        while(true)
        {
            yield return new WaitForSeconds(0.01f);

            if (!isSnapped && Vector3.Distance(objectToPlace.position, locationToPlace.position) != 0 && Vector3.Distance(objectToPlace.position,locationToPlace.position) < nearDistance)
            {
                //Disable manipulation handler to stop continued manipulation of object
                //manipulationHandler.enabled = false;                

                //Place object at target location
                objectToPlace.position = locationToPlace.position;
                objectToPlace.rotation = locationToPlace.rotation;

                //Set parent to target location so that when rocket launches, parts go with it
                objectToPlace.SetParent(locationToPlace.parent);

                //Play audio snapping sound
                audioSource.Play();

                //turn off tool tips
                toolTipObject.SetActive(false);

                //isSnapped = true;          

                //Turn manipulaiton handler back on so that we can grab it again if needed
                //manipulationHandler.enabled = true;
            }

            if (isSnapped && Vector3.Distance(objectToPlace.position, locationToPlace.position) > farDistance)
            {
                isSnapped = false;
            }
        }
    }
}
