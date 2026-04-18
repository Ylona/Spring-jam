using System.Collections;
using UnityEngine;

public class GrandmaWanderer : NPCWanderer
{
    [Header("Grandma Behaviour")]
    [SerializeField] private Transform pickupBasketWaypoint;
    [SerializeField] private Transform dropCherryWaypoint;
    [SerializeField] private Transform putDownBasketWaypoint;
    [SerializeField] private ItemInteractable basket;
    [SerializeField] private ItemInteractable cherry;
    [SerializeField] private Transform basketHoldAnchor;
    [SerializeField] private Transform cherryDropPoint;
    [SerializeField] private Transform basketDropPoint;

    private bool hasBasket = false;

    protected override void OnAwake()
    {
        hasBasket = false;
    }

    protected override IEnumerator OnArrivedAtWaypoint(Transform waypoint, int index)
    {
        if (waypoint == pickupBasketWaypoint && !hasBasket)
        {
            PickUpBasket();
        }
        else if (waypoint == dropCherryWaypoint && hasBasket)
        {
            DropCherry();
        }
        else if (waypoint == putDownBasketWaypoint && !hasBasket)
        {
            PutDownBasket();
        }

        yield break;
    }

    private void PickUpBasket()
    {
        if (basket == null) return;

        Transform anchor = basketHoldAnchor != null ? basketHoldAnchor : transform;
        basket.transform.SetParent(anchor, false);
        basket.transform.localPosition = Vector3.zero;
        basket.transform.localRotation = Quaternion.identity;
        hasBasket = true;
    }

    private void PutDownBasket()
    {
        if (basket == null) return;

        Vector3 dropPosition = basketDropPoint != null ? basketDropPoint.position : transform.position;
        basket.DropToWorld(dropPosition);
        basket.gameObject.SetActive(true);
    }

    private void DropCherry()
    {
        if (cherry == null) return;

        Vector3 dropPosition = cherryDropPoint != null ? cherryDropPoint.position : transform.position;
        cherry.DropToWorld(dropPosition);

        SpriteRenderer sr = cherry.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;

        Collider2D col = cherry.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        hasBasket = false;
    }

    public override void OnLoopReset()
    {
        if (hasBasket && basket != null)
            basket.transform.SetParent(null, true);

        hasBasket = false;
        base.OnLoopReset();
    }
}
