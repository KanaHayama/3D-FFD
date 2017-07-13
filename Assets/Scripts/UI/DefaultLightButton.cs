using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Battlehub.RTCommon;

public class DefaultLightButton : MonoBehaviour {
    public GameObject defaultLight;
    public GameObject customLight;
    public Button defaultLightButton;
    public Button customLightButton;

	void Start () {
        defaultLightButton.onClick.AddListener(OnClick);
	}

    private void OnClick()
    {
        defaultLight.SetActive(true);
        customLight.SetActive(false);
        defaultLightButton.interactable = false;
        customLightButton.interactable = true;
        RuntimeSelection.Select(null, null);
    }
}
