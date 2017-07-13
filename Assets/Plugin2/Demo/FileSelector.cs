using UnityEngine;
using System.Collections;
using OxOD;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Events;

public class FileSelector : MonoBehaviour
{
    [System.Serializable]
    public class FileSelectEvent : UnityEvent<string>
    {
    }

    [Header("OxOD Reference")]
    public FileDialog dialog;

    [Header("File Dialogue Options")]
    public FileDialog.FileDialogMode mode;
    public string extensions;
    public int maxSize = -1;
    public bool saveLastPath = true;

    [Header("Events")]
    public FileSelectEvent OnDialogueEnded;

    [Header("Internal References")]
    public InputField selectedFile;

    [HideInInspector]
    public string result;

    public void SelectFile()
    {
        StartCoroutine(Select(result));
    }

    public IEnumerator Select(string path)
    {
        Debug.Log("[FileSelector] Starting file Dialogue");

        if (mode == FileDialog.FileDialogMode.Open)
        {
            yield return StartCoroutine(dialog.Open(path, extensions, "OPEN FILE", null, maxSize, saveLastPath));
        }
        else if (mode == FileDialog.FileDialogMode.Save)
        {
            yield return StartCoroutine(dialog.Save(path, extensions, "SAVE FILE", null, saveLastPath));
        }
        else
        {
            yield return StartCoroutine(dialog.SelectFolder(path, "SELECT FOLDER", null, saveLastPath));
        }

        if (dialog.result != null)
        {
            Debug.Log("[FileSelector] Dialogue ended, result: " + dialog.result);

            result = dialog.result;
            selectedFile.text = new FileInfo(dialog.result).Name;

            OnDialogueEnded.Invoke(result);
        }
        else
        {
            Debug.Log("[FileSelector] Dialogue canceled");
        }
    }
}
