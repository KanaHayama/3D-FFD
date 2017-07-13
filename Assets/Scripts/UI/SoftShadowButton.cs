using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoftShadowButton : MonoBehaviour
{
    public Light[] lights;
    public Button off;
    public Button soft;
    public Button hard;

    // Use this for initialization
    void Start()
    {
        soft.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        foreach (Light l in lights)
        {
            l.shadows = LightShadows.Soft;            
        }
        off.interactable = true;
        soft.interactable = false;
        hard.interactable = true;
    }
}
