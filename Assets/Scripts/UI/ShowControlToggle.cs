using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ShowControlToggle : MonoBehaviour {
    public GameObject panel;
    private Toggle toggle;
    
	void Start () {
        toggle = gameObject.GetComponent<Toggle>();
        OnValueChanged(toggle.isOn);
        toggle.onValueChanged.AddListener(OnValueChanged);
    }

	private void OnValueChanged(bool value)
    {
        panel.SetActive(value);
    }


}
