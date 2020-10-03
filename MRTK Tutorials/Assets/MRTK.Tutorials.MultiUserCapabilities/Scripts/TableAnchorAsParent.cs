using UnityEngine;

namespace MRTK.Tutorials.MultiUserCapabilities
{
    public class TableAnchorAsParent : MonoBehaviour
    {
        private void Start()
        {
            if (TableAnchor.Instance != null) transform.parent = TableAnchor.Instance.transform;
        }
    }
}
