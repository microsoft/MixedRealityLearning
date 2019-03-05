using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chira
{
    public class ChangeHandModeOnKeyPress : MonoBehaviour
    {
        [Tooltip("Press this key to change hand display modes")]
        public KeyCode Key = KeyCode.N;

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(Key))
            {
                if (ShowDebugChiraHands.Instance != null)
                {
                    ShowDebugChiraHands.Instance.ChangeHandMode();
                }
                else
                {
                    Debug.LogError("Tried to change hand mode on debug chira hands but no debug chira hands in the scene! Did you forget to add it?");
                }
            }
        }
    }

}