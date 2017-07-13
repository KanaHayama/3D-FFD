using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Battlehub.RTCommon;

[RequireComponent(typeof(Button))]
public class UndoButton : MonoBehaviour {
    private Button button;
    
	void Start () {
        button = gameObject.GetComponent<Button>();
        button.onClick.AddListener(OnClick);
        RuntimeUndo.StateChanged += StateChanged;
        RuntimeUndo.UndoCompleted += StateChanged;
        StateChanged();
	}

    void Destroy()
    {
        RuntimeUndo.UndoCompleted -= StateChanged;
        RuntimeUndo.StateChanged -= StateChanged;
    }

    private void StateChanged()
    {
        //button.interactable = RuntimeUndo.CanUndo;
    }

    private void OnClick()
    {
        RuntimeUndo.Undo();
    }
}
