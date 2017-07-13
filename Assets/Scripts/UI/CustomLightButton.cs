using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Battlehub.RTCommon;

public class CustomLightButton : MonoBehaviour
{
    public GameObject defaultLight;
    public GameObject customLight;
    public Button defaultLightButton;
    public Button customLightButton;

    void Start()
    {
        customLightButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        defaultLight.SetActive(false);
        customLight.SetActive(true);
        defaultLightButton.interactable = true;
        customLightButton.interactable = false;
        RuntimeSelection.Select(customLight, new Object[] { customLight });
        RuntimeTools.Current = RuntimeTool.Rotate;
        
    }
}
