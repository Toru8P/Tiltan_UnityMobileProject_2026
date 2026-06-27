using UnityEngine;

namespace _Scripts.MainGame.UI
{
    public static class ScoreFormatter
    {
        public static string FormatScore(double score)
        {
            if (score >= 1000000000)
                return (score / 1000000000D).ToString("0.0") + "B";
            if (score >= 1000000)
                return (score / 1000000D).ToString("0.0") + "M";
            if (score >= 1000)
                return (score / 1000D).ToString("0.0") + "K";
            
            return ((int)score).ToString();
        }
    }
}
