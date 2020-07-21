using MRTK.Tutorials.GettingStarted;
using Photon.Pun;

namespace MRTK.Tutorials.MultiUserCapabilities
{
    public class PunPlacementHintsController : MonoBehaviourPun
    {
        private PlacementHintsController placementHintsController;

        private void Start()
        {
            // Cache references
            placementHintsController = GetComponent<PlacementHintsController>();

            // Subscribe to PunPlacementHintsController events
            placementHintsController.OnTogglePlacementHints += OnTogglePlacementHintsHandler;

            // Enable PUN feature
            placementHintsController.IsPunEnabled = true;
        }

        private void OnTogglePlacementHintsHandler()
        {
            photonView.RPC("PunRPC_TogglePlacementHints", RpcTarget.All);
        }

        [PunRPC]
        private void PunRPC_TogglePlacementHints()
        {
            placementHintsController.Toggle();
        }
    }
}
