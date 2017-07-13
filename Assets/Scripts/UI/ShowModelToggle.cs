using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ShowModelToggle : MonoBehaviour {
    private Toggle toggle;
    private FFD3D[] ffd3ds;

    void Start()
    {
        toggle = gameObject.GetComponent<Toggle>();
        ffd3ds = GameObject.FindObjectsOfType<FFD3D>();
        OnValueChanged(toggle.isOn);
        toggle.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(bool value)
    {
        foreach(FFD3D c in ffd3ds)
        {
            c.showObject = value;
        }
    }
}
