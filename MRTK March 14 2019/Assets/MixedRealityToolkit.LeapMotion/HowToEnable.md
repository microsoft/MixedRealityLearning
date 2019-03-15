# Leap Motion MRTK Integration

![](/External/ReadMeImages/MRTK_Logo_Rev.png)

To enable Leap Motion hand tracking in an MRTK scene the following modifications must be made to the *Mixed Reality Toolkit Profile*:

* Customize the **Registered Service Providers Profile** and add `Microsoft.MixedReality.Toolkit.LeapMotion.Devices.Hands.LeapMotionDeviceManager`.