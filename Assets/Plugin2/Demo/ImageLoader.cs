using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ImageLoader : MonoBehaviour
{
    public FileSelector selector;
    public Image img;

    public IEnumerator LoadImage(string path)
    {
        WWW m_get = new WWW("file://" + path);
        yield return m_get;
        Texture2D texture = m_get.texture;
        Sprite image = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
        yield return image;
        img.sprite = image;
        Debug.Log("[ImageLoader] Loaded image from " + path);
    }

    public void OnLoadClick()
    {
        StartCoroutine(LoadImage(selector.result));
    }
}
