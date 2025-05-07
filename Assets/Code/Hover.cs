using UnityEngine;

public class HoverEffect : MonoBehaviour {
    [SerializeField] private Transform target; // The object to hover
    [SerializeField] private float amplitude = 0.25f; // Height of the hover
    [SerializeField] private float frequency = 1f; // Speed of the hover

    private Vector3 startPos;

    private void Start() {
        if (target == null) target = transform;
        startPos = target.localPosition;
    }

    private void Update() {
        float yOffset = Mathf.Sin(Time.time * frequency) * amplitude;
        target.localPosition = startPos + new Vector3(0f, yOffset, 0f);
    }
}