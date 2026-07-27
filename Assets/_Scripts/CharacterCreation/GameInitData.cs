using UnityEngine;

namespace _Scripts.CharacterCreation
{
    public static class GameInitData
    {
        private const string SkinColorKey = "character_skin_color_index";
        private const string OutfitColorKey = "character_outfit_color_index";
        private const string PlayerNameKey = "character_name";
        private const string CharacterIdKey = "character_id";

        public static CharacterCustomization Customization { get; private set; }

        public static void SetCustomization(CharacterCustomization customization)
        {
            Customization = customization;
            if (customization == null) return;

            PlayerPrefs.SetInt(SkinColorKey, customization.SkinColorIndex);
            PlayerPrefs.SetInt(OutfitColorKey, customization.OutfitColorIndex);
            PlayerPrefs.SetString(PlayerNameKey, customization.PlayerName ?? "Hero");
            PlayerPrefs.SetString(CharacterIdKey, customization.CharacterId ?? "HumanMale_Character_FREE");
            PlayerPrefs.Save();
        }

        public static CharacterCustomization GetCustomizationOrSaved()
        {
            if (Customization != null) return Customization;
            if (!PlayerPrefs.HasKey(SkinColorKey) && !PlayerPrefs.HasKey(OutfitColorKey)) return null;

            Customization = new CharacterCustomization
            {
                CharacterId = PlayerPrefs.GetString(CharacterIdKey, "HumanMale_Character_FREE"),
                PlayerName = PlayerPrefs.GetString(PlayerNameKey, "Hero"),
                SkinColorIndex = PlayerPrefs.GetInt(SkinColorKey, 0),
                OutfitColorIndex = PlayerPrefs.GetInt(OutfitColorKey, 0)
            };
            return Customization;
        }

        public static bool HasCustomization => GetCustomizationOrSaved() != null;
    }
}
