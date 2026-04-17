using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum FlowerBedFeedbackState
{
    Dormant,
    Activated,
    Failed,
    Completed,
}

public sealed class FlowerBedInteractable : BaseInteractable
{
    [Header("Flower Bed")]
    [SerializeField] private string flowerId = "snowdrop";
    [SerializeField] private string displayName = "Snowdrop";
    [SerializeField] private string activatedInteractionText = "Blooming";
    [SerializeField] private string failedInteractionText = "Try Again";
    [SerializeField] private string completedInteractionText = "Bloomed";
    [SerializeField] private FlowerBloomPuzzleController puzzleController;
    [SerializeField] private bool useSpriteMode = false;

    [Header("Visual Feedback")]
    [SerializeField] private List<Renderer> feedbackRenderers = new List<Renderer>();
    [SerializeField] private Color dormantColor = new Color(0.48f, 0.62f, 0.44f, 1f);
    [SerializeField] private Color activatedColor = new Color(0.89f, 0.79f, 0.52f, 1f);
    [SerializeField] private Color failedColor = new Color(0.78f, 0.38f, 0.42f, 1f);
    [SerializeField] private Color completedColor = new Color(1f, 0.88f, 0.64f, 1f);
    [SerializeField] private float activatedScaleMultiplier = 1.06f;
    [SerializeField] private float failedScaleMultiplier = 0.94f;
    [SerializeField] private float completedScaleMultiplier = 1.12f;

    [Header("Visual Feedback sprite")]
    [SerializeField] private SpriteRenderer flowerSpriteRenderer;
    [SerializeField] private Sprite failedSprite;
    [SerializeField] private Sprite dormantSprite;
    [SerializeField] private Sprite activatedSprite;

    [SerializeField] private Sprite completedSprite;

    [Header("Events")]
    [SerializeField] private UnityEvent onActivated;
    [SerializeField] private UnityEvent onRejected;
    [SerializeField] private UnityEvent onCompleted;
    [SerializeField] private UnityEvent onReset;

    private MaterialPropertyBlock propertyBlock;
    private Vector3 initialLocalScale;
    private bool hasCachedVisualState;

    public string FlowerId => NormalizeId(flowerId);
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? FlowerId : displayName.Trim();
    public FlowerBedFeedbackState CurrentFeedbackState { get; private set; }
    public bool IsActivated => CurrentFeedbackState == FlowerBedFeedbackState.Activated || CurrentFeedbackState == FlowerBedFeedbackState.Completed;

    private void Awake()
    {
        EnsureVisualCache();
        ApplyVisualState();
    }

    private void OnEnable()
    {
        EnsureVisualCache();
        ApplyVisualState();
    }

    private void Reset()
    {
        TryAssignParentController();
        TryPopulateFeedbackRenderers();
    }

    private void OnValidate()
    {
        TryAssignParentController();
        TryPopulateFeedbackRenderers();
    }

    public override void Interact(PlayerInteractor interactor)
    {
        if (puzzleController == null)
        {
            Debug.LogWarning($"Flower bed '{DisplayName}' is missing its puzzle controller reference.", this);
            return;
        }

        puzzleController.TryActivateBed(this);
    }

    public override string GetInteractionText(PlayerInteractor interactor)
    {
        return CurrentFeedbackState switch
        {
            FlowerBedFeedbackState.Completed => string.IsNullOrWhiteSpace(completedInteractionText) ? "Bloomed" : completedInteractionText.Trim(),
            FlowerBedFeedbackState.Failed => string.IsNullOrWhiteSpace(failedInteractionText) ? "Try Again" : failedInteractionText.Trim(),
            FlowerBedFeedbackState.Activated => string.IsNullOrWhiteSpace(activatedInteractionText) ? "Blooming" : activatedInteractionText.Trim(),
            _ => base.GetInteractionText(interactor),
        };
    }

    internal void SetPuzzleController(FlowerBloomPuzzleController controller)
    {
        if (controller != null)
        {
            puzzleController = controller;
        }
    }

    internal void NotifyActivated()
    {
        if (CurrentFeedbackState == FlowerBedFeedbackState.Activated)
        {
            return;
        }

        SetFeedbackState(FlowerBedFeedbackState.Activated);
        onActivated?.Invoke();
    }

    internal void NotifyFailed()
    {
        SetFeedbackState(FlowerBedFeedbackState.Failed);
        onRejected?.Invoke();
    }

    internal void NotifyCompleted()
    {
        if (CurrentFeedbackState == FlowerBedFeedbackState.Completed)
        {
            return;
        }

        SetFeedbackState(FlowerBedFeedbackState.Completed);
        onCompleted?.Invoke();
    }

    internal void ResetState(bool invokeResetEvent)
    {
        SetFeedbackState(FlowerBedFeedbackState.Dormant);
        if (invokeResetEvent)
        {
            onReset?.Invoke();
        }
    }

    private void SetFeedbackState(FlowerBedFeedbackState feedbackState)
    {
        CurrentFeedbackState = feedbackState;
        ApplyVisualState();
    }

    private void EnsureVisualCache()
    {
        if (!hasCachedVisualState)
        {
            initialLocalScale = transform.localScale;
            hasCachedVisualState = true;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        TryPopulateFeedbackRenderers();
    }

    private void TryPopulateFeedbackRenderers()
    {
        if (feedbackRenderers != null && feedbackRenderers.Count > 0)
        {
            return;
        }

        feedbackRenderers = new List<Renderer>(GetComponentsInChildren<Renderer>(true));
    }

    private void ApplyVisualState()
    {
        EnsureVisualCache();

        if (useSpriteMode)
            ApplyVisualStateSprite();
        else
            ApplyVisualStateGrow();
    }

    private void ApplyVisualStateSprite()
    {
        EnsureVisualCache();

        if (flowerSpriteRenderer != null)
        {
            Sprite target = GetStateSprite(CurrentFeedbackState);
            if (target != null)
                flowerSpriteRenderer.sprite = target;
        }
    }

    private void ApplyVisualStateGrow()
    {
        EnsureVisualCache();

        transform.localScale = initialLocalScale * GetScaleMultiplier(CurrentFeedbackState);
        Color color = GetStateColor(CurrentFeedbackState);

        for (int i = 0; i < feedbackRenderers.Count; i++)
        {
            Renderer renderer = feedbackRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private Sprite GetStateSprite(FlowerBedFeedbackState feedbackState)
    {
        return feedbackState switch
        {
            FlowerBedFeedbackState.Activated => activatedSprite,
            FlowerBedFeedbackState.Failed => failedSprite,
            FlowerBedFeedbackState.Completed => completedSprite,
            _ => dormantSprite,
        };
    }

    private float GetScaleMultiplier(FlowerBedFeedbackState feedbackState)
    {
        return feedbackState switch
        {
            FlowerBedFeedbackState.Activated => activatedScaleMultiplier,
            FlowerBedFeedbackState.Failed => failedScaleMultiplier,
            FlowerBedFeedbackState.Completed => completedScaleMultiplier,
            _ => 1f,
        };
    }

    private Color GetStateColor(FlowerBedFeedbackState feedbackState)
    {
        return feedbackState switch
        {
            FlowerBedFeedbackState.Activated => activatedColor,
            FlowerBedFeedbackState.Failed => failedColor,
            FlowerBedFeedbackState.Completed => completedColor,
            _ => dormantColor,
        };
    }

    private void TryAssignParentController()
    {
        if (puzzleController == null)
        {
            puzzleController = GetComponentInParent<FlowerBloomPuzzleController>();
        }
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}

