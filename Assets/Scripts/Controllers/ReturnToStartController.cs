using UnityEngine;

// Once Geo has left this object alone for a while, its swing gradually dies down until it hangs still where the
// level placed it. Touching it again while it's settling gives it back its normal, free swing.
[RequireComponent(typeof(Rigidbody2D))]
public class ReturnToStartController : MonoBehaviour
{
    [Tooltip("Seconds without Geo touching it before it starts to settle.")]
    public float idleTime = 10f;
    [Tooltip("Seconds over which the swing is slowed down to full damping.")]
    public float settleTime = 4f;
    [Tooltip("How strongly the swing is slowed once settling is fully under way.")]
    public float settleDamping = 2f;

    // Closer than this to the start, and slower than this, counts as home; the last tiny gap is closed exactly
    private const float AtRestDistance = 0.02f;
    private const float AtRestAngle = 1f;
    private const float AtRestSpeed = 0.05f;

    private Rigidbody2D _rigidbody2D;
    private float _linearDamping;
    private float _angularDamping;
    private Vector2 _startPosition;
    private float _startRotation;

    private float _lastTouched;
    private bool _settling;
    private float _settleStarted;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _linearDamping = _rigidbody2D.linearDamping;
        _angularDamping = _rigidbody2D.angularDamping;
        _startPosition = _rigidbody2D.position;
        _startRotation = _rigidbody2D.rotation;
        _lastTouched = Time.time;
    }

    private void FixedUpdate()
    {
        if (_settling)
        {
            UpdateSettle();
        }
        else if (Time.time - _lastTouched >= idleTime && !IsHome())
        {
            _settling = true;
            _settleStarted = Time.time;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) { Touched(collision); }
    private void OnCollisionStay2D(Collision2D collision) { Touched(collision); }

    /**
    * Input: collision
    * Purpose: Geo is touching it: restart the idle timer and let it swing freely again
    */
    private void Touched(Collision2D collision)
    {
        if (!collision.collider.CompareTag($"Geo")) return;
        _lastTouched = Time.time;
        if (_settling) StopSettle();
    }

    //Purpose: Slow the swing a little more each step until it hangs still at the start
    private void UpdateSettle()
    {
        var t = settleTime > 0f ? Mathf.Clamp01((Time.time - _settleStarted) / settleTime) : 1f;
        _rigidbody2D.linearDamping = Mathf.Lerp(_linearDamping, settleDamping, t);
        _rigidbody2D.angularDamping = Mathf.Lerp(_angularDamping, settleDamping, t);

        if (!IsHome()) return;
        _rigidbody2D.position = _startPosition;
        _rigidbody2D.rotation = _startRotation;
        _rigidbody2D.linearVelocity = Vector2.zero;
        _rigidbody2D.angularVelocity = 0f;
        StopSettle();
    }

    //Purpose: Back to the normal swing
    private void StopSettle()
    {
        _settling = false;
        _rigidbody2D.linearDamping = _linearDamping;
        _rigidbody2D.angularDamping = _angularDamping;
        _lastTouched = Time.time;
    }

    //Output: true when it's hanging (nearly) still at its start
    private bool IsHome()
    {
        return Vector2.Distance(_rigidbody2D.position, _startPosition) < AtRestDistance
               && Mathf.Abs(Mathf.DeltaAngle(_rigidbody2D.rotation, _startRotation)) < AtRestAngle
               && _rigidbody2D.linearVelocity.magnitude < AtRestSpeed
               && Mathf.Abs(_rigidbody2D.angularVelocity) < AtRestSpeed;
    }
}
