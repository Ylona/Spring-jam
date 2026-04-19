using System.Collections;
using SpringJam.Systems.DayLoop;
using UnityEngine;

public class BunnyWanderer : NPCWanderer
{
    [Header("Cherry Eating")]
    [SerializeField] private string cherryItemId = "cherry";
    [SerializeField] private float eatDuration = 1f;

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

        DayLoopRuntime.Instance?.TryLearnKnowledge("cherry-eaten");
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
