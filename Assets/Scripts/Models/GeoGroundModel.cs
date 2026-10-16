using UnityEngine;

// Everything Geo knows about the ground around it: grounded/side checks, which contacts are floor or slope,
// whether there's flat ground underneath (for slope rolling) and whether a wall is in the way of the cube.
// GeoController decides what to do with it.
[RequireComponent(typeof(Rigidbody2D))]
public class GeoGroundModel : MonoBehaviour
{
    //================================================= Layers  ========================================================
    [Tooltip("Everything Geo stands on or is blocked by. Should include the Slope layer.")]
    public LayerMask whatIsGround;
    [Tooltip("Layers that count as slopes: touching one from above turns Geo into a rolling ball.")]
    public LayerMask whatIsSlope;

    //================================================= Foot / Side Checks  ============================================
    public float checkRadius = 0.1f;
    public Transform leftGroundCheck;
    public Transform rightGroundCheck;
    public Transform leftCheck;
    public Transform rightCheck;

    //================================================= Surface Angles  ================================================
    [Tooltip("Surfaces whose normal points up at least this much count as floor or slope (0 = wall, 1 = flat).")]
    [Range(0f, 1f)] public float minFloorNormalY = 0.2f;
    [Tooltip("Surfaces at least this flat count as flat floor.")]
    [Range(0f, 1f)] public float flatNormalY = 0.9f;

    //================================================= Casts  =========================================================
    [Tooltip("How far below Geo's bottom something still counts as holding Geo up (grounded).")]
    [Min(0f)] public float supportCheckDistance = 0.05f;
    [Tooltip("How far below Geo's bottom the flat-ground cast reaches. Flat ground within it blocks rolling.")]
    [Min(0f)] public float flatCastDistance = 0.1f;
    [Tooltip("How far past the cube's sides to look for inclined walls/slopes before moving.")]
    [Min(0f)] public float sideProbeDistance = 0.05f;

    private Rigidbody2D _rigidbody2D;
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];

    public bool IsGrounded { get; private set; }
    public bool IsTouchingLeft { get; private set; }
    public bool IsTouchingRight { get; private set; }

    // Contacts reported by collision callbacks this physics step; cleared by ClearStep()
    public bool TouchingFloor { get; private set; }
    public Vector2 FloorNormal { get; private set; }
    public bool HasSlopeContact { get; private set; }
    public Vector2 SlopeContactPoint { get; private set; }
    public Vector2 SlopeContactNormal { get; private set; }
    public bool OnFlatFloor => TouchingFloor && IsFlat(FloorNormal);

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    public bool IsFlat(Vector2 normal) => normal.y >= flatNormalY;

    public bool IsSlope(Collider2D other) => (whatIsSlope.value & (1 << other.gameObject.layer)) != 0;

    //Purpose: Refresh the grounded and side checks for the collider Geo is currently using
    public void UpdateChecks(Collider2D activeCollider)
    {
        // The foot checks miss when Geo hangs off a corner with only a sliver of its bottom on the platform, so
        // also count anything directly supporting Geo's full bottom edge as ground
        IsGrounded = Physics2D.OverlapCircle(leftGroundCheck.position, checkRadius / 2f, whatIsGround) ||
                     Physics2D.OverlapCircle(rightGroundCheck.position, checkRadius / 2f, whatIsGround) ||
                     IsSupportedFromBelow(activeCollider);
        IsTouchingLeft = Physics2D.OverlapCircle(leftCheck.position, checkRadius, whatIsGround);
        IsTouchingRight = Physics2D.OverlapCircle(rightCheck.position, checkRadius, whatIsGround);
    }

    /**
    * Input: collision
    * Output: normal - the most upward-facing contact normal; isSlope - whether it's a Slope-layer collider
    * Purpose: Record a floor/slope contact for this physics step. Returns false for walls and ceilings.
    */
    public bool RecordContact(Collision2D collision, out Vector2 normal, out bool isSlope)
    {
        normal = Vector2.zero;
        var point = Vector2.zero;
        for (var i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            if (contact.normal.y > normal.y)
            {
                normal = contact.normal;
                point = contact.point;
            }
        }

        isSlope = false;
        if (normal.y < minFloorNormalY) { return false; }

        TouchingFloor = true;
        if (normal.y > FloorNormal.y) { FloorNormal = normal; }

        // Only Slope-layer colliders count as slopes. The normal already faces up, so the underside of a
        // ceiling wedge doesn't count.
        isSlope = IsSlope(collision.collider);
        if (isSlope)
        {
            HasSlopeContact = true;
            SlopeContactPoint = point;
            SlopeContactNormal = normal;
        }
        return true;
    }

    //Purpose: Forget this step's contacts; collision callbacks after the next physics step fill them in again
    public void ClearStep()
    {
        TouchingFloor = false;
        FloorNormal = Vector2.zero;
        HasSlopeContact = false;
    }

    /**
    * Purpose: Sweep a thin box the full width of Geo's bottom a hair downward. Anything floor-like right under
    *          any part of the bottom - even just a platform corner - is holding Geo up.
    */
    public bool IsSupportedFromBelow(Collider2D activeCollider)
    {
        var bounds = activeCollider.bounds;
        var count = Physics2D.BoxCast(bounds.center, BottomCastSize(bounds), 0, Vector2.down, Filter(whatIsGround),
            _hits, bounds.extents.y + supportCheckDistance);
        for (var i = 0; i < count; i++)
        {
            if (IsSelfOrStartOverlap(_hits[i])) { continue; }
            if (_hits[i].normal.y >= minFloorNormalY) { return true; }
        }
        return false;
    }

    /**
    * Output: flatHit - the flat surface found (for logging)
    * Purpose: Sweep a box as wide as Geo's bottom straight down from its centre, a little past Geo. Flat ground
    *          (Ground layers minus Slope) anywhere along the bottom means Geo is on or hanging off a platform.
    *          Starts from the centre because a ball resting on a platform corner sits lower than the platform top.
    */
    public bool IsOverFlatGround(Collider2D activeCollider, out RaycastHit2D flatHit)
    {
        var bounds = activeCollider.bounds;
        var count = Physics2D.BoxCast(bounds.center, BottomCastSize(bounds), 0, Vector2.down,
            Filter(whatIsGround & ~whatIsSlope), _hits, bounds.extents.y + flatCastDistance);
        for (var i = 0; i < count; i++)
        {
            if (IsSelfOrStartOverlap(_hits[i])) { continue; }
            if (IsFlat(_hits[i].normal))
            {
                flatHit = _hits[i];
                return true;
            }
        }

        flatHit = default;
        return false;
    }

    /**
    * Input: xVelocity
    * Purpose: Side probe: sweep the cube's box sideways before it moves. Any surface facing the cube that isn't
    *          flat floor (a slope, inclined or vertical wall) blocks movement that way, so the cube never gets
    *          pushed up onto it by the physics solver.
    */
    public bool InclinedWallAhead(Collider2D cube, float xVelocity)
    {
        var direction = Mathf.Sign(xVelocity);
        var distance = sideProbeDistance + Mathf.Abs(xVelocity) * Time.fixedDeltaTime;

        var count = cube.Cast(new Vector2(direction, 0), Filter(whatIsGround), _hits, distance);
        for (var i = 0; i < count; i++)
        {
            var normal = _hits[i].normal;
            if (normal.x * direction < 0 && !IsFlat(normal)) { return true; }
        }
        return false;
    }

    private static Vector2 BottomCastSize(Bounds bounds) => new Vector2(bounds.size.x * 0.98f, 0.02f);

    // Skip Geo itself, and hits already overlapping at the start of the cast (their normal is meaningless)
    private bool IsSelfOrStartOverlap(RaycastHit2D hit) =>
        hit.collider.attachedRigidbody == _rigidbody2D || hit.distance <= 0;

    private static ContactFilter2D Filter(LayerMask mask)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(mask);
        return filter;
    }

    private void OnDrawGizmosSelected()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box == null) { return; }
        var bounds = box.bounds;

        // Side probes
        var probe = new Vector3(sideProbeDistance, bounds.size.y, 0);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center + Vector3.right * (bounds.extents.x + sideProbeDistance / 2), probe);
        Gizmos.DrawWireCube(bounds.center + Vector3.left * (bounds.extents.x + sideProbeDistance / 2), probe);

        // Flat-ground cast: flat ground inside this box blocks slope rolling
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(bounds.center.x, bounds.min.y - flatCastDistance / 2, 0),
            new Vector3(bounds.size.x, flatCastDistance, 0));
    }
}
