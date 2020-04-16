using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SharingModuleScript : MonoBehaviour
{
    AnchorModuleScript anchorModuleScript;

    void Start()
    {
        anchorModuleScript = GetComponent<AnchorModuleScript>();
    }

    public void ShareAzureAnchor()
    {
        Debug.Log("\nSharingModuleScript.ShareAnchor()");

        GenericNetworkManager.instance.AzureAnchorID = anchorModuleScript.currentAzureAnchorID;
        Debug.Log("GenericNetworkManager.AzureAnchorID: " + GenericNetworkManager.instance.AzureAnchorID);

        GameObject PVuser = GenericNetworkManager.instance.localUser.gameObject;
        PhotonUser pu = PVuser.gameObject.GetComponent<PhotonUser>();
        pu.PVShareAnchorNetwork();
    }

    public void GetAzureAnchor()
    {
        Debug.Log("\nSharingModuleScript.GetSharedAnchor()");
        Debug.Log("GenericNetworkManager.AzureAnchorID: " + GenericNetworkManager.instance.AzureAnchorID);

        anchorModuleScript.FindAzureAnchor(GenericNetworkManager.instance.AzureAnchorID);
    }
}
