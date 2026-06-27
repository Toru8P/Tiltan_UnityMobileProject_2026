using _Scripts.MainGame.Inventory;
using UnityEngine;

namespace _Scripts.MainGame.Player
{
    public class PlayerEquipment : MonoBehaviour
    {
        [SerializeField] private Transform rightHandMount;
        [SerializeField] private Transform elbowR;
        [SerializeField] private Vector3 holdElbowRotation = new Vector3(0, 0, -45);

        [Header("Item Tweak Tools")]
        [Tooltip("If checked, the item won't snap to its saved position, allowing you to move it manually.")]
        public bool unlockItemForEditing = false;
    
        private ItemData currentItemData;
        public ItemData CurrentItem => currentItemData;
        private GameObject currentHeldItem;
        private HotbarUI hotbar;

        public event System.Action<ItemData> OnItemEquipped;

        private void Start()
        {
            hotbar = Object.FindAnyObjectByType<HotbarUI>();
            if (hotbar != null) hotbar.OnItemSelected += EquipItem;
        }

        private void Update()
        {
            if (Application.isPlaying && currentHeldItem != null && currentItemData != null)
            {
                // If editing is UNLOCKED, we let you move it. 
                // If LOCKED, we force it to the saved offset.
                if (!unlockItemForEditing)
                {
                    currentHeldItem.transform.localPosition = currentItemData.holdPosition;
                    currentHeldItem.transform.localRotation = Quaternion.Euler(currentItemData.holdRotation);
                }
            }
        }

        [ContextMenu("1. Select Held Item")]
        public void SelectItem()
        {
            if (currentHeldItem != null) {
#if UNITY_EDITOR
                UnityEditor.Selection.activeObject = currentHeldItem; // Use UnityEditor.Selection to select in hierarchy
#endif
            }
        }

        [ContextMenu("2. Save Item Offset to Asset")]
        public void SaveItemSettings()
        {
            if (currentHeldItem == null || currentItemData == null) return;

            // Capture exactly where the item is relative to the hand
            currentItemData.holdPosition = currentHeldItem.transform.localPosition;
            currentItemData.holdRotation = currentHeldItem.transform.localEulerAngles;
        
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(currentItemData);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"<b>[SUCCESS]</b> Saved hand-offset for {currentItemData.displayName}.");
#endif
        }

        private void EquipItem(ItemData item)
        {
            currentItemData = item;
            if (currentHeldItem != null) Destroy(currentHeldItem);
            if (item != null && item.heldPrefab != null && rightHandMount != null)
            {
                currentHeldItem = Instantiate(item.heldPrefab, rightHandMount);
                currentHeldItem.transform.localPosition = item.holdPosition;
                currentHeldItem.transform.localRotation = Quaternion.Euler(item.holdRotation);
            }
            OnItemEquipped?.Invoke(item);
        }
    }
}
