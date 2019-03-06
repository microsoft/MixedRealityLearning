// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Core.Extensions;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Services.InputSystem
{
    /// <summary>
    /// Add a NearInteractionTouchable to your scene and configure a touchable surface
    /// in order to get PointerDown and PointerUp events whenever a PokePointer touches this surface.
    ///
    /// Technical details:
    /// Provides a listing of near field touch proximity bounds.
    /// This is used to detect if a contact point is near an object to turn on near field interactions
    /// </summary>
    public class NearInteractionTouchable : MonoBehaviour
    {
#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(NearInteractionTouchable))]
        public class Editor : UnityEditor.Editor
        {
            private readonly Color handleColor = Color.blue;
            private readonly Color fillColor = new Color(0, 0, 0, 0);

            protected virtual void OnSceneGUI()
            {
                NearInteractionTouchable t = (NearInteractionTouchable)target;

                if (Event.current.type == EventType.Repaint)
                {
                    UnityEditor.Handles.color = handleColor;

                    float arrowSize = UnityEditor.HandleUtility.GetHandleSize(t.transform.position) * 0.75f;
                    UnityEditor.Handles.ArrowHandleCap(0, t.transform.position, Quaternion.LookRotation(t.transform.rotation * t.localForward), arrowSize, EventType.Repaint);

                    Vector3 rightDelta = t.transform.localToWorldMatrix.MultiplyVector(t.localRight * t.bounds.x / 2);
                    Vector3 upDelta = t.transform.localToWorldMatrix.MultiplyVector(t.localUp * t.bounds.y / 2);

                    Vector3[] points = new Vector3[4];
                    points[0] = t.transform.position + rightDelta + upDelta;
                    points[1] = t.transform.position - rightDelta + upDelta;
                    points[2] = t.transform.position - rightDelta - upDelta;
                    points[3] = t.transform.position + rightDelta - upDelta;

                    UnityEditor.Handles.DrawSolidRectangleWithOutline(points, fillColor, handleColor);

                }
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                NearInteractionTouchable t = (NearInteractionTouchable)target;

                RectTransform rt = t.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // Resize Helper
                    if (rt.sizeDelta != t.bounds)
                    {
                        UnityEditor.EditorGUILayout.HelpBox("Bounds do not match the RectTransform size", UnityEditor.MessageType.Warning);
                        if (GUILayout.Button("Fix Bounds"))
                        {
                            UnityEditor.Undo.RecordObject(t, "Fix Bounds");
                            t.bounds = rt.sizeDelta;
                        }
                    }

                    if (t.GetComponentInParent<Canvas>() != null && t.localForward != new Vector3(0, 0, -1))
                    {
                        UnityEditor.EditorGUILayout.HelpBox("Unity UI generally has forward facing away from the front. The LocalForward direction specified does not match the expected forward direction.", UnityEditor.MessageType.Warning);
                        if (GUILayout.Button("Fix Forward Direction"))
                        {
                            UnityEditor.Undo.RecordObject(t, "Fix Forward Direction");
                            t.localForward = new Vector3(0, 0, -1);
                        }
                    }
                }
            }
        }
#endif

        public static IReadOnlyCollection<NearInteractionTouchable> Instances { get { return instances.AsReadOnly(); } }
        private static readonly List<NearInteractionTouchable> instances = new List<NearInteractionTouchable>();

        /// <summary>
        /// Local space forward direction
        /// </summary>
        [SerializeField]
        protected Vector3 localForward = Vector3.forward;

        /// <summary>
        /// Local space forward direction
        /// </summary>
        [SerializeField]
        protected Vector3 localUp = Vector3.up;

        protected Vector3 localRight { get { return Vector3.Cross(localUp, localForward); } }

        public Vector3 Forward { get { return transform.TransformDirection(localForward); } }

        /// <summary>
        /// Local space forward direction
        /// </summary>
        [SerializeField]
        protected Vector2 bounds = Vector2.zero;

        protected void OnEnable()
        {
            instances.Add(this);
        }

        protected void OnDisable()
        {
            instances.Remove(this);
        }

        protected void OnValidate()
        {
            Debug.Assert(localForward.magnitude > 0);
            Debug.Assert(localUp.magnitude > 0);
            string hierarchy = gameObject.transform.EnumerateAncestors(true).Aggregate("", (result, next) => next.gameObject.name + "=>" + result);
            Debug.Assert(Vector3.Angle(localForward, localUp) > 80, $"localForward and localUp not perpendicular for object {hierarchy}. Did you set Local Forward correctly?");

            // Check initial setup
            if (bounds == Vector2.zero)
            {
                RectTransform rt = GetComponent<RectTransform>();
                if (rt != null)
                {
                    // Initilize bounds to RectTransform SizeDelta
                    bounds = rt.sizeDelta;
                    localForward = new Vector3(0, 0, -1);
                }
            }

            localForward = localForward.normalized;
            localUp = localUp.normalized;

            bounds.x = Mathf.Max(bounds.x, 0);
            bounds.y = Mathf.Max(bounds.y, 0);
        }

        public virtual float DistanceToSurface(Vector3 samplePoint)
        {
            Vector3 localPoint = transform.InverseTransformPoint(samplePoint);

            // Get point on plane
            Plane plane = new Plane(localForward, Vector3.zero);
            Vector3 pointOnPlane = plane.ClosestPointOnPlane(localPoint);

            // Get plane coordinates
            Vector2 planeSpacePoint = new Vector2(
                Vector3.Dot(pointOnPlane, localRight),
                Vector3.Dot(pointOnPlane, localUp));

            // Clamp to bounds
            planeSpacePoint = new Vector2(
                Mathf.Clamp(planeSpacePoint.x, -bounds.x / 2, bounds.x / 2),
                Mathf.Clamp(planeSpacePoint.y, -bounds.y / 2, bounds.y / 2));

            // Convert back to 3D space
            Vector3 clampedPoint = transform.TransformPoint(localRight * planeSpacePoint.x + localUp * planeSpacePoint.y);

            return (samplePoint - clampedPoint).magnitude;
        }

    }
}