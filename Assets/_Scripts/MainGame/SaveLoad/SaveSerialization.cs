using Newtonsoft.Json;

namespace _Scripts.MainGame.SaveLoad
{
    // Shared Newtonsoft settings for game-state (world) saves. Same library as SaveSlotManager,
    // plus compact Unity Vector3/Quaternion handling.
    public static class SaveSerialization
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = { new Vector3Converter(), new QuaternionConverter() }
        };

        public static string Serialize(object value) => JsonConvert.SerializeObject(value, Settings);

        public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);
    }
}
