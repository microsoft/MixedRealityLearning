// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem
{
    public interface IMixedRealityNearPointer : IMixedRealityPointer
    {
        /// <summary>
        /// Returns true if the hand is near anything that's grabbable
        /// Currently performs a sphere cast in the direction of the hand ray.
        /// Currently anything that has a collider is considered "Grabbable"
        /// Eventually we need to filter based on things that can respond
        /// to grab events.
        /// </summary>
        /// <returns></returns>
        bool IsNearObject { get; }

        /// <summary>
        /// For near pointer we may want to draw a tether between the pointer
        /// and the object.
        /// 
        /// The visual grasp point (average of index and thumb) may actually be different from the pointer
        /// position (the palm).
        ///
        /// This method provides a mechanism to get the visual grasp point.
        ///
        /// NOTE: Not all near pointers have a grasp point (for example a poke pointer).
        /// </summary>
        /// <param name="rotation">Out parameter filled with the grasp position if available, otherwise <see cref="Vector3.zero"/>.</param>
        /// <returns>True if a grasp point was retrieved, false if not.</returns>
        bool TryGetNearGraspPoint(out Vector3 position);
    }
}