using UnityEngine;

// Tints a foreground sprite with the color set in the Inspector when the level starts.
public class ForegroundView : MonoBehaviour
{

    public SpriteRenderer spriteRenderer;
    public Color color;

    void Start()
    {
        spriteRenderer.color = color;
    }
}
