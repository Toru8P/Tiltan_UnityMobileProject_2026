using _Scripts.MainGame.SaveLoad;
using UnityEngine;
using TerrainSystem = _Scripts.MainGame.Terrain.Terrain;

namespace _Scripts.MainGame.Player
{
    // Restores the player's position/facing on load, and re-saves it whenever the player crosses into
    // a new chunk — piggy-backing on Terrain's chunk-crossing event, the same moment the world is saved.
    public class PlayerSaveController : MonoBehaviour
    {
        private TerrainSystem _terrain;

        private void Start()
        {
            _terrain = FindAnyObjectByType<TerrainSystem>();

            // Restore saved position for this slot (if any). Terrain runs first (execution order),
            // so it's already generated/loaded here — re-stream chunks around the restored position
            // so the player doesn't land over an inactive chunk and fall through.
            SaveLoadManager save = SaveOrNull();
            if (save != null && save.HasSaveFile && save.Current.hasPlayer)
            {
                PlayerSaveData p = save.Current.player;
                PlayerStatsController stats = GetComponent<PlayerStatsController>();
                if (stats != null) stats.ReadSaveData(p);

            }

            // Stage position on every chunk crossing; Terrain persists the whole save right after.
            if (_terrain != null) _terrain.PlayerChangedChunk += StagePosition;
        }

        private void OnDestroy()
        {
            if (_terrain != null) _terrain.PlayerChangedChunk -= StagePosition;
        }

        // Writes the current position into the shared save. The actual disk write is done by Terrain's
        // SaveToFile, which fires right after this on the same chunk crossing.
        private void StagePosition()
        {
            SaveLoadManager save = SaveOrNull();
            if (save == null) return;

            save.Current.player.position = transform.position;
            save.Current.player.rotationY = transform.eulerAngles.y;
            save.Current.hasPlayer = true;
        }

        // Shared save manager, or null if this scene isn't wired for saving.
        private static SaveLoadManager SaveOrNull()
        {
            if (SingletonPoint.Instance == null || SingletonPoint.Instance.SaveLoad == null)
                return null;
            return SingletonPoint.Instance.SaveLoad;
        }
    }
}
