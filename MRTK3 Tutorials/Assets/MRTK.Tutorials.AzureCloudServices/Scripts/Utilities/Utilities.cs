// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Utilities
{
    public static class Utilities
    {
        /// <summary>
        /// Create a Sprite from a Texture2D.
        /// </summary>
        /// <param name="texture">Source texture.</param>
        /// <returns>Sprite generated from the given texture.</returns>
        public static Sprite CreateSprite(this Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
