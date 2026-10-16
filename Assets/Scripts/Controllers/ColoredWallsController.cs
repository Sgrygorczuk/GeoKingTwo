using System.Collections.Generic;
using UnityEngine;

// A colored wall is solid unless Geo is the same color as it. The walls are collected when the level starts from
// the bucket objects (e.g. the "Red", "Blue", "Green" parents), so new walls only need to be placed in a bucket.
public class ColoredWallsController : MonoBehaviour
{
    [Tooltip("Parent objects holding the colored walls; every wall (BoxCollider2D + sprite) inside them is managed.")]
    public Transform[] wallBuckets;
    public GeoController geo;

    private readonly List<BoxCollider2D> _colliders = new List<BoxCollider2D>();
    private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();

    private void Start()
    {
        foreach (var bucket in wallBuckets)
        {
            if (bucket == null) { continue; }
            // Include inactive walls too, so walls switched on later are still managed
            foreach (var wallCollider in bucket.GetComponentsInChildren<BoxCollider2D>(true))
            {
                var wallRenderer = wallCollider.GetComponent<SpriteRenderer>();
                if (wallRenderer == null) { continue; }
                _colliders.Add(wallCollider);
                _renderers.Add(wallRenderer);
            }
        }
    }

    void Update()
    {
        // A wall is solid unless Geo is the same color as it
        for (var i = 0; i < _colliders.Count; i++)
        {
            _colliders[i].enabled = _renderers[i].color != geo.spriteRenderer.color;
        }
    }
}
