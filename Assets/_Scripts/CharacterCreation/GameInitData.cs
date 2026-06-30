namespace _Scripts.CharacterCreation
{
    // Carries the player's customization choices across scene loads.
    // Set before loading the gameplay scene; read in Awake to apply appearance.
    public static class GameInitData
    {
        public static CharacterCustomization Customization { get; private set; }

        public static void SetCustomization(CharacterCustomization customization)
        {
            Customization = customization;
        }

        public static bool HasCustomization => Customization != null;
    }
}
