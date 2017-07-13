using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneController : MonoBehaviour {
    public KeyCode exitKey = KeyCode.Escape;
    public GameObject panel;
    public float duration = 1;
    public CPUModeButton cpu;
    public GPUModeButton gpu;
    public KeyCode cpuKey = KeyCode.C;
    public KeyCode gpuKey = KeyCode.G;

	// Use this for initialization
	void Start () {
        StartCoroutine(hideMask());
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(exitKey))
        {
            #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
            #else
        Application.Quit();
            #endif
        }
        if (Input.GetKeyDown(cpuKey))
        {
            cpu.press();
        }
        else if (Input.GetKeyDown(gpuKey))
        {
            gpu.press();
        }
    }

    IEnumerator hideMask()
    {
        panel.SetActive(true);
        float totalTime = 0;
        Image i = panel.GetComponent<Image>();
        Color c = i.color;
        float begin = 1;
        float end = 0;
        c.a = begin;
        i.color = c;
        yield return null;
        while (totalTime < duration)
        {
            totalTime += Time.deltaTime;
            c.a = Mathf.Lerp(begin, end, totalTime / duration);
            i.color = c;
            yield return null;
        }
        panel.SetActive(false);
    }
}
