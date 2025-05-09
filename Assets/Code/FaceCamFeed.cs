using UnityEngine;
using UnityEngine.UI;

public class FaceCamSquareCrop : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;

    private WebCamTexture webCamTexture;
    private bool isInitialized = false;

    private void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            webCamTexture = new WebCamTexture(devices[0].name, 640, 480, 30); // Set FPS
            // rawImage.texture = webCamTexture;
            rawImage.material = null;
            webCamTexture.Play();

            StartCoroutine(WaitForWebCamInit());
        }
        else
        {
            Debug.LogWarning("No webcam detected.");
        }
    }

    private System.Collections.IEnumerator WaitForWebCamInit()
    {
        // Wait until webcam texture is initialized (some platforms need longer)
        while (webCamTexture.width < 100)
            yield return new WaitForSeconds(0.1f);

        // Optional: log resolution and FPS
        Debug.Log($"Webcam initialized: {webCamTexture.width}x{webCamTexture.height}, FPS: {webCamTexture.requestedFPS}");

        SetCenteredSquareUV();
        isInitialized = true;
    }

    private void SetCenteredSquareUV()
    {
        int width = webCamTexture.width;
        int height = webCamTexture.height;

        if (width > height)
        {
            float xOffset = (width - height) / 2f / width;
            float cropSize = height / (float)width;
            rawImage.uvRect = new Rect(xOffset, 0f, cropSize, 1f);
        }
        else
        {
            float yOffset = (height - width) / 2f / height;
            float cropSize = width / (float)height;
            rawImage.uvRect = new Rect(0f, yOffset, 1f, cropSize);
        }
    }

    private void OnDisable()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
            webCamTexture.Stop();
    }

    /// <summary>
    /// Call this only when needed to avoid allocations.
    /// </summary>
    public Texture2D GetCroppedSquareTexture()
    {
        if (!isInitialized) return null;

        int width = webCamTexture.width;
        int height = webCamTexture.height;
        int squareSize = Mathf.Min(width, height);
        int x = (width - squareSize) / 2;
        int y = (height - squareSize) / 2;

        Texture2D tex = new Texture2D(squareSize, squareSize, TextureFormat.RGB24, false);
        tex.SetPixels(webCamTexture.GetPixels(x, y, squareSize, squareSize));
        tex.Apply();
        return tex;
    }
}
