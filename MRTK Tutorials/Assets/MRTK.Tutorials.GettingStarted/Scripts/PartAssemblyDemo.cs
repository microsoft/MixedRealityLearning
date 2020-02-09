using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartAssemblyDemo : MonoBehaviour
{

    public Transform objectToPlace;
    public Transform locationToPlace;

    float nearDistance = 0.1f;
    float farDistance = 0.2f;

    public GameObject toolTipObject;
    public AudioSource audioSource;

    bool isManipulating;
    bool isSnapped;

    private Vector3 originalObjectPlacementPosition;
    private Quaternion originalObjectPlacementRotation;
    Transform originalParent;

    // Start is called before the first frame update
    void Start()
    {
        //Get the audio source component to play audio when snapping objects into place
        audioSource = GetComponent<AudioSource>();

        //Save original placement of object
        originalObjectPlacementPosition = objectToPlace.position;
        originalObjectPlacementRotation = objectToPlace.rotation;

        // Chache parent
        originalParent = objectToPlace.parent;

        //Start the coroutine to check for distance every once in a while
        StartCoroutine(checkForSnap());
    }

    public void ResetPlacement()
    {
        //reset object placement
        objectToPlace.position = originalObjectPlacementPosition;
        objectToPlace.rotation = originalObjectPlacementRotation;

        // Reset parent
        objectToPlace.SetParent(originalParent);

        //turn on tool tips again
        toolTipObject.SetActive(true);
    }

    public void setIsManipulating(bool value)
    {
        isManipulating = value;
    }

    //Co routine to check if object is close enough to target location. If so snap to it.
    IEnumerator checkForSnap()
    {
        while(true)
        {
            yield return new WaitForSeconds(0.01f);

            if (!isSnapped && Vector3.Distance(objectToPlace.position, locationToPlace.position) > 0.01 && Vector3.Distance(objectToPlace.position,locationToPlace.position) < nearDistance)
            {        

                //Place object at target location
                objectToPlace.position = locationToPlace.position;
                objectToPlace.rotation = locationToPlace.rotation;

                //Set parent to target location so that when rocket launches, parts go with it
                objectToPlace.SetParent(locationToPlace.parent);

                //Play audio snapping sound
                //TODO: Need to take into account whether manipulation handler is currently being held
                //if (!audioSource.isPlaying)
                    audioSource.Play();

                //turn off tool tips
                toolTipObject.SetActive(false);        

                //isSnapped = true;          

            }

            if (isSnapped && Vector3.Distance(objectToPlace.position, locationToPlace.position) > farDistance)
            {
                isSnapped = false;
            }
        }
    }
}
