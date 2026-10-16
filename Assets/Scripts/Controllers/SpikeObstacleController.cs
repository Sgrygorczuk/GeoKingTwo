using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Square obstacle with a row of spikes on each side. Tick a side in the Inspector to turn its spikes on or off
// while editing. Every row is kept centered on its side and sticking out the same amount past the edge.
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpikeObstacleController : MonoBehaviour
{
    [Header("Sides with spikes")]
    public bool top = true;
    public bool bottom = true;
    public bool left = true;
    public bool right = true;

    [Header("Spike rows")]
    public Transform topSpikes;
    public Transform bottomSpikes;
    public Transform leftSpikes;
    public Transform rightSpikes;

    [Tooltip("How far the spike tips reach past the edge of the square, in the obstacle's local units.")]
    public float overhang = 0.1f;

    // Smaller moves than this are rounding noise; skipping them keeps prefab instances free of empty overrides.
    private const float Tolerance = 0.0001f;

#if UNITY_EDITOR
    // Changing objects inside OnValidate isn't allowed, so wait until the Inspector has finished.
    private void OnValidate()
    {
        EditorApplication.delayCall += Apply;
    }

    //Purpose: Show the ticked sides, hide the rest, and line every row up on its side
    [ContextMenu("Align Spikes")]
    private void Apply()
    {
        if (this == null) return; // deleted before the delayed call ran

        var square = GetComponent<SpriteRenderer>().sprite.bounds;
        ApplySide(topSpikes, top, Vector2.up, square);
        ApplySide(bottomSpikes, bottom, Vector2.down, square);
        ApplySide(leftSpikes, left, Vector2.left, square);
        ApplySide(rightSpikes, right, Vector2.right, square);
    }

    /**
    * Input: row, show, outward, square
    * Purpose: Turn one row on or off, then move it so it's centered along its side
    *          and its tips reach `overhang` past the edge facing `outward`
    */
    private void ApplySide(Transform row, bool show, Vector2 outward, Bounds square)
    {
        if (row == null) return;

        if (row.gameObject.activeSelf != show)
        {
            Undo.RecordObject(row.gameObject, "Toggle Spikes");
            row.gameObject.SetActive(show);
        }

        if (!RowBounds(row, out var spikes)) return;

        // Distance from the square's center to the outermost spike tip, now and where it should be
        var reach = Vector2.Dot(spikes.center - square.center, outward) + Mathf.Abs(Vector2.Dot(spikes.extents, outward));
        var wantedReach = Mathf.Abs(Vector2.Dot(square.extents, outward)) + overhang;

        // Direction along the side, and how far the row has drifted from the side's middle
        var along = new Vector2(Mathf.Abs(outward.y), Mathf.Abs(outward.x));
        var offCenter = Vector2.Dot(spikes.center - square.center, along);

        var move = outward * (wantedReach - reach) - along * offCenter;

        if (move.sqrMagnitude < Tolerance * Tolerance) return;
        Undo.RecordObject(row, "Align Spikes");
        row.localPosition += (Vector3)move;
    }

    /**
    * Input: row
    * Output: bounds of every sprite in the row, measured in this obstacle's local space
    * Purpose: Measure spikes without the padding world-space bounds add when the obstacle is rotated
    */
    private bool RowBounds(Transform row, out Bounds bounds)
    {
        bounds = default;
        var found = false;
        var toLocal = transform.worldToLocalMatrix;
        foreach (var sprite in row.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sprite.sprite == null) continue;
            var local = sprite.sprite.bounds;
            var toObstacle = toLocal * sprite.transform.localToWorldMatrix;
            for (var corner = 0; corner < 4; corner++)
            {
                var point = toObstacle.MultiplyPoint3x4(new Vector3(
                    corner % 2 == 0 ? local.min.x : local.max.x,
                    corner < 2 ? local.min.y : local.max.y));
                if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                else bounds.Encapsulate(point);
            }
        }
        return found;
    }
#endif
}
