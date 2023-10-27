// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using MixedReality.Toolkit.SpatialManipulation;
using System.Collections;
using UnityEngine;

public class PartAssemblyDemo : MonoBehaviour
{
    public Transform objectToPlace;
    public Transform locationToPlace;

    float nearDistance = 0.1f;

    public AudioSource audioSource;

    bool isSnapped;

    private Vector3 originalObjectPlacementPosition;
    private Quaternion originalObjectPlacementRotation;
    Transform originalParent;
    ObjectManipulator manipulator;
    InteractionFlags originalInteractionFlags;

    // Start is called before the first frame update
    void Start()
    {
        //Get the audio source component to play audio when snapping objects into place
        audioSource = GetComponent<AudioSource>();

        // Cache parent
        originalParent = objectToPlace.parent;

        //Save original placement of object
        originalObjectPlacementPosition = objectToPlace.localPosition;
        originalObjectPlacementRotation = objectToPlace.localRotation;

        //Save original allowed interaction types
        manipulator = GetComponent<ObjectManipulator>();
        originalInteractionFlags = manipulator.AllowedInteractionTypes;

        //Start the coroutine to check for distance every once in a while
        StartCoroutine(CheckForSnap());
    }

    public void ResetPlacement()
    {
        // Reset parent
        objectToPlace.SetParent(originalParent);

        //reset object placement
        objectToPlace.localPosition = originalObjectPlacementPosition;
        objectToPlace.localRotation = originalObjectPlacementRotation;

        manipulator.AllowedInteractionTypes = originalInteractionFlags;

        isSnapped = false;
    }

    //Coroutine to check if object is close enough to target location. If so snap to it.
    IEnumerator CheckForSnap()
    {
        while(true)
        {
            yield return new WaitForSeconds(0.01f);

            if (!isSnapped && Vector3.Distance(objectToPlace.position, locationToPlace.position) > 0.01 && Vector3.Distance(objectToPlace.position, locationToPlace.position) < nearDistance)
            {        
                //Place object at target location
                objectToPlace.position = locationToPlace.position;
                objectToPlace.rotation = locationToPlace.rotation;

                //Set parent to target location so that when rocket launches, parts go with it
                objectToPlace.SetParent(locationToPlace.parent);

                //Play audio snapping sound
                if (!audioSource.isPlaying)
                    audioSource.Play();        

                isSnapped = true;          
            }
            else if (isSnapped)
            {
                //Disable ability to manipulate object
                manipulator.AllowedInteractionTypes = InteractionFlags.None;
            }
        }
    }
}
