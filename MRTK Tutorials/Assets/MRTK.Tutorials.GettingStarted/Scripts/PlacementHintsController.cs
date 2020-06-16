using UnityEngine;

namespace MRTK.Tutorials.GettingStarted
{
    public class PlacementHintsController : MonoBehaviour
    {
        [SerializeField] private bool activeAtStart = true;
        [SerializeField] private GameObject[] placementHints;

        private void Start()
        {
            // Ensure all objects are active at start
            foreach (var obj in placementHints) obj.SetActive(activeAtStart);
        }

        public void TogglePlacementHints()
        {
            // Toggle each object's active state
            foreach (var obj in placementHints) obj.SetActive(!obj.activeSelf);
        }
    }
}
