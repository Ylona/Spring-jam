using SpringJam.Systems.DayLoop;
using UnityEngine;

// Add this to any GameObject that gets visually hidden during gameplay and needs to reappear on loop reset.
public class LoopResetVisibility : MonoBehaviour, ILoopResetListener
{
    private SpriteRenderer[] spriteRenderers;
    private BoxCollider[] colliders;

    private void Awake()
    {
        spriteRenderers = GetComponents<SpriteRenderer>();
        colliders = GetComponents<BoxCollider>();

        SetVisible(false);
    }

    public void OnLoopReset()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        foreach (SpriteRenderer sr in spriteRenderers)
            sr.enabled = visible;

        foreach (BoxCollider col in colliders)
            col.enabled = visible;
    }
}
