using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CPUModeButton : MonoBehaviour {
    public Button cpu;
    public Button gpu;

	// Use this for initialization
	void Start () {
        cpu.onClick.AddListener(OnClick);
	}
	
	private void OnClick()
    {
        foreach(FFD3D c in GameObject.FindObjectsOfType<FFD3D>())
        {
            c.setComputeMode(true);
            cpu.interactable = false;
            gpu.interactable = true;
        }
    }

    public void press()
    {
        OnClick();
    }
}
