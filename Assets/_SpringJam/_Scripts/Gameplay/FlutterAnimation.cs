using UnityEngine;

public class FlutterAnimation : MonoBehaviour
{
    [SerializeField] private float flutterSpeed = 10f;
    [SerializeField] private float flutterAmount = 15f;
    [SerializeField] private float timeOffset = 0f;

    private Vector3 startPosition;

    void Start()
    {
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
        startPosition = transform.localPosition;
    }

    void Update()
    {
        float offsetX = Mathf.Sin((Time.time + timeOffset) * flutterSpeed) * flutterAmount;
        float offsetY = Mathf.Sin((Time.time + timeOffset + 1f) * flutterSpeed * 0.7f) * flutterAmount * 0.5f;
        transform.localPosition = startPosition + new Vector3(offsetX, offsetY, 0f);
    }
}
