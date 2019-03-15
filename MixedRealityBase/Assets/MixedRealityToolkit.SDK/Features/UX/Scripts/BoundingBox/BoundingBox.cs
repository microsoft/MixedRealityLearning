// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.Core.Devices.Hands;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Microsoft.MixedReality.Toolkit.SDK.Input.Handlers;
using Microsoft.MixedReality.Toolkit.Services.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Extensions;

namespace Microsoft.MixedReality.Toolkit.SDK.UX
{
    public class BoundingBox : BaseFocusHandler, IMixedRealityPointerHandler, IMixedRealitySourceStateHandler, IMixedRealityFocusChangedHandler, IMixedRealityFocusHandler
    {
        #region Enums
        private enum FlattenModeType
        {
            DoNotFlatten = 0,
            /// <summary>
            /// Flatten the X axis
            /// </summary>
            FlattenX,
            /// <summary>
            /// Flatten the Y axis
            /// </summary>
            FlattenY,
            /// <summary>
            /// Flatten the Z axis
            /// </summary>
            FlattenZ,
            /// <summary>
            /// Flatten the smallest relative axis if it falls below threshold
            /// </summary>
            FlattenAuto,
        }
        private enum HandleType
        {
            none = 0,
            rotation,
            scale
        }
        private enum WireframeType
        {
            Cubic = 0,
            Cylindrical
        }
        private enum CardinalAxisType
        {
            X = 0,
            Y,
            Z
        }
        private enum BoundsCalculationMethod
        {
            Collider = 0,
            Colliders,
            Renderers,
            MeshFilters
        }
        private enum BoundingBoxActivationType
        {
            ActivateOnStart = 0,
            ActivateByProximity,
            ActivateByPointer,
            ActivateByProximityAndPointer,
            ActivateManually
        }
        #endregion Enums

        #region Serialized Fields
        [SerializeField]
        [Tooltip("The object that the bounding box rig will be modifying.")]
        private GameObject targetObject;

        [Tooltip("For complex objects, automatic bounds calculation may not behave as expected. Use an existing Box Collider (even on a child object) to manually determine bounds of Bounding Box.")]
        [SerializeField]
        [FormerlySerializedAs("BoxColliderToUse")]
        private BoxCollider boundsOverride = null;

        [Header("Behavior")]
        [SerializeField]
        private BoundingBoxActivationType activation = BoundingBoxActivationType.ActivateManually;

        [SerializeField]
        [Tooltip("Maximum scaling allowed relative to the initial size")]
        private float scaleMaximum = 2.0f;
        [SerializeField]
        [Tooltip("Minimum scaling allowed relative to the initial size")]
        private float scaleMinimum = 0.2f;

        [Header("Box Display")]
        [SerializeField]
        [Tooltip("Flatten bounds in the specified axis or flatten the smallest one if 'auto' is selected")]
        private FlattenModeType flattenAxis = FlattenModeType.DoNotFlatten;
        [SerializeField]
        [FormerlySerializedAs("wireframePadding")]
        [Tooltip("Extra padding added to the actual Target bounds")]
        private Vector3 boxPadding = Vector3.zero;        
        [SerializeField]
        [Tooltip("Material used to display the bounding box. If set to null no bounding box will be displayed")]
        private Material boxMaterial = null;
        [SerializeField]
		[Tooltip("Material used to display the bounding box when grabbed. If set to null no change will occur when grabbed.")]
        private Material boxGrabbedMaterial = null;
        [SerializeField]
        [Tooltip("Show a wireframe around the bounding box when checked. Wireframe parameters below have no effect unless this is checked")]
        private bool showWireframe = true;
        [SerializeField]
        [Tooltip("Shape used for wireframe display")]
        private WireframeType wireframeShape = WireframeType.Cubic;
        [SerializeField]
        [Tooltip("Material used for wireframe display")]
        private Material wireframeMaterial;
        [SerializeField]
        [FormerlySerializedAs("linkRadius")]
        [Tooltip("Radius for wireframe edges")]
        private float wireframeEdgeRadius = 0.005f;

        [Header("Handles")]
        [SerializeField]
        [Tooltip("Material applied to handles when they are not in a grabbed state")]
        private Material handleMaterial;
        [SerializeField]
        [Tooltip("Material applied to handles while they are a grabbed")]
        private Material handleGrabbedMaterial;
        [SerializeField]
        [Tooltip("Prefab used to display scale handles in corners. If not set, boxes will be displayed instead")]
        GameObject scaleHandlePrefab = null;
        [SerializeField]
        [FormerlySerializedAs("cornerRadius")]
        [Tooltip("Size of the cube collidable used in scale handles")]
        private float scaleHandleSize = 0.03f;
        [SerializeField]
        [Tooltip("Prefab used to display rotation handles in the midpoint of each edge. If not set, spheres will be displayed instead")]
        GameObject rotationHandlePrefab = null;
        [SerializeField]
        [FormerlySerializedAs("ballRadius")]
        [Tooltip("Radius of the sphere collidable used in rotation handles")]
        private float rotationHandleDiameter = 0.035f;

        [SerializeField]
        [Tooltip("Check to show scale handles")]
        private bool showScaleHandles = true;
        public bool ShowScaleHandles
        {
            get
            {
                return showScaleHandles;
            }
            set
            {
                if (showScaleHandles != value)
                {
                    showScaleHandles = value;
                    ResetHandleVisibility();
                }
            }
        }

        [SerializeField]
        [Tooltip("Check to show rotation handles for the X axis")]
        private bool showRotationHandleForX = true;
        public bool ShowRotationHandleForX
        {
            get
            {
                return showRotationHandleForX;
            }
            set
            {
                if (showRotationHandleForX != value)
                {
                    showRotationHandleForX = value;
                    ResetHandleVisibility();
                }
            }
        }

        [SerializeField]
        [Tooltip("Check to show rotation handles for the Y axis")]
        private bool showRotationHandleForY = true;
        public bool ShowRotationHandleForY
        {
            get
            {
                return showRotationHandleForY;
            }
            set
            {
                if (showRotationHandleForY != value)
                {
                    showRotationHandleForY = value;
                    ResetHandleVisibility();
                }
            }
        }

        [SerializeField]
        [Tooltip("Check to show rotation handles for the Z axis")]
        private bool showRotationHandleForZ = true;
        public bool ShowRotationHandleForZ
        {
            get
            {
                return showRotationHandleForZ;
            }
            set
            {
                if (showRotationHandleForZ != value)
                {
                    showRotationHandleForZ = value;
                    ResetHandleVisibility();
                }
            }
        }

        [Header("Debug")]
        [Tooltip("Debug only. Component used to display debug messages")]
        public TextMesh debugText;

        [Header("Events")]
        public UnityEvent RotateStarted;
        public UnityEvent RotateStopped;
        public UnityEvent ScaleStarted;
        public UnityEvent ScaleStopped;
        #endregion Serialized Fields

        #region Constants
        private const int LTB = 0;
        private const int LTF = 1;
        private const int LBF = 2;
        private const int LBB = 3;
        private const int RTB = 4;
        private const int RTF = 5;
        private const int RBF = 6;
        private const int RBB = 7;
        private const int cornerCount = 8;
        #endregion Constants

        #region Private Properties

        // Whether we should be displaying just the wireframe (if enabled) or the handles too
        private bool wireframeOnly = false;

        private Vector3 grabStartPoint;
        private IMixedRealityPointer currentPointer;
        private IMixedRealityInputSource currentInputSource;
        private IMixedRealityController currentController;
        private uint currentSourceId;
        private GameObject rigRoot;

        // Game object used to display the bounding box. Parented to the rig root
        private GameObject boxDisplay;

        private BoxCollider cachedTargetCollider;
        private Vector3[] boundsCorners;

        // Half the size of the current bounds
        private Vector3 currentBoundsExtents;

        private BoundsCalculationMethod boundsMethod;
        private bool hideElementsInInspector = true;

        private List<IMixedRealityInputSource> touchingSources = new List<IMixedRealityInputSource>();
        private List<GameObject> links;
        private List<GameObject> corners;
        private List<GameObject> balls;
        private List<Renderer> linkRenderers;
        private List<IMixedRealityController> sourcesDetected;
        private Vector3[] edgeCenters;

        private Ray initialGrabRay;
        private Ray currentGrabRay;
        private Vector3 currentRotationAxis;

        // Scale of the target at the beginning of the current manipulation
        private Vector3 initialScale;

        // Position of the target at the beginning of the current manipulation
        private Vector3 initialPosition;

        private Vector3 maximumScale;
        private Vector3 minimumScale;
        private Vector3 initialGrabbedPosition;
        private Vector3 initialRigCentroid;
        private Vector3 initialGrabPoint;
        private CardinalAxisType[] edgeAxes;
        private int[] flattenedHandles;
        private GameObject grabbedHandle;

        // Corner opposite to the grabbed one. Scaling will be relative to it.
        private Vector3 oppositeCorner;

        // Direction of the diagonal from the opposite corner to the grabbed one.
        private Vector3 diagonalDir;

        private HandleType currentHandleType;
        private Vector3 lastBounds;

        private Vector3 rayOffset;
        private bool farInteracting = false;

        // TODO Review this, it feels like we should be using Behaviour.enabled instead.
        private bool active = false;
        public bool Active
        {
            get
            {
                return active;
            }
            set
            {
                if (active != value)
                {
                    active = value;
                    rigRoot?.SetActive(value);
                    ResetHandleVisibility();
                }
            }
        }

        public GameObject Target
        {
            get
            {
                if (targetObject == null)
                {
                    targetObject = gameObject;
                }

                return targetObject;
            }
        }

        #endregion Private Properties

        #region Monobehaviour Methods
        private void Start()
        {
            CreateRig();

            if (activation == BoundingBoxActivationType.ActivateByProximityAndPointer ||
                activation == BoundingBoxActivationType.ActivateByProximity ||
                activation == BoundingBoxActivationType.ActivateByPointer)
            {
                wireframeOnly = true;
                Active = true;
            }
            else if (activation == BoundingBoxActivationType.ActivateOnStart)
            {
                Active = true;
            }
        }
        private void Update()
        {
            if (currentController == null)
            {
               UpdateBounds();
               //UpdateProximity();
            }
            else
            {
                TransformRig();

                // Update bounds after transforming so the displayed bounds are correct
                UpdateBounds();
            }

            UpdateRigHandles();
        }
        #endregion Monobehaviour Methods

        #region Private Methods
        private void CreateRig()
        {
            DestroyRig();
            SetMaterials();
            InitializeDataStructures();
            CaptureInitialState();
            SetBoundingBoxCollider();
            UpdateBounds();
            AddCorners();
            AddLinks();
            AddBoxDisplay();
            UpdateRigHandles();
            Flatten();
            ResetHandleVisibility();

            rigRoot.SetActive(active);
        }
        private void DestroyRig()
        {
            if (boundsOverride == null)
            {
                Destroy(cachedTargetCollider);
            }
            else
            {
                boundsOverride.size -= boxPadding;

                if (cachedTargetCollider != null)
                {
                    if (cachedTargetCollider.gameObject.GetComponent<NearInteractionGrabbable>())
                    {
                        Destroy(cachedTargetCollider.gameObject.GetComponent<NearInteractionGrabbable>());
                    }
                }
            }

            if (balls != null)
            {
                foreach (GameObject gameObject in balls)
                {
                    Object.Destroy(gameObject);
                }
                balls.Clear();
            }

            if (links != null)
            {
                foreach (GameObject gameObject in links)
                {
                    Object.Destroy(gameObject);
                }
                links.Clear();
            }

            if (corners != null)
            {
                foreach (GameObject gameObject in corners)
                {
                    Object.Destroy(gameObject);
                }
                corners.Clear();
            }

            if (rigRoot != null)
            {
                Object.Destroy(rigRoot);
                rigRoot = null;
            }

        }
        private void TransformRig()
        {
            if (currentHandleType != HandleType.none)
            {
                Vector3 newGrabbedPosition = GetCurrentGrabPoint();

                if (currentHandleType == HandleType.rotation)
                {
                    Vector3 projPt = Vector3.ProjectOnPlane((newGrabbedPosition - rigRoot.transform.position).normalized, currentRotationAxis);
                    Quaternion q = Quaternion.FromToRotation((grabbedHandle.transform.position - rigRoot.transform.position).normalized, projPt.normalized);
                    Vector3 axis;
                    float angle;
                    q.ToAngleAxis(out angle, out axis);
                    Target.transform.RotateAround(rigRoot.transform.position, axis, angle);
                }
                else if (currentHandleType == HandleType.scale)
                {
                    float initialDist = Vector3.Dot(initialGrabbedPosition - oppositeCorner, diagonalDir);
                    float currentDist = Vector3.Dot(newGrabbedPosition - oppositeCorner, diagonalDir);
                    float scaleFactor = 1 + (currentDist - initialDist) / initialDist;

                    Vector3 newScale = initialScale * scaleFactor;
                    Vector3 clampedScale = ClampScale(newScale);
                    if (clampedScale != newScale)
                    {
                        scaleFactor = clampedScale[0] / initialScale[0];
                    }
                        
                    Target.transform.localScale = clampedScale;
                    Target.transform.position = initialPosition * scaleFactor + (1 - scaleFactor) * oppositeCorner;
                }
            }
        }
        private Vector3 GetCurrentGrabPoint()
        {
            Vector3 newGrabbedPosition = Vector3.zero;
            if (true == GetHandGrabPoint(currentPointer, out newGrabbedPosition))
            {
                if (farInteracting == true)
                {
                    newGrabbedPosition += rayOffset;
                }
            }

            return newGrabbedPosition;
        }
        private Vector3 GetRotationAxis(GameObject handle)
        {
            for (int i = 0; i < balls.Count; ++i)
            {
                if (handle == balls[i])
                {
                    if (edgeAxes[i] == CardinalAxisType.X)
                    {
                        return rigRoot.transform.right;
                    }
                    else if (edgeAxes[i] == CardinalAxisType.Y)
                    {
                        return rigRoot.transform.up;
                    }
                    else
                    {
                        return rigRoot.transform.forward;
                    }
                }
            }

            return Vector3.zero;
        }
        private void AddCorners()
        {
            if (scaleHandlePrefab == null)
            {
                for (int i = 0; i < boundsCorners.Length; ++i)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = "corner_" + i.ToString();
                    if (hideElementsInInspector == true)
                    {
                        cube.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    }
                    cube.transform.localScale = new Vector3(scaleHandleSize, scaleHandleSize, scaleHandleSize);
                    cube.transform.position = boundsCorners[i];

                    // In order for the cube to be grabbed using near interaction we need
                    // to add NearInteractionGrabbable;
                    cube.EnsureComponent<NearInteractionGrabbable>();

                    cube.transform.parent = rigRoot.transform;

                    Renderer renderer = cube.GetComponent<Renderer>();

                    BoxCollider collider = cube.GetComponent<BoxCollider>();
                    collider.size *= 1.35f;
                    corners.Add(cube);

                    if (handleMaterial != null)
                    {
                        renderer.material = handleMaterial;
                    }
                }
            }
            else
            {
                for (int i = 0; i < boundsCorners.Length; ++i)
                {
                    GameObject corner = new GameObject();
                    corner.name = "corner_" + i.ToString();
                    corner.transform.parent = rigRoot.transform;
                    corner.transform.localPosition = boundsCorners[i];

                    BoxCollider collider = corner.AddComponent<BoxCollider>();
                    collider.size = scaleHandleSize * Vector3.one;

                    // In order for the corner to be grabbed using near interaction we need
                    // to add NearInteractionGrabbable;
                    corner.EnsureComponent<NearInteractionGrabbable>();

                    GameObject visualsScale = new GameObject();
                    visualsScale.name = "visualsScale";
                    visualsScale.transform.parent = corner.transform;
                    visualsScale.transform.localPosition = Vector3.zero;

                    // Compute mirroring scale
                    {
                        Vector3 p = boundsCorners[i];
                        visualsScale.transform.localScale = new Vector3(Mathf.Sign(p[0]), Mathf.Sign(p[1]), Mathf.Sign(p[2]));
                    }

                    GameObject cornerVisuals = Instantiate(scaleHandlePrefab, visualsScale.transform);
                    cornerVisuals.name = "visuals";

                    ApplyMaterialToAllRenderers(cornerVisuals, handleMaterial);

                    if (hideElementsInInspector == true)
                    {
                        corner.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    }

                    corners.Add(corner);
                }
            }
        }

        private void AddLinks()
        {
            edgeCenters = new Vector3[12];

            CalculateEdgeCenters();

            edgeAxes = new CardinalAxisType[12];
            edgeAxes[0] = CardinalAxisType.X;
            edgeAxes[1] = CardinalAxisType.Y;
            edgeAxes[2] = CardinalAxisType.X;
            edgeAxes[3] = CardinalAxisType.Y;
            edgeAxes[4] = CardinalAxisType.X;
            edgeAxes[5] = CardinalAxisType.Y;
            edgeAxes[6] = CardinalAxisType.X;
            edgeAxes[7] = CardinalAxisType.Y;
            edgeAxes[8] = CardinalAxisType.Z;
            edgeAxes[9] = CardinalAxisType.Z;
            edgeAxes[10] = CardinalAxisType.Z;
            edgeAxes[11] = CardinalAxisType.Z;

            if (rotationHandlePrefab == null)
            {
                for (int i = 0; i < edgeCenters.Length; ++i)
                {
                    GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    ball.name = "midpoint_" + i.ToString();
                    if (hideElementsInInspector == true)
                    {
                        ball.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    }

                    ball.transform.localScale = new Vector3(rotationHandleDiameter, rotationHandleDiameter, rotationHandleDiameter);
                    ball.transform.position = edgeCenters[i];
                    ball.transform.parent = rigRoot.transform;

                    // In order for the ball to be grabbed using near interaction we need
                    // to add NearInteractionGrabbable;
                    ball.EnsureComponent<NearInteractionGrabbable>();

                    SphereCollider collider = ball.GetComponent<SphereCollider>();
                    collider.radius *= 1.2f;
                    balls.Add(ball);

                    if (handleMaterial != null)
                    {
                        Renderer renderer = ball.GetComponent<Renderer>();
                        renderer.material = handleMaterial;
                    }
                }
            }
            else
            {
                for (int i = 0; i < edgeCenters.Length; ++i)
                {
                    GameObject ball = Instantiate(rotationHandlePrefab, rigRoot.transform);
                    ball.name = "midpoint_" + i.ToString();
                    ball.transform.localPosition = edgeCenters[i];

                    // Align handle with its edge assuming that the prefab is initially aligned with the up direction
                    if (edgeAxes[i] == CardinalAxisType.X)
                    {
                        Quaternion realignment = Quaternion.FromToRotation(Vector3.up, Vector3.right);
                        ball.transform.localRotation = realignment * ball.transform.localRotation;
                    }
                    else if (edgeAxes[i] == CardinalAxisType.Z)
                    {
                        Quaternion realignment = Quaternion.FromToRotation(Vector3.up, Vector3.forward);
                        ball.transform.localRotation = realignment * ball.transform.localRotation;
                    }

                    SphereCollider collider = ball.AddComponent<SphereCollider>();
                    collider.radius = 0.5f * rotationHandleDiameter;

                    // In order for the ball to be grabbed using near interaction we need
                    // to add NearInteractionGrabbable;
                    ball.EnsureComponent<NearInteractionGrabbable>();

                    ApplyMaterialToAllRenderers(ball, handleMaterial);

                    if (hideElementsInInspector == true)
                    {
                        ball.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    }

                    balls.Add(ball);
                }
            }

            if (links != null)
            {
                GameObject link;
                for (int i = 0; i < edgeCenters.Length; ++i)
                {
                    if (wireframeShape == WireframeType.Cubic)
                    {
                        link = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    	Destroy(link.GetComponent<BoxCollider>());
                    }
                    else
                    {
                        link = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
	                    Destroy(link.GetComponent<CapsuleCollider>());
                    }
                    link.name = "link_" + i.ToString();
                    if (hideElementsInInspector == true)
                    {
                        link.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    }

                    Vector3 linkDimensions = GetLinkDimensions();
                    if (edgeAxes[i] == CardinalAxisType.Y)
                    {
                        link.transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.y, wireframeEdgeRadius);
                        link.transform.Rotate(new Vector3(0.0f, 90.0f, 0.0f));
                    }
                    else if (edgeAxes[i] == CardinalAxisType.Z)
                    {
                        link.transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.z, wireframeEdgeRadius);
                        link.transform.Rotate(new Vector3(90.0f, 0.0f, 0.0f));
                    }
                    else//X
                    {
                        link.transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.x, wireframeEdgeRadius);
                        link.transform.Rotate(new Vector3(0.0f, 0.0f, 90.0f));
                    }

                    link.transform.position = edgeCenters[i];
                    link.transform.parent = rigRoot.transform;
                    Renderer linkRenderer = link.GetComponent<Renderer>();
                    linkRenderers.Add(linkRenderer);

                    if (wireframeMaterial != null)
                    {
                        linkRenderer.material = wireframeMaterial;
                    }

                    links.Add(link);
                }
            }
        }

        private void AddBoxDisplay()
        {
            if (boxMaterial != null)
            {
                boxDisplay = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(boxDisplay.GetComponent<BoxCollider>());
                boxDisplay.name = "bounding box";

                ApplyMaterialToAllRenderers(boxDisplay, boxMaterial);

                boxDisplay.transform.localScale = 2.0f * currentBoundsExtents;
                boxDisplay.transform.parent = rigRoot.transform;
                
                if (hideElementsInInspector == true)
                {
                    boxDisplay.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                }
            }
        }

        private void SetBoundingBoxCollider()
        {
            //Collider.bounds is world space bounding volume.
            //Mesh.bounds is local space bounding volume
            //Renderer.bounds is the same as mesh.bounds but in world space coords

            if (boundsOverride != null)
            {
                cachedTargetCollider = boundsOverride;
            }
            else
            {
                Bounds bounds = GetTargetBounds();
                cachedTargetCollider = Target.AddComponent<BoxCollider>();
                if (boundsMethod == BoundsCalculationMethod.Renderers)
                {
                    cachedTargetCollider.center = bounds.center;
                    cachedTargetCollider.size = bounds.size;
                }
                else if (boundsMethod == BoundsCalculationMethod.Colliders)
                {
                    cachedTargetCollider.center = bounds.center;
                    cachedTargetCollider.size = bounds.size;
                }
            }

            Vector3 scale = cachedTargetCollider.transform.lossyScale;
            Vector3 invScale = new Vector3(1.0f / scale[0], 1.0f / scale[1], 1.0f / scale[2]);
            cachedTargetCollider.size += Vector3.Scale(boxPadding, invScale);

            cachedTargetCollider.EnsureComponent<NearInteractionGrabbable>();
        }

        private Bounds GetTargetBounds()
        {
            Bounds bounds = new Bounds();

            if (Target.transform.childCount == 0)
            {
                bounds = GetSingleObjectBounds(Target);
                boundsMethod = BoundsCalculationMethod.Collider;
                return bounds;
            }
            else
            {
                for (int i = 0; i < Target.transform.childCount; ++i)
                {
                    if (bounds.size == Vector3.zero)
                    {
                        bounds = GetSingleObjectBounds(Target.transform.GetChild(i).gameObject);
                    }
                    else
                    {
                        Bounds childBounds = GetSingleObjectBounds(Target.transform.GetChild(i).gameObject);
                        if (childBounds.size != Vector3.zero)
                        {
                            bounds.Encapsulate(childBounds);
                        }
                    }
                }

                if (bounds.size != Vector3.zero)
                {
                    boundsMethod = BoundsCalculationMethod.Colliders;
                    return bounds;
                }
            }

            //simple case: sum of existing colliders
            Collider[] colliders = Target.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                //Collider.bounds is in world space.
                bounds = colliders[0].bounds;
                for (int i = 0; i < colliders.Length; ++i)
                {
                    if (colliders[i].bounds.size != Vector3.zero)
                    {
                        bounds.Encapsulate(colliders[i].bounds);
                    }
                }
                if (bounds.size != Vector3.zero)
                {
                    boundsMethod = BoundsCalculationMethod.Colliders;
                    return bounds;
                }
            }

            //Renderer bounds is local. Requires transform to global coord system.
            Renderer[] childRenderers = Target.GetComponentsInChildren<Renderer>();
            if (childRenderers.Length > 0)
            {
                bounds = new Bounds();
                bounds = childRenderers[0].bounds;
                Vector3[] corners = new Vector3[cornerCount];
                for (int i = 0; i < childRenderers.Length; ++i)
                {
                    bounds.Encapsulate(childRenderers[i].bounds);
                }

                GetCornerPositionsFromBounds(bounds,  ref boundsCorners);
                for (int c = 0; c < corners.Length; ++c)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = c.ToString();
                    cube.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                    cube.transform.position = boundsCorners[c];
                }

                boundsMethod = BoundsCalculationMethod.Renderers;
                return bounds;
            }

            MeshFilter[] meshFilters = Target.GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length > 0)
            {
                //Mesh.bounds is local space bounding volume
                bounds.size = meshFilters[0].mesh.bounds.size;
                bounds.center = meshFilters[0].mesh.bounds.center;
                for (int i = 0; i < meshFilters.Length; ++i)
                {
                    bounds.Encapsulate(meshFilters[i].mesh.bounds);
                }
                if (bounds.size != Vector3.zero)
                {
                    bounds.center = Target.transform.position;
                    boundsMethod = BoundsCalculationMethod.MeshFilters;
                    return bounds;
                }
            }

            BoxCollider boxCollider = Target.AddComponent<BoxCollider>();
            bounds = boxCollider.bounds;
            Destroy(boxCollider);
            boundsMethod = BoundsCalculationMethod.Collider;
            return bounds;
        }
        private Bounds GetSingleObjectBounds(GameObject gameObject)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            Component[] components = gameObject.GetComponents<Component>();
            if (components.Length < 3)
            {
                return bounds;
            }
            BoxCollider boxCollider;
            boxCollider = gameObject.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
                bounds = boxCollider.bounds;
                DestroyImmediate(boxCollider);
            }
            else
            {
                bounds = boxCollider.bounds;
            }

            return bounds;
        }
        private void SetMaterials()
        {
            //ensure materials
            if (wireframeMaterial == null)
            {
                float[] color = { 1.0f, 1.0f, 1.0f, 0.75f };

                Shader shader = Shader.Find("Mixed Reality Toolkit/Standard");

                wireframeMaterial = new Material(shader);
                wireframeMaterial.EnableKeyword("_InnerGlow");
                wireframeMaterial.SetColor("_Color", new Color(0.0f, 0.63f, 1.0f));
                wireframeMaterial.SetFloat("_InnerGlow", 1.0f);
                wireframeMaterial.SetFloatArray("_InnerGlowColor", color);
            }
            if (handleMaterial == null && handleMaterial != wireframeMaterial)
            {
                float[] color = { 1.0f, 1.0f, 1.0f, 0.75f };

                Shader shader = Shader.Find("Mixed Reality Toolkit/Standard");

                handleMaterial = new Material(shader);
                handleMaterial.EnableKeyword("_InnerGlow");
                handleMaterial.SetColor("_Color", new Color(0.0f, 0.63f, 1.0f));
                handleMaterial.SetFloat("_InnerGlow", 1.0f);
                handleMaterial.SetFloatArray("_InnerGlowColor", color);
            }
            if (handleGrabbedMaterial == null && handleGrabbedMaterial != handleMaterial && handleGrabbedMaterial != wireframeMaterial)
            {
                float[] color = { 1.0f, 1.0f, 1.0f, 0.75f };

                Shader shader = Shader.Find("Mixed Reality Toolkit/Standard");

                handleGrabbedMaterial = new Material(shader);
                handleGrabbedMaterial.EnableKeyword("_InnerGlow");
                handleGrabbedMaterial.SetColor("_Color", new Color(0.0f, 0.63f, 1.0f));
                handleGrabbedMaterial.SetFloat("_InnerGlow", 1.0f);
                handleGrabbedMaterial.SetFloatArray("_InnerGlowColor", color);
            }
        }
        private void InitializeDataStructures()
        {
            rigRoot = new GameObject("rigRoot");
            rigRoot.transform.parent = transform;
            if (hideElementsInInspector == true)
            {
                rigRoot.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            }

            boundsCorners = new Vector3[8];

            corners = new List<GameObject>();
            balls = new List<GameObject>();

            if (showWireframe)
            {
                links = new List<GameObject>();
                linkRenderers = new List<Renderer>();
            }
            
            sourcesDetected = new List<IMixedRealityController>();
        }
        private void CalculateEdgeCenters()
        {
            if (boundsCorners != null && edgeCenters != null)
            {
                edgeCenters[0] = (boundsCorners[0] + boundsCorners[1]) * 0.5f;
                edgeCenters[1] = (boundsCorners[1] + boundsCorners[2]) * 0.5f;
                edgeCenters[2] = (boundsCorners[2] + boundsCorners[3]) * 0.5f;
                edgeCenters[3] = (boundsCorners[3] + boundsCorners[0]) * 0.5f;

                edgeCenters[4] = (boundsCorners[4] + boundsCorners[5]) * 0.5f;
                edgeCenters[5] = (boundsCorners[5] + boundsCorners[6]) * 0.5f;
                edgeCenters[6] = (boundsCorners[6] + boundsCorners[7]) * 0.5f;
                edgeCenters[7] = (boundsCorners[7] + boundsCorners[4]) * 0.5f;

                edgeCenters[8] = (boundsCorners[0] + boundsCorners[4]) * 0.5f;
                edgeCenters[9] = (boundsCorners[1] + boundsCorners[5]) * 0.5f;
                edgeCenters[10] = (boundsCorners[2] + boundsCorners[6]) * 0.5f;
                edgeCenters[11] = (boundsCorners[3] + boundsCorners[7]) * 0.5f;
            }
        }
        private void CaptureInitialState()
        {
            var target = Target;
            if(target != null)
            {
                maximumScale = Target.transform.localScale * scaleMaximum;
                minimumScale = Target.transform.localScale * scaleMinimum;
            }
        }

        private Vector3 ClampScale(Vector3 scale)
        {
            if (Vector3.Min(maximumScale, scale) != scale)
            {
                float maxRatio = 0.0f;
                int maxIdx = -1;

                // Find out the component with the maximum ratio to its maximum allowed value
                for (int i = 0; i < 3; ++i)
                {
                    if (maximumScale[i] > 0)
                    {
                        float ratio = scale[i] / maximumScale[i];
                        if (ratio > maxRatio)
                        {
                            maxRatio = ratio;
                            maxIdx = i;
                        }
                    }
                }

                if (maxIdx != -1)
                {
                    scale /= maxRatio;
                }
            }

            if (Vector3.Max(minimumScale, scale) != scale)
            {
                float minRatio = 1.0f;
                int minIdx = -1;

                // Find out the component with the minimum ratio to its minimum allowed value
                for (int i = 0; i < 3; ++i)
                {
                    if (minimumScale[i] > 0)
                    {
                        float ratio = scale[i] / minimumScale[i];
                        if (ratio < minRatio)
                        {
                            minRatio = ratio;
                            minIdx = i;
                        }
                    }
                }

                if (minIdx != -1)
                {
                    scale /= minRatio;
                }
            }

            return scale;
        }

        private Vector3 GetLinkDimensions()
        {
            float linkLengthAdjustor = wireframeShape == WireframeType.Cubic ? 2.0f : 1.0f - (6.0f * wireframeEdgeRadius);
            return (currentBoundsExtents * linkLengthAdjustor) + new Vector3(wireframeEdgeRadius, wireframeEdgeRadius, wireframeEdgeRadius);
        }
        private bool ShouldRotateHandleBeVisible(CardinalAxisType axisType)
        {
            if (axisType == CardinalAxisType.X && showRotationHandleForX)
            {
                return true;
            }
            else if (axisType == CardinalAxisType.Y && showRotationHandleForY)
            {
                return true;
            }
            else if (axisType == CardinalAxisType.Z && showRotationHandleForZ)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void ResetHandleVisibility()
        {
            if (grabbedHandle != null)
            {
                return;
            }

            bool isVisible;

            //set balls visibility
            if (balls != null)
            {
                isVisible = (active == true && wireframeOnly == false);
                for (int i = 0; i < balls.Count; ++i)
                {
                    balls[i].SetActive(isVisible && ShouldRotateHandleBeVisible(edgeAxes[i]));
                    ApplyMaterialToAllRenderers(balls[i], handleMaterial);
                }
            }

            //set link visibility
            if (links != null)
            {
                isVisible = active == true;
                for (int i = 0; i < linkRenderers.Count; ++i)
                {
                    if (linkRenderers[i] != null)
                    {
                        linkRenderers[i].enabled = isVisible;
                    }
                }
            }

            //set box display visibility
            if (boxDisplay != null)
            {
                boxDisplay.SetActive(active);
                ApplyMaterialToAllRenderers(boxDisplay, boxMaterial);
            }

            //set corner visibility
            if (corners != null)
            {
                isVisible = (active == true && wireframeOnly == false && showScaleHandles == true);

                for (int i = 0; i < corners.Count; ++i)
                {
                    corners[i].SetActive(isVisible);
                    ApplyMaterialToAllRenderers(corners[i], handleMaterial);
                }
            }

            SetHiddenHandles();
        }
        private void ShowOneHandle(GameObject handle)
        {
            //turn off all balls
            if (balls != null)
            {
                for (int i = 0; i < balls.Count; ++i)
                {
                    if (balls[i] != handle)
                    {
                        balls[i].SetActive(false);
                    }
                    else
                    {
                        ApplyMaterialToAllRenderers(balls[i], handleGrabbedMaterial);
                    }
                }
            }

            //turn off all corners
            if (corners != null)
            {
                for (int i = 0; i < corners.Count; ++i)
                {
                    if (corners[i] != handle)
                    {
                        corners[i].SetActive(false);
                    }
                    else
                    {
                        ApplyMaterialToAllRenderers(corners[i], handleGrabbedMaterial);
                    }
                }
            }

            //update the box material to the grabbed material
            if (boxDisplay != null)
            {
                ApplyMaterialToAllRenderers(boxDisplay, boxGrabbedMaterial);
            }
        }

        private void UpdateBounds()
        {
            Vector3 boundsSize = Vector3.zero;
            Vector3 centroid = Vector3.zero;

            //store current rotation then zero out the rotation so that the bounds
            //are computed when the object is in its 'axis aligned orientation'.
            Quaternion currentRotation = Target.transform.rotation;
            Target.transform.rotation = Quaternion.identity;

            if (cachedTargetCollider != null)
            {
                boundsSize = cachedTargetCollider.bounds.extents;
                centroid = cachedTargetCollider.bounds.center;
            }

            //after bounds are computed, restore rotation...
            Target.transform.rotation = currentRotation;

            if (boundsSize != Vector3.zero)
            {
                if (flattenAxis == FlattenModeType.FlattenAuto)
                {
                    float min = Mathf.Min(boundsSize.x, Mathf.Min(boundsSize.y, boundsSize.z));
                    flattenAxis = min == boundsSize.x ? FlattenModeType.FlattenX : (min == boundsSize.y ? FlattenModeType.FlattenY : FlattenModeType.FlattenZ);
                }

                boundsSize.x = flattenAxis == FlattenModeType.FlattenX ? 0.0f : boundsSize.x;
                boundsSize.y = flattenAxis == FlattenModeType.FlattenY ? 0.0f : boundsSize.y;
                boundsSize.z = flattenAxis == FlattenModeType.FlattenZ ? 0.0f : boundsSize.z;

                currentBoundsExtents = boundsSize;

                boundsCorners[0] = new Vector3(+ currentBoundsExtents.x, + currentBoundsExtents.y, + currentBoundsExtents.z);
                boundsCorners[1] = new Vector3(- currentBoundsExtents.x, + currentBoundsExtents.y, + currentBoundsExtents.z);
                boundsCorners[2] = new Vector3(- currentBoundsExtents.x, - currentBoundsExtents.y, + currentBoundsExtents.z);
                boundsCorners[3] = new Vector3(+ currentBoundsExtents.x, - currentBoundsExtents.y, + currentBoundsExtents.z);
                boundsCorners[4] = new Vector3(+ currentBoundsExtents.x, + currentBoundsExtents.y, - currentBoundsExtents.z);
                boundsCorners[5] = new Vector3(- currentBoundsExtents.x, + currentBoundsExtents.y, - currentBoundsExtents.z);
                boundsCorners[6] = new Vector3(- currentBoundsExtents.x, - currentBoundsExtents.y, - currentBoundsExtents.z);
                boundsCorners[7] = new Vector3(+ currentBoundsExtents.x, - currentBoundsExtents.y, - currentBoundsExtents.z);

                CalculateEdgeCenters();
            }
        }

        private void UpdateRigHandles()
        {
            if (rigRoot != null && Target != null)
            {
                rigRoot.transform.rotation = Quaternion.identity;
                rigRoot.transform.position = Vector3.zero;

                for (int i = 0; i < corners.Count; ++i)
                {
                    corners[i].transform.position = boundsCorners[i];
                }

                Vector3 rootScale = rigRoot.transform.lossyScale;
                Vector3 invRootScale = new Vector3(1.0f / rootScale[0], 1.0f / rootScale[1], 1.0f / rootScale[2]);

                // Compute the local scale that produces the desired world space dimensions
                Vector3 linkDimensions = Vector3.Scale(GetLinkDimensions(), invRootScale);

                for (int i = 0; i < edgeCenters.Length; ++i)
                {
                    balls[i].transform.position = edgeCenters[i];

                    if (links != null)
                    {
                        links[i].transform.position = edgeCenters[i];

                        if (edgeAxes[i] == CardinalAxisType.X)
                        {
                            links[i].transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.x, wireframeEdgeRadius);
                        }
                        else if (edgeAxes[i] == CardinalAxisType.Y)
                        {
                            links[i].transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.y, wireframeEdgeRadius);
                        }
                        else//Z
                        {
                            links[i].transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.z, wireframeEdgeRadius);
                        }
                    }
                }

                if (boxDisplay != null)
                {
                    // Compute the local scale that produces the desired world space size
                    boxDisplay.transform.localScale = Vector3.Scale(2.0f * currentBoundsExtents, invRootScale);
                }

                //move rig into position and rotation
                rigRoot.transform.position = cachedTargetCollider.bounds.center;
                rigRoot.transform.rotation = Target.transform.rotation;
            }
        }
        private HandleType GetHandleType(GameObject handle)
        {
            for (int i = 0; i < balls.Count; ++i)
            {
                if (handle == balls[i])
                {
                    return HandleType.rotation;
                }
            }
            for (int i = 0; i < corners.Count; ++i)
            {
                if (handle == corners[i])
                {
                    return HandleType.scale;
                }
            }

            return HandleType.none;
        }
        private Collider GetGrabbedCollider(Vector3 grabPoint)
        {
            for (int i = 0; i < corners.Count; ++i)
            {
                if (corners[i].activeSelf)
                {
                    Collider collider = corners[i].GetComponent<Collider>();
                    if (collider != null && collider.bounds.Contains(grabPoint))
                    {
                        return collider;
                    }
                }
            }

            for (int i = 0; i < balls.Count; ++i)
            {
                if (balls[i].activeSelf)
                {
                    Collider collider = balls[i].GetComponent<Collider>();
                    if (collider != null && collider.bounds.Contains(grabPoint))
                    {
                        return collider;
                    }
                }
            }

            return null;
        }
        private Collider GetGrabbedCollider(Ray ray, out float distance)
        {
            Collider closestCollider = null;
            float currentDistance;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < corners.Count; ++i)
            {
                if (corners[i].activeSelf)
                {
                    currentDistance = PointToRayDistance(ray, corners[i].transform.position);
                    if (currentDistance < closestDistance && currentDistance < scaleHandleSize)
                    {
                        closestDistance = currentDistance;
                        closestCollider = corners[i].GetComponent<Collider>();
                    }
                }
            }
            for (int i = 0; i < balls.Count; ++i)
            {
                if (balls[i].activeSelf)
                {
                    currentDistance = PointToRayDistance(ray, balls[i].transform.position);
                    if (currentDistance < closestDistance && currentDistance < rotationHandleDiameter)
                    {
                        closestDistance = currentDistance;
                        closestCollider = balls[i].GetComponent<Collider>();
                    }
                }
            }

            distance = closestDistance;
            return closestCollider;
        }

        private Ray GetHandleGrabbedRay()
        {
            Ray pointerRay = new Ray();
            if (currentInputSource.Pointers.Length > 0)
            {
               currentInputSource.Pointers[0].TryGetPointingRay(out pointerRay);
            }

            return pointerRay;
        }
        private void Flatten()
        {
            if (flattenAxis == FlattenModeType.FlattenX)
            {
                flattenedHandles = new int[] { 0, 4, 2, 6 };
            }
            else if (flattenAxis == FlattenModeType.FlattenY)
            {
                flattenedHandles = new int[] { 1, 3, 5, 7 };
            }
            else if (flattenAxis == FlattenModeType.FlattenZ)
            {
                flattenedHandles = new int[] { 9, 10, 8, 11 };
            }

            if (flattenedHandles != null && linkRenderers != null)
            {
                for (int i = 0; i < flattenedHandles.Length; ++i)
                {
                    linkRenderers[flattenedHandles[i]].enabled = false;
                }
            }
        }
        private void AutoSetFlatten()
        {
            if (flattenAxis == FlattenModeType.FlattenAuto)
            {
                flattenAxis = FlattenModeType.FlattenZ;

                if (cachedTargetCollider != null)
                {
                    Vector3 size = cachedTargetCollider.bounds.size;
                    if (size.x < size.y && size.x < size.z)
                    {
                        flattenAxis = FlattenModeType.FlattenX;
                    }
                    else if (size.y < size.x && size.y < size.z)
                    {
                        flattenAxis = FlattenModeType.FlattenY;
                    }
                }
            }
        }
        private void SetHiddenHandles()
        {
            if (flattenedHandles != null)
            {
                for (int i = 0; i < flattenedHandles.Length; ++i)
                {
                    balls[flattenedHandles[i]].SetActive(false);
                }
            }
        }
        private bool GetHandLocation(IMixedRealityController controller, out Vector3 handPoint)
        {
            if (controller == null)
            {
                handPoint = Vector3.zero;
                return false;
            }
            IMixedRealityHandVisualizer handVisualizer = controller.Visualizer as IMixedRealityHandVisualizer;

            Transform indexTipTransform;
            if (handVisualizer != null && handVisualizer.TryGetJoint(TrackedHandJoint.IndexTip, out indexTipTransform))
            {
                handPoint = indexTipTransform.position;
                return true;
            }

            handPoint = Vector3.zero;
            return false;
        }
        private bool GetHandGrabPoint(IMixedRealityPointer pointer, out Vector3 grabPoint)
        {
            if (pointer == null || pointer.Controller == null)
            {
                grabPoint = Vector3.zero;
                return false;
            }
            IMixedRealityHandVisualizer handVisualizer = pointer.Controller.Visualizer as IMixedRealityHandVisualizer;

            Transform indexTipTransform;
            if (handVisualizer != null && handVisualizer.TryGetJoint(TrackedHandJoint.IndexTip, out indexTipTransform))
            {
                grabPoint= indexTipTransform.position;
                return true;
            }

            grabPoint = Vector3.zero;
            return false;
        }
        private bool GetHandRayPoint(IMixedRealityPointer pointer, out Vector3 grabPoint)
        {
            if (pointer != null)
            {
                grabPoint = pointer.Result.Details.Point;
                return true;
            }
            grabPoint = Vector3.zero;
            return false;
        }
        private bool IsInsideBoundingSphere(Vector3 pointToCheck)
        {
            if (corners.Count > 0)
            {
                // TODO This seems to assume that the Target origin is in the center of the bounding box. That may not be true.
                float radius = (corners[0].transform.position - Target.transform.position).magnitude + scaleHandleSize;
                float distance = (pointToCheck - Target.transform.position).magnitude;
                return distance <= radius;
            }

            // TODO The size of a box collider seems to be the full diagonal. If that's true we need to halve it.
            return (pointToCheck - Target.transform.position).magnitude <= cachedTargetCollider.size.magnitude + scaleHandleSize;
        }
        private void GetCornerPositionsFromBounds(Bounds bounds, ref Vector3[] positions)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            float leftEdge = center.x - extents.x;
            float rightEdge = center.x + extents.x;
            float bottomEdge = center.y - extents.y;
            float topEdge = center.y + extents.y;
            float frontEdge = center.z - extents.z;
            float backEdge = center.z + extents.z;

            if (positions == null || positions.Length != cornerCount)
            {
                positions = new Vector3[cornerCount];
            }

            positions[LBF] = new Vector3(leftEdge, bottomEdge, frontEdge);
            positions[LBB] = new Vector3(leftEdge, bottomEdge, backEdge);
            positions[LTF] = new Vector3(leftEdge, topEdge, frontEdge);
            positions[LTB] = new Vector3(leftEdge, topEdge, backEdge);
            positions[RBF] = new Vector3(rightEdge, bottomEdge, frontEdge);
            positions[RBB] = new Vector3(rightEdge, bottomEdge, backEdge);
            positions[RTF] = new Vector3(rightEdge, topEdge, frontEdge);
            positions[RTB] = new Vector3(rightEdge, topEdge, backEdge);
        }
        private static Vector3 PointToRay(Vector3 origin, Vector3 end, Vector3 closestPoint)
        {
            Vector3 originToPoint = closestPoint - origin;
            Vector3 originToEnd = end - origin;
            float magnitudeAB = originToEnd.sqrMagnitude;
            float dotProduct = Vector3.Dot(originToPoint, originToEnd);
            float distance = dotProduct / magnitudeAB;
            return origin + (originToEnd * distance);
        }
        private static float PointToRayDistance(Ray ray, Vector3 point)
        {
            return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
        }
        private static Vector3 GetSizeFromBoundsCorners(Vector3[] corners)
        {
            return new Vector3(Mathf.Abs(corners[0].x - corners[1].x),
                                Mathf.Abs(corners[0].y - corners[3].y),
                                Mathf.Abs(corners[0].z - corners[4].z));
        }
        private static Vector3 GetCenterFromBoundsCorners(Vector3[] corners)
        {
            Vector3 center = Vector3.zero;
            for (int i = 0; i < corners.Length; i++)
            {
                center += corners[i];
            }
            center *= (1.0f / (float)corners.Length);
            return center;
        }

        private static void ApplyMaterialToAllRenderers(GameObject root, Material material)
        {
            if (material != null)
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

                for (int i = 0; i < renderers.Length; ++i)
                {
                    renderers[i].material = material;
                }
            }
        }

        /// <summary>
        /// Returns true if parent is an ancestor of child
        /// </summary>
        /// <param name="eventData"></param>
        private bool IsAncestorOf(Transform child, Transform parent)
        {
            Transform cur = child;
            while (cur != null)
            {
                if (cur == parent)
                {
                    return true;
                }
                cur = cur.parent;
            }
            return false;
        }

        private bool DoesActivationMatchFocus(FocusEventData eventData)
        {
            switch (activation)
            {
                case BoundingBoxActivationType.ActivateOnStart:
                case BoundingBoxActivationType.ActivateManually:
                    return false;
                case BoundingBoxActivationType.ActivateByProximity:
                    return eventData.Pointer is IMixedRealityNearPointer;
                case BoundingBoxActivationType.ActivateByPointer:
                    return eventData.Pointer is IMixedRealityPointer;
                case BoundingBoxActivationType.ActivateByProximityAndPointer:
                    return true;
                default:
                    return false;
            }
        }
#endregion Private Methods

#region Used Event Handlers

        void IMixedRealityFocusChangedHandler.OnFocusChanged(FocusEventData eventData)
        {
            if (activation == BoundingBoxActivationType.ActivateManually || activation == BoundingBoxActivationType.ActivateOnStart)
            {
                return;
            }

            if (!DoesActivationMatchFocus(eventData))
            {
                return;
            }

            bool handInProximity = eventData.NewFocusedObject != null && IsAncestorOf(eventData.NewFocusedObject.transform, transform);
            if (handInProximity == wireframeOnly)
            {
                wireframeOnly = !handInProximity;
                ResetHandleVisibility();
            }
        }

        void IMixedRealityFocusHandler.OnFocusExit(FocusEventData eventData)
        {
            if (currentController != null && eventData.Pointer == currentPointer)
            {
                DropController();
            }
        }

        void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (currentController != null && eventData.SourceId == currentSourceId)
            {
                DropController();

                eventData.Use();
            }
        }

        void DropController()
        {
            HandleType lastHandleType = currentHandleType;

            currentInputSource = null;
            currentController = null;
            currentSourceId = 0;
            currentHandleType = HandleType.none;
            currentPointer = null;
            grabbedHandle = null;
            ResetHandleVisibility();

            if (lastHandleType == HandleType.scale)
            {
                if (debugText != null) debugText.text = "OnPointerUp:ScaleStopped";
                ScaleStopped?.Invoke();
            }
            else if (lastHandleType == HandleType.rotation)
            {
                if (debugText != null) debugText.text = "OnPointerUp:RotateStopped";
                RotateStopped?.Invoke();
            }
        }
        void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (currentController == null && !eventData.used)
            {
                if (eventData.MixedRealityInputAction.Description == "Grip Press" || eventData.MixedRealityInputAction.Description == "Select")
                {
                    Vector3 handPoint = Vector3.zero;
                    Vector3 rayPoint = Vector3.zero;
                    initialGrabbedPosition = Vector3.zero;
                    Collider handleColliderGrabbed = null;
                    farInteracting = false;

                    // TODO Review this, it seems that we could get the collider from the event data
                    if (GetHandGrabPoint(eventData.Pointer, out handPoint))
                    {
                        if (IsInsideBoundingSphere(handPoint) == true)
                        {
                            handleColliderGrabbed = GetGrabbedCollider(handPoint);
                            if (handleColliderGrabbed != null)
                            {
                                initialGrabbedPosition = handleColliderGrabbed.gameObject.transform.position;
                            }
                        }
                    }
                    if (handleColliderGrabbed == null && handPoint != Vector3.zero && GetHandRayPoint(eventData.Pointer, out rayPoint))
                    {
                        bool isFarPointer = !(eventData.Pointer is IMixedRealityNearPointer);
                        if (isFarPointer && IsInsideBoundingSphere(rayPoint) == true)
                        {
                            float distance;
                            handleColliderGrabbed = GetGrabbedCollider(new Ray(handPoint, (rayPoint - handPoint).normalized), out distance);
                            if (handleColliderGrabbed != null)
                            {
                                initialGrabbedPosition = handleColliderGrabbed.gameObject.transform.position;
                                rayOffset = initialGrabbedPosition - handPoint;
                                farInteracting = true;
                            }
                        }
                    }
                   
                    if (initialGrabbedPosition != Vector3.zero)
                    {
                        if (handleColliderGrabbed != null)
                        {
                            currentInputSource = (IMixedRealityInputSource)eventData.InputSource;
                            currentController = eventData.Pointer.Controller;
                            currentPointer = eventData.Pointer;
                            currentSourceId = eventData.InputSource.SourceId;
                            grabbedHandle = handleColliderGrabbed.gameObject;
                            currentHandleType = GetHandleType(grabbedHandle);

                            if (currentHandleType == HandleType.scale)
                            {
                                // Will use this to scale the target relative to the opposite corner
                                oppositeCorner = rigRoot.transform.TransformPoint(-grabbedHandle.transform.localPosition);
                                diagonalDir = (grabbedHandle.transform.position - oppositeCorner).normalized;
                            }

                            currentRotationAxis = GetRotationAxis(grabbedHandle);
                            initialRigCentroid = rigRoot.transform.position;
                            initialScale = Target.transform.localScale;
                            initialPosition = Target.transform.position;
                            ShowOneHandle(grabbedHandle);

                            if (currentHandleType == HandleType.scale)
                            {
                                if (debugText != null) debugText.text = "OnPointerDown:ScaleStarted";
                                ScaleStarted?.Invoke();
                            }
                            else if (currentHandleType == HandleType.rotation)
                            {
                                if (debugText != null) debugText.text = "OnPointerDown:RotateStarted";
                                RotateStarted?.Invoke();
                            }

                            eventData.Use();

                        }
                    }

                }
            }
        }

        public void OnSourceDetected(SourceStateEventData eventData)
        {
            if (eventData.Controller != null)
            {
                if (sourcesDetected.Count == 0 || sourcesDetected.Contains(eventData.Controller) == false)
                {
                    sourcesDetected.Add(eventData.Controller);
                }
            }
        }

        public void OnSourceLost(SourceStateEventData eventData)
        {
            sourcesDetected.Remove(eventData.Controller);

            if (currentController != null && currentController.InputSource.SourceId == eventData.SourceId)
            {
                HandleType lastHandleType = currentHandleType;

                currentInputSource = null;
                currentController = null;
                currentHandleType = HandleType.none;
                currentPointer = null;
                grabbedHandle = null;
                ResetHandleVisibility();

                if (lastHandleType == HandleType.scale)
                {
                    if (debugText != null) debugText.text = "OnSourceLost:ScaleStopped";
                    ScaleStopped?.Invoke();
                }
                else if (lastHandleType == HandleType.rotation)
                {
                    if (debugText != null) debugText.text = "OnSourceLost:RotateStopped";
                    RotateStopped?.Invoke();
                }
            }
        }
#endregion Used Event Handlers

#region Unused Event Handlers
        void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData) { }

        void IMixedRealityFocusChangedHandler.OnBeforeFocusChange(FocusEventData eventData) { }
#endregion Unused Event Handlers

        private void DrawTriax(Vector3 point)
        {
            float radius = 0.02f;
            Debug.DrawLine(point - new Vector3(0.0f, radius, 0.0f), point + new Vector3(0.0f, radius, 0.0f), Color.white, 5.0f);
            Debug.DrawLine(point - new Vector3(radius, 0.0f, 0.0f), point + new Vector3(radius, 0.0f, 0.0f), Color.white, 5.0f);
            Debug.DrawLine(point - new Vector3(0.0f, 0.0f, radius), point + new Vector3(0.0f, 0.0f, radius), Color.white, 5.0f);
        }
    }
}
