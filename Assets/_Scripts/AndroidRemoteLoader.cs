using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AndroidRemoteLoader : MonoBehaviour
{
    [SerializeField] private string assetKey = "MyAndroidPrefab";
    [SerializeField] private Transform parentTransform;
    
    private AsyncOperationHandle<GameObject> loadHandle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async Task Start()
    {
        await LoadRemoteAsset();
    }

    // Update is called once per frame
    async Task LoadRemoteAsset()
    {
        try
        {
            Debug.Log($"Loading remote asset: {assetKey}");
            
            loadHandle = Addressables.LoadAssetAsync<GameObject>(assetKey);
            
            GameObject prefab = await loadHandle.Task;
            
            if (prefab != null)
            {
                Transform spawnParent = parentTransform != null ? parentTransform : transform;
                Instantiate(prefab, spawnParent);
                Debug.Log($"Successfully loaded and instantiated: {assetKey}");
            }
            else
            {
                Debug.LogError($"Failed to load asset: {assetKey} - Asset is null");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading remote asset '{assetKey}': {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        if (loadHandle.IsValid())
        {
            Addressables.Release(loadHandle);
        }
    }
}
