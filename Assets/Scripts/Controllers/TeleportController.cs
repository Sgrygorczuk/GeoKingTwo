using UnityEngine;

// One end of a teleporter: when Geo leaves this trigger moving the right way, it is moved to the exit.
// Checking the direction lets Geo pass back out the way it came without teleporting.
public class TeleportController : MonoBehaviour
{
    [Tooltip("Where Geo comes out.")]
    public Transform exit;
    public GeoController geo;
    [Tooltip("Which way Geo must be moving to teleport. Vertical: on = falling, off = rising. " +
             "Horizontal: on = moving right, off = moving left.")]
    public bool isInOne;
    [Tooltip("On: the teleporter is entered from above/below. Off: from the side.")]
    public bool isVertical;

    /**
    * Input: hitBox
    * Purpose: Geo left the trigger: teleport it if it was moving in the entry direction
    */
    private void OnTriggerExit2D(Collider2D hitBox)
    {
        var position = exit.position;

        if (hitBox.CompareTag($"Geo"))
        {
            if (isVertical)
            {
                if ((isInOne && geo.GetVelocity().y < 0) || (!isInOne && geo.GetVelocity().y > 0))
                {
                    geo.Teleport(position.x, position.y);
                }
            }
            else
            {
                if ((isInOne && geo.GetVelocity().x > 0) || (!isInOne && geo.GetVelocity().x < 0))
                {
                    geo.Teleport(position.x, position.y);
                }
            }
        }
    }

}
