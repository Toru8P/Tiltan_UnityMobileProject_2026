using System;

namespace _Scripts.CharacterCreation
{
    [Serializable]
    public class SaveSlotData
    {
        public int slotIndex;
        public string characterId;
        public string characterName;
        public float playtimeSeconds;
        public string saveDate;
        public string thumbnailFileName;
        public string sceneName;
        public int skinColorIndex;
        public int outfitColorIndex;

        public string FormattedPlaytime()
        {
            TimeSpan ts = TimeSpan.FromSeconds(playtimeSeconds);
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        public string FormattedDate()
        {
            return DateTime.TryParse(saveDate, out DateTime dt)
                ? dt.ToString("yyyy-MM-dd  HH:mm")
                : saveDate;
        }
    }
}
