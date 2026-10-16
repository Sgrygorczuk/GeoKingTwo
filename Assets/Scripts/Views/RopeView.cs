using UnityEngine;

// Draws a rope from a fixed anchor point to the object it holds up, and keeps the object's DistanceJoint2D
// hooked to that anchor. Move the anchor object while editing to change where the rope hangs from;
// the rope's length is whatever the distance is when the level starts.
[ExecuteAlways]
[RequireComponent(typeof(DistanceJoint2D), typeof(LineRenderer))]
public class RopeView : MonoBehaviour
{
    [Tooltip("The point the rope hangs from. Keep it outside this object so it stays put.")]
    public Transform anchor;

    private DistanceJoint2D _joint;
    private LineRenderer _line;

    private void OnEnable()
    {
        _joint = GetComponent<DistanceJoint2D>();
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.positionCount = 2;
        HookToAnchor();
    }

    // LateUpdate so the rope is drawn after physics has moved the object this frame
    private void LateUpdate()
    {
        if (anchor == null) return;
        if (!Application.isPlaying) HookToAnchor();

        _line.SetPosition(0, anchor.position);
        _line.SetPosition(1, transform.TransformPoint(_joint.anchor));
    }

    //Purpose: Pin the joint's far end to the anchor's position in the world
    private void HookToAnchor()
    {
        if (anchor == null) return;
        _joint.connectedBody = null;
        _joint.connectedAnchor = anchor.position;
    }
}
