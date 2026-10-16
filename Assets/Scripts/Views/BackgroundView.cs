using UnityEngine;
using UnityEngine.Serialization;

// Parallax background: three rows of layers follow the camera vertically (above, level with and below it), each
// piece bobbing gently up and down. The whole background scales with the camera's orthographic size so it
// keeps covering the screen when a CameraZoomZone zooms out.
// Runs after the camera (CinemachineBrain is at the default order 0) so the layers follow this frame's camera position
[DefaultExecutionOrder(100)]
public class BackgroundView : MonoBehaviour
{
    [Tooltip("Pieces kept 3.5 units above the camera.")]
    [FormerlySerializedAs("transformsOne")]
    public Transform[] topRow;
    [Tooltip("Pieces kept level with the camera.")]
    [FormerlySerializedAs("transformsTwo")]
    public Transform[] middleRow;
    [Tooltip("Pieces kept 3.5 units below the camera.")]
    [FormerlySerializedAs("transformsThree")]
    public Transform[] bottomRow;
    [FormerlySerializedAs("cameraPosition")]
    public Transform cameraTransform;
    [Tooltip("How far each piece bobs up and down.")]
    [FormerlySerializedAs("offset")]
    public float bobHeight;

    private const float RowSpacing = 3.5f;

    private Camera _camera;
    private Vector3 _baseScale;
    private float _baseSize;
    private float _zoom = 1f;

    private void Start()
    {
        _camera = cameraTransform.GetComponent<Camera>();
        _baseScale = transform.localScale;
        _baseSize = _camera.orthographicSize;
    }

    // LateUpdate rather than FixedUpdate: follow the camera every rendered frame instead of at the physics rate
    private void LateUpdate()
    {
        ScaleToCamera();
        FollowCamera(topRow, RowSpacing * _zoom);
        FollowCamera(middleRow, 0);
        FollowCamera(bottomRow, -RowSpacing * _zoom);
    }

    //Purpose: Grow or shrink with the camera's view, never smaller than the screen
    private void ScaleToCamera()
    {
        _zoom = _camera.orthographicSize / _baseSize;
        var viewHeight = _camera.orthographicSize * 2f;
        transform.localScale = new Vector3(
            Mathf.Max(_baseScale.x * _zoom, viewHeight * _camera.aspect),
            Mathf.Max(_baseScale.y * _zoom, viewHeight),
            _baseScale.z);
    }

    //Purpose: Keep a row at a fixed height relative to the camera; each piece bobs slightly out of step with the next
    private void FollowCamera(Transform[] row, float heightAboveCamera)
    {
        for (var i = 0; i < row.Length; i++)
        {
            row[i].position = new Vector2(row[i].position.x,
                cameraTransform.position.y + heightAboveCamera + bobHeight * _zoom * Mathf.Sin(i + Time.time));
        }
    }
}
