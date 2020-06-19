using UnityEngine;

namespace MRTK.Tutorials.GettingStarted
{
    public class PlacementHintsController : MonoBehaviour
    {
        [SerializeField] private bool activeAtStart = true;
        [SerializeField] private GameObject[] placementHints = default;

        private void Start()
        {
            // Set the active state depending on the editor setting
            foreach (var obj in placementHints) obj.SetActive(activeAtStart);
        }

        public void TogglePlacementHints()
        {
            // Toggle each object's active state
            foreach (var obj in placementHints) obj.SetActive(!obj.activeSelf);
        }
    }
}
