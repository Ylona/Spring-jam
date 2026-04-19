using System;
using System.Collections.Generic;
using SpringJam.Systems.DayLoop;
using SpringJam2026.Audio;
using SpringJam2026.Utils;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class PrepTableIngredientRequirement
{
    [SerializeField] private string requiredItemId = string.Empty;
    [SerializeField] private ItemSocketInteractable socket;

    public PrepTableIngredientRequirement(string requiredItemId)
    {
        this.requiredItemId = requiredItemId ?? string.Empty;
    }

    public PrepTableIngredientRequirement(string requiredItemId, ItemSocketInteractable socket)
        : this(requiredItemId)
    {
        this.socket = socket;
    }

    public string RequiredItemId => NormalizeId(requiredItemId);
    public ItemSocketInteractable Socket => socket;
    public ItemInteractable PlacedItem => socket != null ? socket.PlacedItem : null;
    public bool IsSatisfied => PlacedItem != null && PlacedItem.ItemId == RequiredItemId;

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}

[DisallowMultipleComponent]
public sealed class PrepTableController : MonoBehaviour
{
    [SerializeField] private string taskId = "cook-spring-meal";
    [SerializeField]
    private List<PrepTableIngredientRequirement> ingredientSockets = new List<PrepTableIngredientRequirement>
    {
        new PrepTableIngredientRequirement("cherry"),
        new PrepTableIngredientRequirement("honey-jar"),
        new PrepTableIngredientRequirement("mint-bundle"),
        new PrepTableIngredientRequirement("blossom-petals"),
    };

    [Header("Completion Effect")]
    [SerializeField] private MealCompletionEffect completionEffect;

    [Header("Events")]
    [SerializeField] private UnityEvent onIngredientStateChanged;
    [SerializeField] private UnityEvent onMealCompleted;

    private readonly List<ItemSocketInteractable> subscribedSockets = new List<ItemSocketInteractable>();
    private DayLoopRuntime subscribedRuntime;
    private bool hasCompletedMeal;

    public IReadOnlyList<PrepTableIngredientRequirement> IngredientSockets => ingredientSockets;
    public bool HasAllIngredients => AreAllIngredientsPlaced();

    private void OnEnable()
    {
        TrySubscribeRuntime();
        RefreshSocketSubscriptions();
        EvaluateCompletion();
    }

    private void Start()
    {
        TrySubscribeRuntime();
        RefreshSocketSubscriptions();
        EvaluateCompletion();
    }

    private void OnDisable()
    {
        UnsubscribeRuntime();
        UnsubscribeSockets();
    }

    public bool EvaluateCompletion()
    {
        RefreshSocketSubscriptions();
        onIngredientStateChanged?.Invoke();

        if (!AreAllIngredientsPlaced())
        {
            return false;
        }

        return TryCompleteMeal();
    }

    private bool AreAllIngredientsPlaced()
    {
        if (ingredientSockets == null || ingredientSockets.Count == 0)
        {
            return false;
        }

        foreach (PrepTableIngredientRequirement ingredientSocket in ingredientSockets)
        {
            if (ingredientSocket == null || !ingredientSocket.IsSatisfied)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryCompleteMeal()
    {
        if (hasCompletedMeal)
        {
            return false;
        }

        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        string normalizedTaskId = NormalizeId(taskId);
        if (runtime == null
            || string.IsNullOrEmpty(normalizedTaskId)
            || runtime.CurrentPhase != DayLoopPhase.ActiveDay
            || !runtime.TryGetTask(normalizedTaskId, out DayLoopTaskSnapshot taskSnapshot)
            || !taskSnapshot.IsUnlocked
            || taskSnapshot.IsCompleted)
        {
            return false;
        }

        hasCompletedMeal = true;
        if (!runtime.TryCompleteTask(normalizedTaskId))
        {
            hasCompletedMeal = false;
            return false;
        }

        PlayCompletionAudio();
        completionEffect?.Play();
        onMealCompleted?.Invoke();
        return true;
    }

    private void HandleSocketChanged(ItemSocketInteractable _)
    {
        EvaluateCompletion();
    }

    private void HandleTaskChanged(DayLoopTaskSnapshot _)
    {
        EvaluateCompletion();
    }

    private void HandleLoopStarted(DayLoopSnapshot _)
    {
        hasCompletedMeal = false;
        EvaluateCompletion();
    }

    private void TrySubscribeRuntime()
    {
        DayLoopRuntime runtime = DayLoopRuntime.Instance;
        if (runtime == null || subscribedRuntime == runtime)
        {
            return;
        }

        UnsubscribeRuntime();
        subscribedRuntime = runtime;
        subscribedRuntime.TaskChanged += HandleTaskChanged;
        subscribedRuntime.LoopStarted += HandleLoopStarted;
    }

    private void UnsubscribeRuntime()
    {
        if (subscribedRuntime == null)
        {
            return;
        }

        subscribedRuntime.TaskChanged -= HandleTaskChanged;
        subscribedRuntime.LoopStarted -= HandleLoopStarted;
        subscribedRuntime = null;
    }

    private void RefreshSocketSubscriptions()
    {
        UnsubscribeSockets();

        if (ingredientSockets == null)
        {
            return;
        }

        foreach (PrepTableIngredientRequirement ingredientSocket in ingredientSockets)
        {
            ItemSocketInteractable socket = ingredientSocket?.Socket;
            if (socket == null || subscribedSockets.Contains(socket))
            {
                continue;
            }

            socket.ItemPlaced += HandleSocketChanged;
            socket.ItemCleared += HandleSocketChanged;
            subscribedSockets.Add(socket);
        }
    }

    private void UnsubscribeSockets()
    {
        foreach (ItemSocketInteractable socket in subscribedSockets)
        {
            if (socket == null)
            {
                continue;
            }

            socket.ItemPlaced -= HandleSocketChanged;
            socket.ItemCleared -= HandleSocketChanged;
        }

        subscribedSockets.Clear();
    }

    private static void PlayCompletionAudio()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (ServiceLocator.TryGet<AudioService>(out AudioService audioService))
        {
            audioService.PlayPrepTable();
        }
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
