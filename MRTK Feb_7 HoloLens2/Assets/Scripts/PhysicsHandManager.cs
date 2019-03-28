using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Services;
using System.Collections.Generic;
using UnityEngine;

namespace MRDL.Interaction
{
    public class PhysicsHandManager : MonoBehaviour
    {
        /// <summary>
        /// Kinematic collider to attach to joint transforms 
        /// </summary>
        [SerializeField]
        [Tooltip("Kinematic collider to attach to joint transforms")]
        private GameObject kinematicGO;

        public GameObject KinematicGO
        {
            get => kinematicGO;
            set => kinematicGO = value;
        }

        private Dictionary<TrackedHandJoint, Transform> leftJoints = new Dictionary<TrackedHandJoint, Transform>();
        public IReadOnlyDictionary<TrackedHandJoint, Transform> LeftJoints => leftJoints;

        private Dictionary<TrackedHandJoint, Transform> rightJoints = new Dictionary<TrackedHandJoint, Transform>();
        public IReadOnlyDictionary<TrackedHandJoint, Transform> RightJoints => rightJoints;

        private IMixedRealityHandJointService HandJointService => handJointService ?? (handJointService = MixedRealityToolkit.Instance.GetService<IMixedRealityHandJointService>());
        private IMixedRealityHandJointService handJointService = null;


        private void OnEnable()
        {
            if (kinematicGO == null)
            {
                Debug.LogError("PhysicsHands needs a reference to attach to joint transforms");
                enabled = false;
            }
        }

        private void Start()
        {

            //left joints
            leftJoints.Add(TrackedHandJoint.ThumbTip, HandJointService.RequestJoint(TrackedHandJoint.ThumbTip, Handedness.Left));
            leftJoints.Add(TrackedHandJoint.IndexTip, HandJointService.RequestJoint(TrackedHandJoint.IndexTip, Handedness.Left));
            leftJoints.Add(TrackedHandJoint.MiddleTip, HandJointService.RequestJoint(TrackedHandJoint.MiddleTip, Handedness.Left));
            leftJoints.Add(TrackedHandJoint.RingTip, HandJointService.RequestJoint(TrackedHandJoint.RingTip, Handedness.Left));
            leftJoints.Add(TrackedHandJoint.PinkyTip, HandJointService.RequestJoint(TrackedHandJoint.PinkyTip, Handedness.Left));

            SetUpKinematicObjects(kinematicGO, leftJoints);

            //right joints
            rightJoints.Add(TrackedHandJoint.ThumbTip, HandJointService.RequestJoint(TrackedHandJoint.ThumbTip, Handedness.Right));
            rightJoints.Add(TrackedHandJoint.IndexTip, HandJointService.RequestJoint(TrackedHandJoint.IndexTip, Handedness.Right));
            rightJoints.Add(TrackedHandJoint.MiddleTip, HandJointService.RequestJoint(TrackedHandJoint.MiddleTip, Handedness.Right));
            rightJoints.Add(TrackedHandJoint.RingTip, HandJointService.RequestJoint(TrackedHandJoint.RingTip, Handedness.Right));
            rightJoints.Add(TrackedHandJoint.PinkyTip, HandJointService.RequestJoint(TrackedHandJoint.PinkyTip, Handedness.Right));

            SetUpKinematicObjects(kinematicGO, rightJoints);
        }

        private void Update() { }

        private static void SetUpKinematicObjects(GameObject goToCreate, Dictionary<TrackedHandJoint, Transform> JointDict)
        {
            foreach (Transform t in JointDict.Values)
            {
                GameObject k = Instantiate(goToCreate);
                k.transform.parent = t;
                k.SetActive(true);
            }
        }
    }
}
