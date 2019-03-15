using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Utilities.Lines.DataProviders;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.SDK.UX.Pointers
{
    public class SpherePointerVisual : MonoBehaviour
    {
        [Tooltip("The pointer these visuals decorate")]
        private SpherePointer pointer;

        [SerializeField]
        [Tooltip("Tether will not be shown unless it is at least this long")]
        private float minTetherLength = 0.03f;

        [SerializeField]
        private Transform visualsRoot = null;

        [SerializeField]
        private Transform tetherEndPoint = null;

        /// Assumption: Tether line is a child of the visuals!
        [SerializeField]
        private BaseMixedRealityLineDataProvider tetherLine = null;


        public void OnValidate()
        {
            CheckInitialization();
        }

        public void OnEnable()
        {
            CheckInitialization();

        }

        public void OnDestroy()
        {
            if (visualsRoot != null)
            {
                Destroy(visualsRoot.gameObject);
            }
        }

        public void Start()
        {
            // put it at root of scene
            visualsRoot.transform.parent = MixedRealityToolkit.Instance.MixedRealityPlayspace;
            visualsRoot.gameObject.name = $"{gameObject.name}_NearTetherVisualsRoot";
        }

        private void CheckInitialization()
        {
            if (pointer == null)
            {
                pointer = GetComponent<SpherePointer>();
            }
            if (pointer == null)
            {
                Debug.LogError($"No SpherePointer found on {gameObject.name}.");
            }

            CheckAsset(visualsRoot, "Visuals Root");
            CheckAsset(tetherEndPoint, "Tether End Point");
            CheckAsset(tetherLine, "Tether Line");
        }

        private void CheckAsset(object asset, string fieldname)
        {
            if (asset == null)
            {
                Debug.LogError($"No {fieldname} specified on {gameObject.name}.SpherePointerVisual. Did you forget to set the {fieldname}?");
            }
        }

        public void Update()
        {
            bool tetherVisualsEnabled = false;
            if (pointer.IsFocusLocked)
            {
                
                Vector3 graspPosition;
                pointer.TryGetNearGraspPoint(out graspPosition);
                tetherLine.FirstPoint = graspPosition;
                Vector3 endPoint = pointer.Result.Details.Object.transform.TransformPoint(pointer.Result.Details.PointLocalSpace);
                tetherLine.LastPoint = endPoint;
                tetherVisualsEnabled = Vector3.Distance(tetherLine.FirstPoint, tetherLine.LastPoint) > minTetherLength;
                tetherLine.enabled = tetherVisualsEnabled;
    
                tetherEndPoint.position = endPoint;

                tetherEndPoint.gameObject.SetActive(tetherVisualsEnabled);
            }

            visualsRoot.gameObject.SetActive(tetherVisualsEnabled);

        }

    }
}