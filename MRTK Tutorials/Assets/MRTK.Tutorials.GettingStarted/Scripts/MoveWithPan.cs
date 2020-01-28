using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class MoveWithPan : MonoBehaviour
{
    /// <summary>
    /// Moves a game object in response to panning motion from the specified
    /// panzoom component.
    /// </summary>
    [SerializeField]
    [Tooltip("The pan object to listen to events from. If null, will look for first parent")]
    private HandInteractionPanZoom panInputSource;
    private void OnEnable()
    {
        if (panInputSource == null)
        {
            panInputSource = GetComponentInParent<HandInteractionPanZoom>();
        }
        if (panInputSource == null)
        {
            Debug.LogError("MoveWithPan did not find a HandInteractionPanZoom to listen to, the component will not work", gameObject);
        }
        else
        {
            panInputSource.PanUpdated.AddListener(OnPanning);
        }
    }

    private void OnDisable()
    {
        if (panInputSource != null)
        {
            panInputSource.PanUpdated.RemoveListener(OnPanning);
        }
    }

    public void OnPanning(HandPanEventData eventData)
    {
        Vector3 panningPosition = new Vector3(eventData.PanDelta.x, eventData.PanDelta.y * -1, 0.0f);
        transform.localPosition += panningPosition;
    }
}
