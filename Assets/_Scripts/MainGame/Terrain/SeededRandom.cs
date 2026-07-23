namespace _Scripts.MainGame.Terrain
{
    // A deterministic random number generator with the same call-style as UnityEngine.Random,
    // but backed by System.Random so it never touches (or is disturbed by) Unity's global RNG.
    // Feed it the same seed and you get the exact same sequence every run.
    public class SeededRandom
    {
        private readonly System.Random _rng;

        public SeededRandom(int seed)
        {
            _rng = new System.Random(seed);
        }

        // Matches UnityEngine.Random.Range(int, int): min inclusive, max exclusive.
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return _rng.Next(minInclusive, maxExclusive);
        }

        // Matches UnityEngine.Random.Range(float, float): min inclusive, max inclusive-ish.
        public float Range(float min, float max)
        {
            return min + (float)_rng.NextDouble() * (max - min);
        }
    }

    public static class SeedUtility
    {
        // Combines a world seed with a chunk's grid coordinates into a stable, well-mixed seed.
        // Same (worldSeed, row, col) always yields the same value, independent of generation order.
        public static int Combine(int worldSeed, int row, int col)
        {
            unchecked
            {
                uint hash = (uint)worldSeed;
                hash = (hash ^ (uint)row) * 2654435761u;   // Knuth's multiplicative hash
                hash = (hash ^ (uint)col) * 2654435761u;
                hash ^= hash >> 15;
                return (int)hash;
            }
        }
    }
}
