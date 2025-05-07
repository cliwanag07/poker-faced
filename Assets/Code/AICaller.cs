using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AICaller : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public event Action<string> OnApiResponseReceived;
    
    void Start() {   
    }

    // Update is called once per frame
    void Update() {  
    }
    
    public void GetAIResponse(string[] cards, string[] suits, double ratio, byte[] image) {
        StartCoroutine(GetAIResponseIEnumerator(cards, suits, ratio, image));
    }

    private IEnumerator GetAIResponseIEnumerator(string[] cards, string[] suits, double ratio, byte[] image) {
        string url = "http://localhost:8080/compute";
        string cardsArrayJson = JsonUtility.ToJson(cards);
        string suitsArrayJson = JsonUtility.ToJson(suits);

        WWWForm form = new WWWForm();
        form.AddField("cards", cardsArrayJson);
        form.AddField("suits", suitsArrayJson);
        form.AddField("ratio", ratio.ToString("F3"));
        form.AddBinaryData("image", image, "image.jpg", "image/jpg");

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success) {
            string jsonResponse = www.downloadHandler.text;
            string response = JsonUtility.FromJson<string>(jsonResponse);
            OnApiResponseReceived?.Invoke(response); 
        }
        else
        {
            Debug.LogError("Upload failed: " + www.error);
        }
    }
}
