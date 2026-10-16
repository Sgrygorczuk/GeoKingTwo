using UnityEngine;

// Color pickup: when Geo passes through, Geo turns this color (walls of the same color become passable).
public class ColorSpawnController : MonoBehaviour
{
    public GeoController geo;
    [Tooltip("The color Geo turns when it touches this pickup.")]
    public Color color;


    /**
    * Input: hitBox
    * Purpose: Geo entered the pickup: recolor Geo
    */
    private void OnTriggerEnter2D(Collider2D hitBox)
    {
        if (hitBox.CompareTag($"Geo"))
        {
            geo.UpdateColor(color);
        }
    }

}
