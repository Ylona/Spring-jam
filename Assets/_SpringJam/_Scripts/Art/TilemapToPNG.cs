using UnityEngine;
using System.IO;

public class TilemapToPNG : MonoBehaviour
{
    public Camera renderCamera;
    public RenderTexture renderTexture;
    public string fileName = "tilemap_export.png";

    [ContextMenu("Save Tilemap To PNG")]
    public void SavePNG()
    {
        if (renderCamera == null || renderTexture == null)
        {
            Debug.LogError("Render camera or render texture is missing.");
            return;
        }

        RenderTexture currentActiveRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        renderCamera.targetTexture = renderTexture;
        renderCamera.Render();

        Texture2D image = new Texture2D(
            renderTexture.width,
            renderTexture.height,
            TextureFormat.RGBA32,
            false,
            true // lees als linear
        );

        image.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        image.Apply();

        // Zet linear -> gamma zodat PNG eruit ziet zoals jij verwacht
        Color[] pixels = image.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = pixels[i].gamma;
        }
        image.SetPixels(pixels);
        image.Apply();

        byte[] bytes = image.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, fileName);
        File.WriteAllBytes(path, bytes);

        RenderTexture.active = currentActiveRT;

        Debug.Log("PNG saved to: " + path);
    }
}