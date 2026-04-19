using System.Collections;
using SpringJam2026.Audio;
using SpringJam2026.Utils;
using UnityEngine;

public class BunnyWanderer : NPCWanderer, IGameService
{
    public int Priority => 45;
    
    [Header("Cherry Eating")]
    [SerializeField] private string cherryItemId = "cherry";
    [SerializeField] private float eatDuration = 1f;
    
    [Header("Audio")]
    [SerializeField] private float hopInterval = 0.2f;
    [SerializeField] private float movementThreshold = 0.001f;

    private AudioService audioService;
    private Vector3 lastPosition;
    private float hopTimer;
    
    public void Initialize()
    {
        lastPosition = transform.position;
        audioService = ServiceLocator.Get<AudioService>();
    }
    
    public void Bind()
    {
        // Silence is golden
    }
    
    private void Update()
    {
        HandleHopAudio();
    }

    private void HandleHopAudio()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        bool isMoving = distanceMoved > movementThreshold;

        if (isMoving)
        {
            hopTimer += Time.deltaTime;

            if (hopTimer >= hopInterval)
            {
                hopTimer = 0f;
                audioService.PlayBunnyHop(transform.position);
            }
        }
        else
        {
            hopTimer = 0f;
        }

        lastPosition = transform.position;
    }

    protected override IEnumerator OnArrivedAtWaypoint(Transform waypoint, int index)
    {
        ItemInteractable cherry = FindCherryAt(waypoint.position);
        if (cherry == null) yield break;

        yield return new WaitForSeconds(eatDuration);

        EatCherry(cherry);
    }

    private void EatCherry(ItemInteractable cherry)
    {
        // Hide visuals but keep the GameObject active so ItemInteractable receives the LoopStarted event and resets itself.
        foreach (SpriteRenderer sr in cherry.GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = false;
        foreach (Collider col in cherry.GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    private ItemInteractable FindCherryAt(Vector3 position)
    {
        Collider[] nearby = Physics.OverlapSphere(position, ArrivalDistance * 2f);
        foreach (Collider col in nearby)
        {
            ItemInteractable item = col.GetComponent<ItemInteractable>();
            if (item != null && item.ItemId == cherryItemId && item.gameObject.activeSelf)
                return item;
        }

        return null;
    }
}
