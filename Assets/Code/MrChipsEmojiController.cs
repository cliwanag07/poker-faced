using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MrChipsEmojiController : MonoBehaviour {
    [SerializeField] private SpriteRenderer emojiRenderer;
    [SerializeField] private List<EmotionEntry> emotionEntries;
    [SerializeField] private float emojiSwitchTimer = 2f;

    private EmotionEntry currentState;
    private Queue<Sprite> currentEmojiQueue = new Queue<Sprite>();

    private void Awake() {
        SetEmotion(Emotion.Idle);
        StartCoroutine(EmojiLoop());
    }

    private IEnumerator EmojiLoop() {
        while (true) {
            yield return new WaitForSeconds(emojiSwitchTimer);

            if (currentEmojiQueue.Count == 0 && currentState != null) {
                ShuffleAndRefillQueue(currentState.emojiSprites);
            }

            if (currentEmojiQueue.Count > 0) {
                SetEmoji(currentEmojiQueue.Dequeue());
            }
        }
    }

    private void SetEmoji(Sprite sprite) {
        emojiRenderer.sprite = sprite;
    }

    public void SetEmotion(Emotion emotion) {
        currentState = emotionEntries.Find(e => e.emotion == emotion);
        if (currentState != null) {
            ShuffleAndRefillQueue(currentState.emojiSprites);
        }
    }

    private void ShuffleAndRefillQueue(Sprite[] sprites) {
        List<Sprite> shuffled = new List<Sprite>(sprites);
        for (int i = 0; i < shuffled.Count; i++) {
            int j = Random.Range(i, shuffled.Count);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        currentEmojiQueue = new Queue<Sprite>(shuffled);
    }

    [System.Serializable]
    public class EmotionEntry {
        public Emotion emotion;
        public Sprite[] emojiSprites;
    }
}

public enum Emotion {
    Idle,
    Thinking,
    Happy,
    Sad,
}