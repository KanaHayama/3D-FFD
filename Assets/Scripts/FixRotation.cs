using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixRotation : MonoBehaviour {
    public Vector3 rotation;
	
	void LateUpdate () {
        transform.localRotation = Quaternion.Euler(rotation);
	}
}
