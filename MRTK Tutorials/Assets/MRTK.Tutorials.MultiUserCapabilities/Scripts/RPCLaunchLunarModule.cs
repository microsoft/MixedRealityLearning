using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.Networking;

public class RPCLaunchLunarModule : MonoBehaviourPunCallbacks, IInRoomCallbacks
{
    public float thrust;
    public Rigidbody rb;
    public bool ThrustOn;
    public GameObject[] gameObjectArray;

    private PhotonView photonView1;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private TogglePlacementHints ToggleHints;

    public Transform objectToPlace;
    public Transform locationToPlace;

    public AudioSource audioSource;
    public GameObject toolTipObject;

    public float nearDistance = 0.1f;
    public float farDistance = 0.2f;

    [SerializeField]
    bool isSnapped;

    private Vector3 originalObjectPlacementPosition;
    private Quaternion originalObjectPlacementRotation;
    public Transform originalParentObject;

    OwnershipHandler ownershipHandler;
    void Start()
    {
        ownershipHandler = GetComponent<OwnershipHandler>();

        rb = GetComponent<Rigidbody>();
        photonView1 = GetComponent<PhotonView>();

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;

        originalObjectPlacementPosition = objectToPlace.localPosition;
        originalObjectPlacementRotation = objectToPlace.localRotation;
        originalParentObject = objectToPlace.transform.parent;

        ToggleHints = GetComponent<TogglePlacementHints>();

        photonView1.RPC("PartAssembly", RpcTarget.All);
    }

    [PunRPC]
    private void resetModule()
    {
        StopThurster();
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        //transform.localScale = originalScale;
    }

    [PunRPC]
    private void StartThurster()
    {
        StartCoroutine(Thruster());
    }

    private void StopThurster()
    {
        ThrustOn = false;
    }

    private IEnumerator Thruster()
    {
        rb.isKinematic = false;

        ThrustOn = true;

        yield return null;

        while (ThrustOn)
        {
            yield return new WaitForSeconds(0.01f);
            rb.AddForce(transform.up * thrust);
        }
    }

    [PunRPC]
    private void ToggleGameObjects1()
    {
        ToggleHints.ToggleGameObjects();
    }

    [PunRPC]
    private void PartAssembly()
    {
        StartCoroutine(checkForSnap());
    }

    public void ResetPlacement()
    {
        photonView1.RPC("RPC_ResetPlacement", RpcTarget.All);
    }

    [PunRPC]
    void RPC_ResetPlacement()
    {
        photonView1.RPC("IsNotSnapped", RpcTarget.All);
        photonView1.RPC("ResetParent", RpcTarget.All);

        objectToPlace.localPosition = originalObjectPlacementPosition;
        objectToPlace.localRotation = originalObjectPlacementRotation;

        photonView1.RPC("ShowToolTip", RpcTarget.All);
    }

    IEnumerator checkForSnap()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.01f);

            if (!isSnapped && Vector3.Distance(objectToPlace.position, locationToPlace.position) > 0.01 && Vector3.Distance(objectToPlace.position, locationToPlace.position) < nearDistance)
            {
                audioSource.Play();

                objectToPlace.position = locationToPlace.position;
                objectToPlace.rotation = locationToPlace.rotation;

                photonView1.RPC("IsSnapped", RpcTarget.All);
                photonView1.RPC("ChangeParent", RpcTarget.All);

                photonView1.RPC("HideToolTip", RpcTarget.All);
            }

            if (isSnapped && Vector3.Distance(objectToPlace.position, locationToPlace.position) > 0.01)
            {
                objectToPlace.position = locationToPlace.position;
                objectToPlace.rotation = locationToPlace.rotation;
            }
        }
    }

    [PunRPC]
    private void IsSnapped()
    {
        isSnapped = true;
    }

    [PunRPC]
    private void IsNotSnapped()
    {
        isSnapped = false;
    }

    [PunRPC]
    private void ShowToolTip()
    {
        if (toolTipObject != null)
        {
            toolTipObject.SetActive(true);
        }
    }

    [PunRPC]
    private void HideToolTip()
    {
        if (toolTipObject != null)
        {
            toolTipObject.SetActive(false);
        }
    }

    [PunRPC]
    private void ChangeParent()
    {
        objectToPlace.SetParent(locationToPlace.parent);
    }

    [PunRPC]
    private void ResetParent()
    {
        objectToPlace.SetParent(originalParentObject);
    }

    public void launch()
    {
        photonView1.RPC("StartThurster", RpcTarget.All);
    }

    public void reset()
    {
        photonView1.RPC("resetModule", RpcTarget.All);
    }

    public void hints()
    {
        photonView1.RPC("ToggleGameObjects1", RpcTarget.All);
    }
}
