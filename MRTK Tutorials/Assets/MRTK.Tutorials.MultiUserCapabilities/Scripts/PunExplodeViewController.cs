using MRTK.Tutorials.GettingStarted;
using Photon.Pun;

namespace MRTK.Tutorials.MultiUserCapabilities
{
    /// <summary>
    ///     Handles PUN RPC for ExplodeViewController.
    /// </summary>
    public class PunExplodeViewController : MonoBehaviourPun
    {
        private ExplodeViewController explodeViewController;

        private void Start()
        {
            // Cache references
            explodeViewController = GetComponent<ExplodeViewController>();

            // Subscribe to ExplodeViewController events
            explodeViewController.OnToggleExplodedView += OnToggleExplodedViewHandler;

            // Enable PUN feature
            explodeViewController.IsPunEnabled = true;
        }

        private void OnToggleExplodedViewHandler()
        {
            photonView.RPC("PunRPC_ToggleExplodedView", RpcTarget.All);
        }

        [PunRPC]
        private void PunRPC_ToggleExplodedView()
        {
            explodeViewController.Toggle();
        }
    }
}
