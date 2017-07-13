﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Battlehub.RTCommon;

[RequireComponent(typeof(Button))]
public class PositionButton : MonoBehaviour {
    private Button button;
	
	void Start () {
        button = gameObject.GetComponent<Button>();
        button.onClick.AddListener(OnClick);
        RuntimeTools.ToolChanged += ToolChanged;
        ToolChanged();

    }

    void Destroy()
    {
        RuntimeTools.ToolChanged -= ToolChanged;
    }

    private void OnClick()
    {
        RuntimeTools.Current = RuntimeTool.Move;
    }

    private void ToolChanged()
    {
        
        if (button != null)
            button.interactable = RuntimeTools.Current != RuntimeTool.Move;
    }
}