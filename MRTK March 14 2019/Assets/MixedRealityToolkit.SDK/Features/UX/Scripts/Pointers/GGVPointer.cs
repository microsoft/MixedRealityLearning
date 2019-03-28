using UnityEngine;
using System.Collections;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Physics;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Physics;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.TeleportSystem;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Services.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.SDK.UX.Cursors;

namespace Microsoft.MixedReality.Toolkit.SDK.UX.Pointers
{
    public class GGVPointer : InputSystemGlobalListener, IMixedRealityPointer, IMixedRealityInputHandler, IMixedRealityInputHandler<MixedRealityPose>, IMixedRealitySourcePoseHandler, IMixedRealitySourceStateHandler
    {
        [Header("Pointer")]
        [SerializeField]
        private MixedRealityInputAction selectAction = MixedRealityInputAction.None;
        [SerializeField]
        private MixedRealityInputAction poseAction = MixedRealityInputAction.None;


        private GazeProvider gazeProvider;
        private Vector3 sourcePosition;
        private bool isSelectPressed;
        private Handedness lastControllerHandedness;

        #region IMixedRealityPointer
        private IMixedRealityController controller;
        private IMixedRealityInputSource inputSourceParent;


        /// <inheritdoc cref="IMixedRealityController" />
        public IMixedRealityController Controller
        {
            get { return controller; }
            set
            {
                controller = value;

                if (controller != null && this != null)
                {
                    gameObject.name = $"{Controller.ControllerHandedness}_GGVPointer";
                    pointerName = gameObject.name;
                    inputSourceParent = controller.InputSource;
                }
            }
        }

        private uint pointerId;

        /// <inheritdoc />
        public uint PointerId
        {
            get
            {
                if (pointerId == 0)
                {
                    pointerId = MixedRealityToolkit.InputSystem.FocusProvider.GenerateNewPointerId();
                }

                return pointerId;
            }
        }

        private string pointerName = string.Empty;

        /// <inheritdoc />
        public string PointerName
        {
            get { return pointerName; }
            set
            {
                pointerName = value;
                if (this != null)
                {
                    gameObject.name = value;
                }
            }
        }

        public IMixedRealityInputSource InputSourceParent => inputSourceParent;

        public IMixedRealityCursor BaseCursor { get; set; }

        public ICursorModifier CursorModifier { get; set; }

        public IMixedRealityTeleportHotSpot TeleportHotSpot { get; set; }

        public bool IsInteractionEnabled => true;

        /// <inheritdoc />
        public bool IsFocusLocked { get; set; }
        public float PointerExtent
        {
            get
            {
                return MixedRealityToolkit.InputSystem.FocusProvider.GlobalPointingExtent;
            }
            set { throw new System.NotImplementedException(); }
        }

        public RayStep[] Rays { get; protected set; } = { new RayStep(Vector3.zero, Vector3.forward) };


        public LayerMask[] PrioritizedLayerMasksOverride { get; set; }

        public IMixedRealityFocusHandler FocusTarget { get; set; }

        /// <inheritdoc />
        public IPointerResult Result { get; set; }

        /// <inheritdoc />
        public IBaseRayStabilizer RayStabilizer { get; set; }

        /// <inheritdoc />
        public virtual RaycastMode RaycastMode { get; set; } = RaycastMode.Simple;
        public float SphereCastRadius
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }

        public float PointerOrientation
        {
            get
            {
                throw new System.NotImplementedException();
            }
        }


        private static bool Equals(IMixedRealityPointer left, IMixedRealityPointer right)
        {
            return left != null && left.Equals(right);
        }

        /// <inheritdoc />
        bool IEqualityComparer.Equals(object left, object right)
        {
            return left.Equals(right);
        }
        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) { return false; }
            if (ReferenceEquals(this, obj)) { return true; }
            if (obj.GetType() != GetType()) { return false; }

            return Equals((IMixedRealityPointer)obj);
        }

        private bool Equals(IMixedRealityPointer other)
        {
            return other != null && PointerId == other.PointerId && string.Equals(PointerName, other.PointerName);
        }

        /// <inheritdoc />
        int IEqualityComparer.GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;
                hashCode = (hashCode * 397) ^ (int)PointerId;
                hashCode = (hashCode * 397) ^ (PointerName != null ? PointerName.GetHashCode() : 0);
                return hashCode;
            }
        }

        public void OnPostRaycast()
        {

        }

        public void OnPreRaycast()
        {
            Rays[0] = gazeProvider.GazePointer.Rays[0];
        }

        public bool TryGetPointerPosition(out Vector3 position)
        {
            position = sourcePosition;
            return true;
        }

        public bool TryGetPointerRotation(out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            return false;
        }

        public bool TryGetPointingRay(out Ray pointingRay)
        {
            return gazeProvider.GazePointer.TryGetPointingRay(out pointingRay);
        }
        #endregion

        #region IMixedRealityInputHandler Implementation

        /// <inheritdoc />
        public void OnInputUp(InputEventData eventData)
        {
            if (eventData.SourceId == InputSourceParent.SourceId)
            {
                if (eventData.MixedRealityInputAction == selectAction)
                {
                    isSelectPressed = false;
                    if (IsInteractionEnabled)
                    {
                        BaseCursor c = gazeProvider.GazePointer.BaseCursor as BaseCursor;
                        if (c != null)
                        {
                            c.IsPointerDown = false;
                        }
                        MixedRealityToolkit.InputSystem.RaisePointerClicked(this, selectAction, 0, Controller.ControllerHandedness);
                        MixedRealityToolkit.InputSystem.RaisePointerUp(this, selectAction, Controller.ControllerHandedness);
                    }
                }
            }
        }


        /// <inheritdoc />
        public void OnInputDown(InputEventData eventData)
        {
            if (eventData.SourceId == InputSourceParent.SourceId)
            {
                if (eventData.MixedRealityInputAction == selectAction)
                {
                    isSelectPressed = true;
                    lastControllerHandedness = Controller.ControllerHandedness;
                    if (IsInteractionEnabled)
                    {
                        BaseCursor c = gazeProvider.GazePointer.BaseCursor as BaseCursor;
                        if (c != null)
                        {
                            c.IsPointerDown = true;
                        }
                        MixedRealityToolkit.InputSystem.RaisePointerDown(this, selectAction, Controller.ControllerHandedness);
                    }
                }
            }
        }

        #endregion  IMixedRealityInputHandler Implementation

        protected override void Start()
        {
            base.Start();
            this.gazeProvider = MixedRealityToolkit.InputSystem.GazeProvider as GazeProvider;
            BaseCursor c = gazeProvider.GazePointer.BaseCursor as BaseCursor;
            if (c != null)
            {
                c.VisibleSourcesCount++;
            }
        }

        #region IMixedRealitySourcePoseHandler

        public void OnInputPressed(InputEventData<float> eventData)
        {
        }

        public void OnPositionInputChanged(InputEventData<Vector2> eventData)
        {
        }

        public void OnSourcePoseChanged(SourcePoseEventData<TrackingState> eventData)
        {
        }

        public void OnSourcePoseChanged(SourcePoseEventData<Vector2> eventData)
        {
        }

        public void OnSourcePoseChanged(SourcePoseEventData<Vector3> eventData)
        {
        }

        public void OnSourcePoseChanged(SourcePoseEventData<Quaternion> eventData)
        {
        }

        public void OnSourcePoseChanged(SourcePoseEventData<MixedRealityPose> eventData)
        {
        }


        public void OnSourceDetected(SourceStateEventData eventData)
        {
        }

        public void OnSourceLost(SourceStateEventData eventData)
        {
            if (eventData.SourceId == InputSourceParent.SourceId)
            {
                if (isSelectPressed)
                {
                    // Raise OnInputUp if pointer is lost while select is pressed
                    MixedRealityToolkit.InputSystem.RaisePointerUp(this, selectAction, lastControllerHandedness);
                }

                if (gazeProvider != null)
                {
                    BaseCursor c = gazeProvider.GazePointer as BaseCursor;
                    if (c != null)
                    {
                        c.VisibleSourcesCount--;
                    }
                }
                
                // Destroy the pointer since nobody else is destroying us
                if (Application.isEditor)
                {
                    DestroyImmediate(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
            }


        }

        public void OnInputChanged(InputEventData<MixedRealityPose> eventData)
        {
            if (eventData.SourceId == Controller?.InputSource.SourceId &&
                eventData.Handedness == Controller?.ControllerHandedness && 
                eventData.MixedRealityInputAction == poseAction)
            {
                sourcePosition = eventData.InputData.Position;
            }
        }
        #endregion
    }
}
