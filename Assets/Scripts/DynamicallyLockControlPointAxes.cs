using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Battlehub.RTCommon;
using System.Linq;

[RequireComponent(typeof(LockAxes))]
public class DynamicallyLockControlPointAxes : MonoBehaviour {

    private LockAxes lockAxes;
    private bool selected;
    
	void Start () {
        lockAxes = gameObject.GetComponent<LockAxes>();
        RuntimeSelection.SelectionChanged += OnSelectionChanged;
    }

    void OnDestroy()
    {
        RuntimeSelection.SelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(Object[] unselectedObjects)
    {
        GameObject[] array = RuntimeSelection.gameObjects;
        int len = array == null ? 0 : array.Length;
        bool selected = array == null ? false : array.Any(o => o.Equals(gameObject));  //C# Linq
        bool multiSelected = len > 1;
        bool singleSelected = len == 1;
        bool needLock = selected && !multiSelected;
        lockAxes.RotationX = needLock;
        lockAxes.RotationY = needLock;
        lockAxes.RotationZ = needLock;
        lockAxes.RotationScreen = needLock;
        lockAxes.ScaleX = needLock;
        lockAxes.ScaleY = needLock;
        lockAxes.ScaleZ = needLock;
        if (singleSelected) RuntimeTools.Current = RuntimeTool.Move;
    }
}
