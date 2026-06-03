using UnityEngine;

namespace _Scripts.MainGame.Terrain
{
    [System.Serializable]
    public struct NoiseSettings
    {
        public float scale;          // e.g. 20f
        public float amplitude;      // e.g. 5f
        [Range(1, 8)] public int octaves;
        [Range(0f, 1f)] public float persistence;
        public float lacunarity;     // e.g. 2f
        public int seed;

        public static NoiseSettings Default => new NoiseSettings
        {
            scale = 20f,
            amplitude = 5f,
            octaves = 4,
            persistence = 0.5f,
            lacunarity = 2f,
            seed = 12345
        };
    }

    public static class NoiseGenerator
    {
        // Returns terrain height in world units.
        public static float SampleHeight(float worldX, float worldZ, NoiseSettings settings)
        {
            float scale = Mathf.Max(0.0001f, settings.scale);
            int octaves = Mathf.Max(1, settings.octaves);

            float amplitude = 1f;
            float frequency = 1f;
            float sum = 0f;
            float norm = 0f;

            float seedOffsetX = (settings.seed * 0.12345f) % 10000f;
            float seedOffsetZ = (settings.seed * 0.54321f) % 10000f;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = (worldX + seedOffsetX) / scale * frequency;
                float sampleZ = (worldZ + seedOffsetZ) / scale * frequency;

                float perlin = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f; // [-1,1]
                sum += perlin * amplitude;
                norm += amplitude;

                amplitude *= settings.persistence;
                frequency *= settings.lacunarity;
            }

            if (norm <= 0f) return 0f;
            float normalized = sum / norm; // roughly [-1,1]
            return normalized * settings.amplitude;
        }
    }
}