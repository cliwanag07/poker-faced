using UnityEngine;
using UnityEngine.UI;

public class FaceCamSquareCrop : MonoBehaviour
{
    [SerializeField] private RawImage rawImage; // Assign in Inspector
    private WebCamTexture webCamTexture;

    private void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            webCamTexture = new WebCamTexture(devices[0].name);
            rawImage.texture = webCamTexture;
            webCamTexture.Play();

            StartCoroutine(SetCenteredSquareCrop());
        }
        else
        {
            Debug.LogWarning("No webcam detected.");
        }
    }

    private System.Collections.IEnumerator SetCenteredSquareCrop()
    {
        // Wait until the webcam texture is initialized
        while (webCamTexture.width <= 16)
            yield return null;

        int width = webCamTexture.width;
        int height = webCamTexture.height;

        float xOffset = 0f;
        float yOffset = 0f;
        float cropSize = 1f;

        if (width > height)
        {
            float diff = (width - height) / 2f;
            xOffset = diff / width;
            cropSize = height / (float)width;
            rawImage.uvRect = new Rect(xOffset, 0f, cropSize, 1f);
        }
        else
        {
            float diff = (height - width) / 2f;
            yOffset = diff / height;
            cropSize = width / (float)height;
            rawImage.uvRect = new Rect(0f, yOffset, 1f, cropSize);
        }
    }

    private void OnDisable()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
            webCamTexture.Stop();
    }
}