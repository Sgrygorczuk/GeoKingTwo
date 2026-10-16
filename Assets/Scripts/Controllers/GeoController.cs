using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// The player: moves, switches between circle (bounces) and cube, rolls down slopes as a ball, dies and respawns.
// Ground/slope/wall sensing lives in GeoGroundModel; controls in GeoInputController.
[RequireComponent(typeof(GeoGroundModel))]
public class GeoController : MonoBehaviour
{
    //==================================================================================================================
    // Variables
    //==================================================================================================================

    //====================================================== Movement  =================================================
    [Tooltip("Horizontal move speed (full stick / key press).")]
    [Min(0f)] public float moveSpeed = 7.5f;
    [Tooltip("Fastest Geo may travel upward; also caps bounce speed.")]
    [Min(0f)] public float maxUpwardSpeed = 20f;
    [Tooltip("Minimum fall speed of the cube once it leaves the ground (it drops fast instead of floating).")]
    [Min(0f)] public float cubeFallSpeed = 7.5f;
    [Tooltip("Upward hop when changing into a circle while on the ground.")]
    [Min(0f)] public float shapeChangeHop = 10f;

    //====================================================== Components  ===============================================
    private Rigidbody2D _rigidbody2D;
    private GeoGroundModel _ground;
    public SpriteRenderer spriteRenderer;
    [Tooltip("Sprite renderer of the shape-change animation; recolored together with Geo.")]
    [FormerlySerializedAs("animationRender")]
    public SpriteRenderer animationRenderer;
    public BoxCollider2D boxCollider2D;
    public CircleCollider2D circleCollider2D;
    [FormerlySerializedAs("animatorGeo")]
    public Animator shapeAnimator;
    [Tooltip("Screen fade played on death.")]
    [FormerlySerializedAs("animationFade")]
    public Animator fadeAnimator;
    public AudioSource dieSFX;
    public AudioSource splashSFX;

    //====================================================== Shape  ====================================================
    private const int Circle = 0;
    private const int Cube = 1;
    [Tooltip("Sprite for each shape: 0 = circle, 1 = cube.")]
    public Sprite[] shapes;
    [Tooltip("Current shape: 0 = circle, 1 = cube. Set in the scene to choose the starting shape.")]
    [FormerlySerializedAs("_shapeIndex")]
    public int shapeIndex = Circle;

    //====================================================== State  ====================================================
    [Tooltip("Where Geo respawns; checkpoints move it.")]
    public Transform respawnPoint;
    private Vector3 _levelStartPosition; // respawnPoint's starting spot, restored by Restart

    [Tooltip("Off on the title screen and once the level is finished: Geo ignores input and just drops in place.")]
    [FormerlySerializedAs("_stopMovement")]
    public bool canMove = true;

    //================================================= Respawn  =======================================================
    // After respawning the controls stay locked until the ball's first bounce off the floor, so a held direction
    // doesn't walk Geo straight back into danger
    [Tooltip("Safety net: if Geo never bounces after respawning, give control back after this many seconds anyway.")]
    [Min(0f)] public float respawnLockTimeout = 3f;
    private float _inputLockedUntil;
    private bool _isRespawning;
    private bool _waitingForFirstBounce;
    private bool InputLocked => Time.time < _inputLockedUntil;

    //================================================= Bounce  ========================================================
    [Tooltip("Bounce speed used when the circle comes to rest on flat ground without a fresh impact.")]
    public float restingBounceSpeed = 10f;
    [Tooltip("How much bounce speed the circle regains per bounce until it is back to its highest bounce.")]
    [Min(0f)] public float bounceBuildUp = 1.5f;
    private float _lastBounceSpeed;
    private float _peakBounceSpeed;

    //================================================= Slope Roll  ====================================================
    // Touching a slope (Slope layer) without flat ground underneath takes away control and lets the sphere
    // physically roll down. Once it has stopped on flat ground it becomes a cube and the player gets control back.
    [Tooltip("Grip between the sphere and the slope while rolling (needs > 0 to actually spin).")]
    [Min(0f)] public float rollFriction = 0.6f;
    [Tooltip("Extra acceleration down the slope while rolling, on top of gravity. 0 = gravity only.")]
    [Min(0f)] public float slopeRollBoost = 8f;
    [Tooltip("Top speed while rolling down a slope.")]
    [Min(0f)] public float maxRollSpeed = 18f;
    [Tooltip("How quickly the sphere slows down on flat ground after rolling off a slope.")]
    [FormerlySerializedAs("slideFriction")]
    public float rollResistance = 6f;
    [Tooltip("How long the sphere must be stopped on flat ground before it turns into a cube.")]
    public float settleTime = 0.15f;
    [Tooltip("How much faster the cube-to-sphere morph plays when hitting a slope (4 = about 0.08s).")]
    [Min(1f)] public float slopeMorphSpeed = 4f;
    [Tooltip("After a roll ends, how long before touching a slope can start another roll.")]
    [Min(0f)] public float slopeRetriggerDelay = 0.5f;
    [Tooltip("Log every slope contact and whether it started a roll (and why) to the Console.")]
    public bool logSlopeDecisions;
    private float _slopeCooldownUntil;
    private bool _isRolling;    // rolling down a slope with no player control
    private float _settleTimer; // how long the sphere has been stopped on flat ground
    private PhysicsMaterial2D _rollMaterial;
    private PhysicsMaterial2D _landMaterial; // no bounce: the cube lands dead after reaching the Crown

    private Collider2D ActiveCollider => shapeIndex == Circle ? (Collider2D)circleCollider2D : boxCollider2D;

    private void Awake()
    {
        // Looked up in Awake so other scripts can safely call into Geo before its Start has run
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _ground = GetComponent<GeoGroundModel>();
    }

    private void Start()
    {
        // Use a runtime copy so we never modify the Geo.physicsMaterial2D asset itself
        var sourceMaterial = _rigidbody2D.sharedMaterial;
        var runtimeMaterial = sourceMaterial != null ? Instantiate(sourceMaterial) : new PhysicsMaterial2D { friction = 0 };
        runtimeMaterial.bounciness = 1;
        _rigidbody2D.sharedMaterial = runtimeMaterial;
        _rollMaterial = new PhysicsMaterial2D("GeoRoll") { friction = rollFriction, bounciness = 0 };
        _landMaterial = new PhysicsMaterial2D("GeoLand") { friction = 0, bounciness = 0 };

        // Start in the shape the scene asks for (e.g. a cube on the title screen) - all of it, not just the sprite
        SnapToShape(shapeIndex);

        _levelStartPosition = respawnPoint.position;
    }

    private void Update()
    {
        if (!_isRolling && !InputLocked && GeoInputController.ChangeShapePressed)
        {
            SetShape(shapeIndex == Circle ? Cube : Circle);
        }

        if (GeoInputController.RestartPressed) { Restart(); }

        _ground.UpdateChecks(ActiveCollider);
    }

    //Purpose: Jump straight into the morph clip (no waiting on animator transitions) and play it sped up
    private IEnumerator QuickMorph(int newShape, float speed)
    {
        shapeAnimator.SetInteger("State", newShape);
        shapeAnimator.Play(newShape == Circle ? "Square_To_Circle" : "Circle_To_Square", 0, 0f);
        shapeAnimator.speed = speed;
        yield return new WaitForSeconds(0.35f / speed); // morph clips are ~0.32s long
        shapeAnimator.speed = 1f;
    }

    //Purpose: Switch between circle and cube, playing the shape change animation
    private void SetShape(int newShape, bool hopIntoCircle = true, float morphSpeed = 1f)
    {
        if (newShape == shapeIndex) { return; }

        if (morphSpeed > 1f) { StartCoroutine(QuickMorph(newShape, morphSpeed)); }
        else { shapeAnimator.SetInteger("State", newShape); }
        ApplyShape(newShape);

        if (shapeIndex == Circle && hopIntoCircle && _ground.IsGrounded)
        {
            _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, shapeChangeHop);
        }
    }

    //Purpose: Switch shape instantly with no morph animation (level start, respawn)
    private void SnapToShape(int newShape)
    {
        ApplyShape(newShape);
        shapeAnimator.SetInteger("State", newShape);
        shapeAnimator.Play(newShape == Circle ? "Circle" : "Square", 0, 0f);
    }

    //Purpose: The shape number, sprite and colliders always change together
    private void ApplyShape(int newShape)
    {
        shapeIndex = newShape;
        spriteRenderer.sprite = shapes[newShape];
        circleCollider2D.enabled = newShape == Circle;
        boxCollider2D.enabled = newShape == Cube;
    }

    //Purpose: Physics step: start a roll if needed, then move, roll or stay put
    private void FixedUpdate()
    {
        TryStartRoll();

        if (!canMove) { UpdateStopped(); }
        else if (_isRolling) { UpdateRolling(); }
        else { UpdateMovement(); }

        _ground.ClearStep();
    }

    /**
    * Purpose: Decide once per physics step, after every collision has been reported, whether to start a roll.
    *          Any slope contact rolls (ball or cube), unless the flat-ground cast finds flat ground under any
    *          part of Geo's bottom - i.e. it's standing on or hanging off the edge of a platform.
    */
    private void TryStartRoll()
    {
        if (_isRolling || !_ground.HasSlopeContact || Time.time < _slopeCooldownUntil) { return; }

        var overFlat = _ground.IsOverFlatGround(ActiveCollider, out var flatHit);

        if (logSlopeDecisions)
        {
            var reason = overFlat ? $"flat ground under Geo at {flatHit.point:F2} ({flatHit.collider.name})" : "no flat ground under Geo";
            Debug.Log($"[Geo] Slope contact {_ground.SlopeContactNormal:F2} at {_ground.SlopeContactPoint:F2} as " +
                      $"{(shapeIndex == Circle ? "circle" : "cube")}: {(overFlat ? "no roll" : "ROLL")} ({reason})", this);
        }

        if (!overFlat) { LoseControlOnSlope(_ground.SlopeContactNormal); }
    }

    //Purpose: Player-controlled movement: sideways from input, vertical from bouncing (circle) or falling (cube)
    private void UpdateMovement()
    {
        var xVelocity = InputLocked ? 0 : GeoInputController.Horizontal * moveSpeed;

        // Don't push into a wall Geo is already touching
        if (_ground.IsTouchingLeft && xVelocity < 0 || _ground.IsTouchingRight && xVelocity > 0)
        {
            xVelocity = 0;
        }

        // The cube can't climb slopes or push onto inclined walls
        if (shapeIndex == Cube && xVelocity != 0 && _ground.InclinedWallAhead(boxCollider2D, xVelocity))
        {
            xVelocity = 0;
        }

        // The cube sticks to the ground and drops fast instead of floating
        var yVelocity = Mathf.Min(_rigidbody2D.linearVelocity.y, maxUpwardSpeed);
        if (shapeIndex == Cube)
        {
            yVelocity = _ground.IsGrounded ? 0 : Mathf.Min(yVelocity, -cubeFallSpeed);
        }

        _rigidbody2D.linearVelocity = new Vector2(xVelocity, yVelocity);
    }

    //Purpose: Movement switched off (title screen, level finished at the Crown): no sideways motion, and the cube
    //         drops straight down and stays put once it lands
    private void UpdateStopped()
    {
        var velocity = _rigidbody2D.linearVelocity;
        velocity.x = 0;
        if (shapeIndex == Cube)
        {
            velocity.y = _ground.IsGrounded ? 0 : Mathf.Min(velocity.y, -cubeFallSpeed);
        }
        _rigidbody2D.linearVelocity = velocity;
    }

    //Purpose: No player control: physics rolls the sphere down the slope, then it slows to a stop on flat ground
    private void UpdateRolling()
    {
        if (!_ground.OnFlatFloor)
        {
            _settleTimer = 0; // on the slope or in the air, gravity and friction do the rolling
            if (_ground.TouchingFloor) { BoostDownSlope(_ground.FloorNormal); }
            return;
        }

        var velocity = _rigidbody2D.linearVelocity;
        if (velocity.y > 0) { velocity.y = 0; }
        velocity.x = Mathf.MoveTowards(velocity.x, 0, rollResistance * Time.fixedDeltaTime);
        _rigidbody2D.linearVelocity = velocity;

        // Keep the spin matched to the ground speed so it rolls rather than skids to a stop
        var radius = circleCollider2D.radius * Mathf.Abs(transform.lossyScale.x);
        if (radius > 0) { _rigidbody2D.angularVelocity = -velocity.x / radius * Mathf.Rad2Deg; }

        _settleTimer = Mathf.Abs(velocity.x) < 0.05f ? _settleTimer + Time.fixedDeltaTime : 0;
        if (_settleTimer >= settleTime)
        {
            StopRolling();
            SetShape(Cube);
        }
    }

    /**
    * Input: collision
    * Purpose: Slope contacts are left for TryStartRoll; any other floor contact - flat ground or a platform
    *          corner - bounces the circle straight up. (Bouncing off corners too stops the ball from jittering on
    *          them: the physics bounce goes sideways, UpdateMovement zeroes that, and the bounces die out.)
    */
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_ground.RecordContact(collision, out _, out var isSlope)) { return; }
        if (_isRolling || isSlope || shapeIndex != Circle) { return; }

        var impactSpeed = Mathf.Abs(collision.relativeVelocity.y);
        if (impactSpeed < 0.5f) { return; } // too soft to count; OnCollisionStay2D keeps a resting ball bouncing

        Bounce(BuildUpBounce(impactSpeed));
    }

    /**
    * Input: collision
    * Purpose: Keep tracking the floor (rolling, slope contacts); if the circle comes to rest on the floor, keep it bouncing
    */
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!_ground.RecordContact(collision, out _, out var isSlope)) { return; }
        if (_isRolling || isSlope || shapeIndex != Circle || _rigidbody2D.linearVelocity.y > 0.01f) { return; }

        Bounce(_lastBounceSpeed > 0.5f ? BuildUpBounce(_lastBounceSpeed) : restingBounceSpeed);
    }

    //Purpose: Rolling with friction alone is slow (about 2/3 of a sliding object), so add a push down the slope
    private void BoostDownSlope(Vector2 slopeNormal)
    {
        var downhill = new Vector2(slopeNormal.y, -slopeNormal.x);
        if (downhill.y > 0) { downhill = -downhill; }

        var velocity = _rigidbody2D.linearVelocity + downhill * (slopeRollBoost * Time.fixedDeltaTime);
        _rigidbody2D.linearVelocity = Vector2.ClampMagnitude(velocity, maxRollSpeed);
    }

    //Purpose: Become a sphere, take away control and let it roll freely; UpdateRolling hands control back
    private void LoseControlOnSlope(Vector2 slopeNormal)
    {
        SetShape(Circle, hopIntoCircle: false, morphSpeed: slopeMorphSpeed);
        _isRolling = true;
        _settleTimer = 0;

        // The bouncy material already reflected this impact; drop the part bouncing away from the slope
        var velocity = _rigidbody2D.linearVelocity;
        var away = Vector2.Dot(velocity, slopeNormal);
        if (away > 0) { _rigidbody2D.linearVelocity = velocity - away * slopeNormal; }

        circleCollider2D.sharedMaterial = _rollMaterial;
        _rigidbody2D.constraints &= ~RigidbodyConstraints2D.FreezeRotation;
    }

    //Purpose: First bounce (or roll ending) after a respawn: hand the controls back
    private void ReleaseRespawnLock()
    {
        if (!_waitingForFirstBounce) { return; }
        _waitingForFirstBounce = false;
        _inputLockedUntil = 0;
    }

    //Purpose: Lock rotation upright again, restore the normal material and give control back
    private void StopRolling()
    {
        ReleaseRespawnLock(); // respawned onto a slope: the roll ending counts as landing
        circleCollider2D.sharedMaterial = null; // back to the Rigidbody's Geo material
        _rigidbody2D.constraints |= RigidbodyConstraints2D.FreezeRotation;
        _rigidbody2D.angularVelocity = 0;
        _rigidbody2D.rotation = 0;
        transform.rotation = Quaternion.identity;
        _settleTimer = 0;
        _isRolling = false;
        _slopeCooldownUntil = Time.time + slopeRetriggerDelay;
    }

    /**
    * Input: impactSpeed
    * Purpose: Remember the highest bounce, and let weaker bounces build back up to it a little each time
    */
    private float BuildUpBounce(float impactSpeed)
    {
        _peakBounceSpeed = Mathf.Max(_peakBounceSpeed, impactSpeed);
        _lastBounceSpeed = Mathf.Min(impactSpeed + bounceBuildUp, _peakBounceSpeed);
        return _lastBounceSpeed;
    }

    //Purpose: Launch the circle upward (capped at maxUpwardSpeed); any bounce also ends the respawn lock
    private void Bounce(float speed)
    {
        ReleaseRespawnLock();
        _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, Mathf.Min(speed, maxUpwardSpeed));
    }

    /**
    * Input: hitBox
    * Purpose: Check if the player enters into any triggering hitBoxes
    */
    private void OnTriggerEnter2D(Collider2D hitBox)
    {
        if (hitBox.CompareTag($"Spike"))
        {
            StartCoroutine(Respawn());
        }
    }

    //Purpose: Die: fade out, move to the respawn point as a black circle, and lock controls until the first bounce
    private IEnumerator Respawn()
    {
        if (_isRespawning) { yield break; } // already dying - don't stack a second respawn
        _isRespawning = true;
        _waitingForFirstBounce = false;     // a bounce during the death fade mustn't hand control back
        _inputLockedUntil = float.MaxValue; // no control while dead

        if(!dieSFX.isPlaying){dieSFX.Play(0);}
        fadeAnimator.Play("Fade");
        _rigidbody2D.linearVelocity = new Vector2(0, 0);

        yield return new WaitForSeconds(0.4f);

        SnapTo(respawnPoint.position);
        StopRolling();

        // Respawn as a black circle, with no morph animation
        SnapToShape(Circle);
        spriteRenderer.color = Color.black;
        animationRenderer.color = Color.black;

        _waitingForFirstBounce = true;
        _inputLockedUntil = Time.time + respawnLockTimeout;
        _isRespawning = false;
    }

    //Purpose: Restart key: drop any checkpoint progress and respawn at the start of the level
    private void Restart()
    {
        respawnPoint.position = _levelStartPosition;
        StartCoroutine(Respawn());
    }


//================================================= Functions Called in Other Scripts ==============================

    //Purpose: Teleporter exit
    public void Teleport(float x, float y) { SnapTo(new Vector2(x, y)); }

    //Purpose: Jump instantly to a position. Setting the transform too stops interpolation from drawing Geo
    //         partway between the old and new spots for a frame.
    private void SnapTo(Vector2 position)
    {
        _rigidbody2D.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    //Purpose: Color pickup: recolor Geo, which changes which colored walls are solid
    public void UpdateColor(Color color)
    {
        if(!splashSFX.isPlaying){splashSFX.Play(0);}
        spriteRenderer.color = color;
        animationRenderer.color = color;
    }

    //Purpose: Checkpoint reached: respawn here from now on
    public void UpdateCheckpoint(Vector3 position)
    {
        respawnPoint.position = position;
    }

    public Vector2 GetVelocity()
    {
        return _rigidbody2D.linearVelocity;
    }

    //Purpose: Level finished (Crown): become a cube, stop all sideways motion and fall straight down without bouncing
    public void StopMovement()
    {
        if (_isRolling) { StopRolling(); }
        canMove = false;
        SetShape(Cube);

        boxCollider2D.sharedMaterial = _landMaterial;
        _rigidbody2D.linearVelocity = new Vector2(0, Mathf.Min(_rigidbody2D.linearVelocity.y, 0));
    }

}
