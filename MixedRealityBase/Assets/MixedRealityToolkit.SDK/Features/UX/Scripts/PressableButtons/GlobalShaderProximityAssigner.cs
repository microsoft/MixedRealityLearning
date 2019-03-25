using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Devices.Hands;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.SDK.UX.PressableButtons
{
    public class GlobalShaderProximityAssigner : MonoBehaviour
    {
        void Update()
        {
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Left, out MixedRealityPose leftTip))
            {
                Shader.SetGlobalVector("Global_Left_Index_Tip_Position", leftTip.Position);
            }
            else
            {
                //If we don't have this finger, make sure its values are set to a location highly unlikely to be used.
                Shader.SetGlobalVector("Global_Left_Index_Tip_Position", Vector3.one * 1000);
            }
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out MixedRealityPose rightTip))
            {
                Shader.SetGlobalVector("Global_Right_Index_Tip_Position", rightTip.Position);
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