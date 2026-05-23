using UnityEngine;

namespace Code.MainGame
{
    public interface INoiseFilter {

        float Evaluate(Vector3 point);
    }
}