using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Battlehub.RTCommon;

public class YNumberSlider : MonoBehaviour
{
    private Slider slider;
    private FFD3D[] ffd3ds;
    private bool update;

    // Use this for initialization
    void Start()
    {
        update = true;
        slider = gameObject.GetComponentInChildren<Slider>();
        slider.onValueChanged.AddListener(OnValueChanged);
        ffd3ds = GameObject.FindObjectsOfType<FFD3D>();
        foreach (FFD3D c in ffd3ds)
        {
            c.reloadEvent += OnReload;
        }
    }

    void OnDestroy()
    {
        foreach (FFD3D c in ffd3ds)
        {
            c.reloadEvent -= OnReload;
        }
    }

    private void OnValueChanged(float value)
    {
        if (slider != null && update)
        {
            slider.GetComponentInChildren<Text>().text = slider.value.ToString();
            foreach (FFD3D c in ffd3ds)
            {
                c.initControlPoints(c.xNumber, (int)slider.value, c.zNumber);
            }
            RuntimeSelection.Select(null, null);
        }
    }

    private void OnReload()
    {
        update = false;
        try
        {
            if (slider != null)
            {
                foreach (FFD3D c in ffd3ds)
                {
                    slider.value = c.xNumber;
                }
            }
        }
        finally
        {
            update = true;
        }
    }
}
