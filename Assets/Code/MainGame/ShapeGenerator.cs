using Code.MainGame.Settings;
using UnityEngine;

namespace Code.MainGame
{
    public class ShapeGenerator {

        ShapeSettings settings;

        public ShapeGenerator(ShapeSettings settings)
        {
            this.settings = settings;
        }

        public Vector3 CalculatePointOnPlanet(Vector3 pointOnUnitSphere)
        {
            return pointOnUnitSphere * settings.planetRadius;
        }
    }
}