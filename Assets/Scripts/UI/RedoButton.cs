using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Battlehub.RTCommon;

[RequireComponent(typeof(Button))]
public class RedoButton : MonoBehaviour
{
    private Button button;

    void Start()
    {
        button = gameObject.GetComponent<Button>();
        button.onClick.AddListener(OnClick);
        RuntimeUndo.StateChanged += StateChanged;
        RuntimeUndo.RedoCompleted += StateChanged;
        StateChanged();
    }

    void Destroy()
    {
        RuntimeUndo.RedoCompleted -= StateChanged;
        RuntimeUndo.StateChanged -= StateChanged;
    }

    private void StateChanged()
    {
        //button.interactable = RuntimeUndo.CanRedo;
    }

    private void OnClick()
    {
        RuntimeUndo.Redo();
    }
}
