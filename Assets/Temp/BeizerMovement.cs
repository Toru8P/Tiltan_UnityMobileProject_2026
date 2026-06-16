using UnityEngine;

public class BeizerMovement : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public Transform pointC;

    public float duration = 2f;

    private float t;
    
    // Update is called once per frame
    void Update()
    {
        if (t < 1f)
        {
            t += Time.deltaTime / duration;

            float easedT = EaseInOut(t);
            
            Vector3 position = CalculateQuadraticBezier(pointA.position, pointB.position, pointC.position, easedT);
            
            transform.position = position;
        }
    }

    Vector3 CalculateQuadraticBezier(Vector3 a, Vector3 c, Vector3 b, float t)
    {
        float u = 1f - t;
        return u * u * a + 2 * u * t * c + t * t * b;
    }

    private float EaseInOut(float x)
    {
        return x * x * (3f - 2f * x);
    }
}
