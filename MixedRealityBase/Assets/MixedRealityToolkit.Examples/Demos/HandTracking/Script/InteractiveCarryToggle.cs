using Microsoft.MixedReality.Toolkit.SDK.Utilities.Solvers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveCarryToggle : MonoBehaviour
{
    private Orbital orbital;

    private void Start()
    {
        // Get Orbital Solver component
        orbital = GetComponent<Orbital>();
    }

    public void ToggleInteractiveCarry()
    {
        if(orbital != null)
        {
            // Toggle Orbital Solver component
            // You can tweak the detailed positioning behavior such as offset, lerping time, orientation type in the Inspector panel
            orbital.enabled = !orbital.enabled;
        }
        
    }
}
