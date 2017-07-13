using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GPUModeButton : MonoBehaviour {
    public Button cpu;
    public Button gpu;

    // Use this for initialization
    void Start()
    {
        gpu.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        foreach (FFD3D c in GameObject.FindObjectsOfType<FFD3D>())
        {
            bool result = c.setComputeMode(false);
            cpu.interactable = result;
            gpu.interactable = !result;
        }
    }

    public void press()
    {
        OnClick();
    }
}
