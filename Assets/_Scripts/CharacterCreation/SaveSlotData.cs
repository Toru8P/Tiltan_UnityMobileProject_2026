using System;

namespace _Scripts.CharacterCreation
{
    [Serializable]
    public class SaveSlotData
    {
        public int slotIndex;
        public string characterName;
        public float playtimeSeconds;
        public string saveDate;          // ISO 8601 string
        public string thumbnailFileName; // relative to persistentDataPath
        public string sceneName;         // scene to load when resuming

        // Convenience for display
        public string FormattedPlaytime()
        {
            var ts = TimeSpan.FromSeconds(playtimeSeconds);
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        public string FormattedDate()
        {
            if (DateTime.TryParse(saveDate, out var dt))
                return dt.ToString("yyyy-MM-dd  HH:mm");
            return saveDate;
        }
    }
}
