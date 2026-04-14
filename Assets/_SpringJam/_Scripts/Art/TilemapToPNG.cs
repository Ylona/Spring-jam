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

        RenderTexture previousActiveRT = RenderTexture.active;
        RenderTexture previousCameraTarget = renderCamera.targetTexture;
        Texture2D image = null;

        try
        {
            renderCamera.targetTexture = renderTexture;
            renderCamera.Render();

            RenderTexture.active = renderTexture;

            image = new Texture2D(
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

            Debug.Log("PNG saved to: " + path);
        }
        finally
        {
            RenderTexture.active = previousActiveRT;
            renderCamera.targetTexture = previousCameraTarget;
            if (image != null)
                DestroyImmediate(image);
        }
    }
}