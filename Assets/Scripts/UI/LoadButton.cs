using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OxOD;
using Battlehub.RTCommon;
using Battlehub.RTHandles;
using System.IO;
using System.Text;

[RequireComponent(typeof(Button))]
public class LoadButton : MonoBehaviour
{
    private const string EXTENSION = ".ffd";
    public FileDialog dialog;

    // Use this for initialization
    void Start()
    {
        gameObject.GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        StartCoroutine(Save());
    }

    public IEnumerator Save()
    {
        MouseOrbit mo = GameObject.FindObjectOfType<MouseOrbit>();
        RuntimeSelectionComponent rsc = GameObject.FindObjectOfType<RuntimeSelectionComponent>();
        BoxSelection bs = GameObject.FindObjectOfType<BoxSelection>();
        mo.enable = false;
        rsc.enable = false;
        bs.enable = false;
        try
        {
            yield return StartCoroutine(dialog.Open(null, EXTENSION, "LOAD FILE", null, -1, true));
            if (dialog.result != null)
            {
                StreamReader sr = new StreamReader(dialog.result, Encoding.UTF8);
                try
                {
                    string lines = sr.ReadToEnd();
                    FFD3D ffd3d = GameObject.FindObjectOfType<FFD3D>();
                    ffd3d.decodeFromString(lines);
                }
                finally
                {
                    sr.Close();
                }
            }
        }
        finally
        {
            bs.enable = true;
            rsc.enable = true;
            mo.enable = true;
        }
    }
}
