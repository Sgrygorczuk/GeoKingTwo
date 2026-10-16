using UnityEngine;
using UnityEngine.Serialization;

// When Geo passes through, Geo respawns here from now on.
public class CheckpointController : MonoBehaviour
{

    public SpriteRenderer spriteRenderer;
    [Tooltip("Where Geo respawns after reaching this checkpoint.")]
    [FormerlySerializedAs("checkpoint")]
    public Transform respawnPoint;
    public GeoController geo;
    public Color color;

    void Start()
    {
        spriteRenderer.color = color;
    }

    /**
    * Input: hitBox
    * Purpose: Geo entered the checkpoint: move its respawn point here
    */
    private void OnTriggerEnter2D(Collider2D hitBox)
    {
        if (hitBox.CompareTag($"Geo"))
        {
            geo.UpdateCheckpoint(respawnPoint.position);
        }
    }


}
