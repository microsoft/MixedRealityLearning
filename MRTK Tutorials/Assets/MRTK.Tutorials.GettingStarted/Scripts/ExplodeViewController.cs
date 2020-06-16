using System.Collections.Generic;
using UnityEngine;

namespace MRTK.Tutorials.GettingStarted
{
    public class ExplodeViewController : MonoBehaviour
    {
        private readonly List<Vector3> explodedPos = new List<Vector3>();
        private readonly List<Vector3> startingPos = new List<Vector3>();
        [SerializeField] private List<GameObject> defaultPositions;
        [SerializeField] private List<GameObject> explodedPositions;
        private bool isInDefaultPosition;
        [SerializeField] private float speed = 0.1f;

        private void Start()
        {
            // Capture the starting position and exploded position of the objects
            foreach (var item in defaultPositions) startingPos.Add(item.transform.localPosition);
            foreach (var item in explodedPositions) explodedPos.Add(item.transform.localPosition);
        }

        private void Update()
        {
            // Reverse position based on the position we are currently in
            if (isInDefaultPosition)
                // Move objects to exploded position
                for (var i = 0; i < defaultPositions.Count; i++)
                    defaultPositions[i].transform.localPosition = Vector3.Lerp(
                        defaultPositions[i].transform.localPosition,
                        explodedPos[i], speed);
            else
                // Move objects to default position
                for (var i = 0; i < defaultPositions.Count; i++)
                    defaultPositions[i].transform.localPosition = Vector3.Lerp(
                        defaultPositions[i].transform.localPosition,
                        startingPos[i], speed);
        }

        public void ToggleExplodedView()
        {
            isInDefaultPosition = !isInDefaultPosition;
        }
    }
}
