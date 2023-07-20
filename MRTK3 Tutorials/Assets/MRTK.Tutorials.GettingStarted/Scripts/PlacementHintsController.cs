using UnityEngine;

namespace MRTK.Tutorials.GettingStarted
{
    public class PlacementHintsController : MonoBehaviour
    {
        public delegate void PlacementHintsControllerDelegate();

        [SerializeField] private bool activeAtStart = true;
        [SerializeField] private GameObject[] placementHints = default;

        private bool isPunEnabled;

        public bool IsPunEnabled
        {
            set => isPunEnabled = value;
        }

        private void Start()
        {
            // Set the active state depending on the editor setting
            foreach (var obj in placementHints) obj.SetActive(activeAtStart);
        }

        /// <summary>
        /// Triggers the placement hints feature.
        /// Hooked up in Unity.
        /// </summary>
        public void TogglePlacementHints()
        {
            if (isPunEnabled)
                OnTogglePlacementHints?.Invoke();
            else
                Toggle();
        }

        /// <summary>
        /// Toggles the placement hints' active state.
        /// </summary>
        public void Toggle()
        {
            // Toggle each object's active state
            foreach (var obj in placementHints) obj.SetActive(!obj.activeSelf);
        }

        /// <summary>
        ///     Raised when TogglePlacementHints is called and PUN is enabled.
        /// </summary>
        public event PlacementHintsControllerDelegate OnTogglePlacementHints;
    }
}
