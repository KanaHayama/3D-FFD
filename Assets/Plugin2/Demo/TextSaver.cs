using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.IO;

public class TextSaver : MonoBehaviour
{
    public FileSelector selector;
    public InputField text;

    public void SaveText(string path)
    {
        File.WriteAllText(path, text.text);
        Debug.Log("[TextSaver] Text saved to " + path);
    }

    public void OnSaveClick()
    {
        SaveText(selector.result);
    }
}
