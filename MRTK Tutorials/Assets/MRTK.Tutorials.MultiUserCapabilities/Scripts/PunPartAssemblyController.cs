using MRTK.Tutorials.GettingStarted;
using Photon.Pun;

namespace MRTK.Tutorials.MultiUserCapabilities
{
    /// <summary>
    ///     Handles PUN RPC for PartAssemblyController.
    /// </summary>
    public class PunPartAssemblyController : MonoBehaviourPun
    {
        private PartAssemblyController partAssemblyController;

        private void Start()
        {
            // Cache references
            partAssemblyController = GetComponent<PartAssemblyController>();

            // Subscribe to PartAssemblyController events
            partAssemblyController.OnSetPlacement += OnSetPlacementHandler;
            partAssemblyController.OnResetPlacement += OnResetPlacementHandler;

            // Enable PUN feature
            partAssemblyController.IsPunEnabled = true;
        }

        private void OnSetPlacementHandler()
        {
            photonView.RPC("PunRPC_SetPlacement", RpcTarget.All);
        }

        [PunRPC]
        private void PunRPC_SetPlacement()
        {
            partAssemblyController.Set();
        }

        private void OnResetPlacementHandler()
        {
            photonView.RPC("PunRPC_ResetPlacement", RpcTarget.All);
        }

        [PunRPC]
        private void PunRPC_ResetPlacement()
        {
            partAssemblyController.Reset();
        }
    }
}
