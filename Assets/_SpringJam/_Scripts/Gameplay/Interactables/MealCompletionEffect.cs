using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class MealCompletionEffect : MonoBehaviour
{
    [Header("Cake boven hoofd")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private SpriteRenderer cakeSpriteRenderer;
    [SerializeField] private Vector3 spriteOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private float spriteDuration = 2.5f;
    [SerializeField] private float spriteBobAmplitude = 0.08f;
    [SerializeField] private float spriteBobSpeed = 3f;

    [Header("Victory overlay")]
    [SerializeField] private float fadeDelay = 2f;
    [SerializeField] private float fadeDuration = 1.2f;
    [SerializeField] private float holdDuration = 3f;
    [SerializeField] [TextArea] private string victoryText = "You saved spring!";
    [SerializeField] private string victorySubText = "The blossoms bloom again, thanks to you.";

    [Header("Scene")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private VisualElement fadeOverlay;
    private Label victoryLabel;
    private Label subLabel;
    private bool isPlaying;

    public void Play()
    {
        if (isPlaying) return;
        isPlaying = true;
        StartCoroutine(RunEffect());
    }

    private IEnumerator RunEffect()
    {
        if (cakeSpriteRenderer != null)
        {
            cakeSpriteRenderer.gameObject.SetActive(true);
            yield return BobSprite(spriteDuration);
            cakeSpriteRenderer.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(spriteDuration);
        }

        float extraDelay = fadeDelay - spriteDuration;
        if (extraDelay > 0f)
            yield return new WaitForSeconds(extraDelay);

        // Inject into the existing UIDocument that is already rendering correctly.
        yield return InjectIntoExistingPanel();

        if (victoryLabel != null) victoryLabel.text = victoryText;
        if (subLabel != null) subLabel.text = victorySubText;

        yield return FadeIn(fadeDuration);

        yield return new WaitForSeconds(holdDuration);

        foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad")
                Destroy(go);
        }

        SceneManager.LoadScene(mainMenuScene);
    }

    private IEnumerator InjectIntoExistingPanel()
    {
        // Find the UIDocument that is already active and rendering (the dialogue overlay).
        UIDocument doc = FindFirstObjectByType<UIDocument>();

        // Wait up to 2 seconds if it isn't ready yet.
        float waited = 0f;
        while ((doc == null || doc.rootVisualElement == null) && waited < 2f)
        {
            waited += Time.deltaTime;
            yield return null;
            if (doc == null)
                doc = FindFirstObjectByType<UIDocument>();
        }

        if (doc == null || doc.rootVisualElement == null)
        {
            Debug.LogWarning("[MealCompletionEffect] Could not find a UIDocument to inject into.");
            yield break;
        }

        VisualElement root = doc.rootVisualElement;

        fadeOverlay = new VisualElement();
        fadeOverlay.style.position = Position.Absolute;
        fadeOverlay.style.left = 0; fadeOverlay.style.top = 0;
        fadeOverlay.style.right = 0; fadeOverlay.style.bottom = 0;
        fadeOverlay.style.backgroundColor = new Color(0.063f, 0.031f, 0.141f, 0.95f);
        fadeOverlay.style.justifyContent = Justify.Center;
        fadeOverlay.style.alignItems = Align.Center;
        fadeOverlay.style.flexDirection = FlexDirection.Column;
        fadeOverlay.style.opacity = 0f;
        fadeOverlay.style.display = DisplayStyle.None;

        victoryLabel = new Label(victoryText);
        victoryLabel.style.color = new Color(1f, 0.90f, 0.71f, 1f);
        victoryLabel.style.fontSize = new Length(64, LengthUnit.Pixel);
        victoryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        victoryLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        victoryLabel.style.whiteSpace = WhiteSpace.Normal;
        victoryLabel.style.width = new Length(100, LengthUnit.Percent);
        victoryLabel.style.marginBottom = 24;
        victoryLabel.style.paddingLeft = 48; victoryLabel.style.paddingRight = 48;

        subLabel = new Label(victorySubText);
        subLabel.style.color = new Color(0.82f, 0.94f, 0.78f, 1f);
        subLabel.style.fontSize = new Length(32, LengthUnit.Pixel);
        subLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        subLabel.style.whiteSpace = WhiteSpace.Normal;
        subLabel.style.width = new Length(100, LengthUnit.Percent);
        subLabel.style.paddingLeft = 48; subLabel.style.paddingRight = 48;

        fadeOverlay.Add(victoryLabel);
        fadeOverlay.Add(subLabel);
        root.Add(fadeOverlay);
    }

    private IEnumerator BobSprite(float duration)
    {
        float elapsed = 0f;
        Transform spriteTransform = cakeSpriteRenderer != null ? cakeSpriteRenderer.transform : null;
        Transform anchor = playerTransform != null ? playerTransform : transform;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (spriteTransform != null)
            {
                float bob = Mathf.Sin(elapsed * spriteBobSpeed) * spriteBobAmplitude;
                spriteTransform.position = anchor.position + spriteOffset + new Vector3(0f, bob, 0f);
            }
            yield return null;
        }
    }

    private IEnumerator FadeIn(float duration)
    {
        if (fadeOverlay == null) yield break;

        fadeOverlay.style.display = DisplayStyle.Flex;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.style.opacity = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        fadeOverlay.style.opacity = 1f;
    }
}
