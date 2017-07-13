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
public class SaveButton : MonoBehaviour {
    private const string EXTENSION = ".ffd";
    public FileDialog dialog;

    // Use this for initialization
    void Start () {
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
            yield return StartCoroutine(dialog.Save(null, EXTENSION, "SAVE FILE", null, true));
            if (dialog.result != null)
            {
                FileStream fs = new FileStream(dialog.result, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                try
                {
                    FFD3D ffd3d = GameObject.FindObjectOfType<FFD3D>();
                    sw.Write(ffd3d.encodeToString());
                }
                finally
                {
                    sw.Close();
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
