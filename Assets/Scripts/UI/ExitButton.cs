using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Button))]
public class ExitButton : MonoBehaviour {
    
	void Start () {
        gameObject.GetComponent<Button>().onClick.AddListener(OnClick);
	}

    private void OnClick()
    {
        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
