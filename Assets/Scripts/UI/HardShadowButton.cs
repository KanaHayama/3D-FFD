using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HardShadowButton : MonoBehaviour
{
    public Light[] lights;
    public Button off;
    public Button soft;
    public Button hard;

    // Use this for initialization
    void Start()
    {
        hard.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        foreach (Light l in lights)
        {
            l.shadows = LightShadows.Hard;
        }
        off.interactable = true;
        soft.interactable = true;
        hard.interactable = false;
    }
}
