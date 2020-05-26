using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShouldAlwaysBeDisabledAtStart : MonoBehaviour
{
	void Start()
	{
		gameObject.SetActive(false);
	}
}
