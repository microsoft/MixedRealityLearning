using UnityEngine;

using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Devices.Hands;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Utilities;

namespace Microsoft.MixedReality.Toolkit.SDK.UX.PressableButtons
{
    public class GlobalShaderProximityAssigner : MonoBehaviour
    {
        Transform leftTip;
        Transform rightTip;

        private IMixedRealityHandJointService HandJointService => handJointService ?? (handJointService = MixedRealityToolkit.Instance.GetService<IMixedRealityHandJointService>());
        private IMixedRealityHandJointService handJointService = null;


        void Start()
        {
            leftTip = HandJointService.RequestJoint(TrackedHandJoint.IndexTip, Handedness.Left);
            rightTip = HandJointService.RequestJoint(TrackedHandJoint.IndexTip, Handedness.Right);
        }
        void Update()
        {
            if (leftTip != null)
            {
                Shader.SetGlobalVector("Global_Left_Index_Tip_Position", leftTip.position);
            }
            else
            {
                //If we don't have this finger, make sure its values are set to a location highly unlikely to be used.
                Shader.SetGlobalVector("Global_Left_Index_Tip_Position", Vector3.one * 1000);
            }
            if (rightTip != null)
            {
                Shader.SetGlobalVector("Global_Right_Index_Tip_Position", rightTip.position);
            }
            else
            {
                //If we don't have this finger, make sure its values are set to a location highly unlikely to be used.
                Shader.SetGlobalVector("Global_Right_Index_Tip_Position", Vector3.one * 1000);
            }

            //Need to control head cursor blob position

            //Need to control the far select positionining
        }
    }
}