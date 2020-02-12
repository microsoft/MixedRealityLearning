using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AnchorFeedbackScript : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Reference to the Text Mesh Pro component on this object.")]
    private TextMeshPro feedbackText = default;

    private AnchorModuleScript anchorModuleScript;

    void Awake()
    {
        anchorModuleScript = GameObject.FindObjectOfType<AnchorModuleScript>();

        anchorModuleScript.OnStartASASession += AnchorModuleScript_OnStartASASession;
        anchorModuleScript.OnEndASASession += AnchorModuleScript_OnEndASASession;

        anchorModuleScript.OnCreateAnchorStarted += AnchorModuleScript_OnCreateAnchorStarted;
        anchorModuleScript.OnCreateAnchorSucceeded += AnchorModuleScript_OnCreateAnchorSucceeded;
        anchorModuleScript.OnCreateAnchorFailed += AnchorModuleScript_OnCreateAnchorFailed;

        anchorModuleScript.OnCreateLocalAnchor += AnchorModuleScript_OnCreateLocalAnchor;
        anchorModuleScript.OnRemoveLocalAnchor += AnchorModuleScript_OnRemoveLocalAnchor;

        anchorModuleScript.OnFindASAAnchor += AnchorModuleScript_OnFindASAAnchor;
        anchorModuleScript.OnASAAnchorLocated += AnchorModuleScript_OnASAAnchorLocated;
    }

    void Start()
    {
        feedbackText.text = "";    
    }

    private void AnchorModuleScript_OnStartASASession()
    {
        feedbackText.text = " Starting Azure session";
    }

    private void AnchorModuleScript_OnEndASASession()
    {
        feedbackText.text = "Ending Azure session";
    }

    private void AnchorModuleScript_OnCreateAnchorStarted()
    {
        feedbackText.text = "Creating Azure anchor";
    }

    private void AnchorModuleScript_OnCreateAnchorSucceeded()
    {
        feedbackText.text = "Azure anchor creation succeeded";
    }

    private void AnchorModuleScript_OnCreateAnchorFailed()
    {
        feedbackText.text = "Azure anchor creation failed";
    }

    private void AnchorModuleScript_OnCreateLocalAnchor()
    {
        feedbackText.text = "Creating local anchor";
    }

    private void AnchorModuleScript_OnRemoveLocalAnchor()
    {
        feedbackText.text = "Removing local anchor";
    }

    private void AnchorModuleScript_OnFindASAAnchor()
    {
        feedbackText.text = "Trying to find Azure anchor";
    }

    private void AnchorModuleScript_OnASAAnchorLocated()
    {
        feedbackText.text = "Azure anchor located";
    }
}
