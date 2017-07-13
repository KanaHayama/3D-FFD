using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoShadowButton : MonoBehaviour {
    public Light[] lights;
    public Button off;
    public Button soft;
    public Button hard;

	// Use this for initialization
	void Start () {
        off.onClick.AddListener(OnClick);
	}
	
	private void OnClick()
    {
        foreach(Light l in lights)
        {
            l.shadows = LightShadows.None;            
        }
        off.interactable = false;
        soft.interactable = true;
        hard.interactable = true;
    }
}
