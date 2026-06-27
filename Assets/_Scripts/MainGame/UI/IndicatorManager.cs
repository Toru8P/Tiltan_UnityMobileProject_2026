using UnityEngine;

namespace _Scripts.MainGame.UI
{
    public class IndicatorManager : MonoBehaviour
    {
        public static IndicatorManager Instance { get; private set; }

        [SerializeField] private GameObject indicatorPrefab;

        [Header("Colors")]
        [SerializeField] private Color zombieDamageColor = Color.white;
        [SerializeField] private Color playerDamageColor = Color.red;
        [SerializeField] private Color pointsColor = Color.yellow;
        [SerializeField] private Color healingColor = Color.green;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SpawnDamageZombie(Vector3 position, int amount)
        {
            Spawn(position, amount.ToString(), zombieDamageColor);
        }

        public void SpawnDamagePlayer(Vector3 position, int amount)
        {
            Spawn(position, amount.ToString(), playerDamageColor);
        }

        public void SpawnPoints(Vector3 position, double amount)
        {
            Spawn(position, "+" + amount.ToString("F0"), pointsColor);
        }

        public void SpawnHealing(Vector3 position, int amount)
        {
            if (amount <= 0) return;
            Spawn(position, "+" + amount.ToString(), healingColor);
        }

        private void Spawn(Vector3 position, string text, Color color)
        {
            if (indicatorPrefab == null) return;

            // Randomize position slightly to avoid overlap
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), 1.5f, Random.Range(-0.5f, 0.5f));
            GameObject go = Instantiate(indicatorPrefab, position + offset, Quaternion.identity);
            FloatingIndicator indicator = go.GetComponent<FloatingIndicator>();
            if (indicator != null)
            {
                indicator.Setup(text, color);
            }
        }
    }
}
