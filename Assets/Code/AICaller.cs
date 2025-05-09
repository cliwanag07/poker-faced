using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AICaller : MonoBehaviour
{
    public event Action<float> OnApiResponseReceived;
    
    void Start() { }

    void Update() { }
    
    public IEnumerator PingServer(Action<bool> onComplete = null, string url = "http://localhost:8080/test")
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            bool success = www.result == UnityWebRequest.Result.Success;
            onComplete?.Invoke(success);
        }
    }
    
    public void GetAIResponse(List<int> cards, List<string> suits, double ratio, byte[] image) {
        StartCoroutine(GetAIResponseIEnumerator(cards, suits, ratio, image));
    }

    private IEnumerator GetAIResponseIEnumerator(List<int> cards, List<string> suits, double ratio, byte[] image) {
        Debug.Log(string.Join(", ", cards));
        Debug.Log(string.Join(", ", suits));
        Debug.Log(ratio);
#if UNITY_EDITOR
        Debug.Log("Image byte array: " + Convert.ToBase64String(image)); // Print the byte array
#endif

        string url = "http://localhost:8080/compute";

        // Serialize lists properly
        IntListWrapper cardsWrapper = new IntListWrapper {
            items = cards
        };
        StringListWrapper suitsWrapper = new StringListWrapper {
            items = suits
        };

        string cardsListJson = JsonUtility.ToJson(cardsWrapper);
        string suitsListJson = JsonUtility.ToJson(suitsWrapper);

        WWWForm form = new WWWForm();
        form.AddField("cards", cardsListJson);
        form.AddField("suits", suitsListJson);
        form.AddField("ratio", ratio.ToString("F3"));
        form.AddBinaryData("image", image, "image.jpg", "image/jpg");
        
#if UNITY_EDITOR
        Debug.Log("Cards JSON: " + cardsListJson);
        Debug.Log("Suits JSON: " + suitsListJson);
        Debug.Log("Ratio: " + ratio.ToString("F3"));
#endif

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success) {
            string jsonResponse = www.downloadHandler.text;
            Debug.Log(jsonResponse);
            FloatResponse parsed = JsonUtility.FromJson<FloatResponse>(jsonResponse);
            Debug.Log(parsed.value);
            OnApiResponseReceived?.Invoke(float.Parse(parsed.value.ToString("F3")));
        }
        else {
#if UNITY_EDITOR
            Debug.LogError("Upload failed: " + www.error);
#endif
        }
    }

    [System.Serializable]
    public class StringListWrapper {
        public List<string> items;
    }

    [System.Serializable]
    public class IntListWrapper {
        public List<int> items;
    }
    
    [System.Serializable]
    public class FloatResponse {
        public float value;
    }

}
