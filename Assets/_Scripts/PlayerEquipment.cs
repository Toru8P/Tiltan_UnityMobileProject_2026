using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] private Transform rightHandMount;
    [SerializeField] private Transform elbowR;
    [SerializeField] private Vector3 holdElbowRotation = new Vector3(0, 0, -45); // Adjust for a "holding" pose
    
    private GameObject currentHeldItem;
    private HotbarUI hotbar;
    private bool isHolding;

    private void Start()
    {
        hotbar = Object.FindAnyObjectByType<HotbarUI>();
        if (hotbar != null)
        {
            hotbar.OnItemSelected += EquipItem;
        }
    }

    private void LateUpdate()
    {
        if (isHolding && elbowR != null)
        {
            // Apply a simple rotation to the elbow to make the arm look like it's holding something
            elbowR.localRotation *= Quaternion.Euler(holdElbowRotation);
        }
    }

    private void EquipItem(ItemData item)
    {
        if (currentHeldItem != null)
        {
            Destroy(currentHeldItem);
        }

        isHolding = (item != null && item.heldPrefab != null);

        if (isHolding && rightHandMount != null)
        {
            currentHeldItem = Instantiate(item.heldPrefab, rightHandMount);
            currentHeldItem.transform.localPosition = Vector3.zero;
            currentHeldItem.transform.localRotation = Quaternion.Euler(item.holdRotation);
        }
    }
}
