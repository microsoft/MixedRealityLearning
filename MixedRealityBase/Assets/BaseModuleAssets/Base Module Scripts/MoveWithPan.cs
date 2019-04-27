using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;

public class MoveWithPan : MonoBehaviour, IMixedRealityHandPanHandler
{


    private void Start ()
    {

    }

    // Update is called once per frame
    private void Update ()
    {
	}

    public void OnPanEnded(HandPanEventData eventData)
    {

    }

    public void OnPanning(HandPanEventData eventData)
    {
        Vector3 panningPosition = new Vector3(eventData.PanPosition.x, eventData.PanPosition.y , 0.0f);
        transform.localPosition += panningPosition;


    }

    public void OnPanStarted(HandPanEventData eventData)
    {

    }
}
