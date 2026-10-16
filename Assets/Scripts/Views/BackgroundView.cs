using UnityEngine;
using UnityEngine.Serialization;

// Parallax background: three rows of layers follow the camera vertically (above, level with and below it), each
// piece bobbing gently up and down.
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

    // LateUpdate rather than FixedUpdate: follow the camera every rendered frame instead of at the physics rate
    private void LateUpdate()
    {
        FollowCamera(topRow, RowSpacing);
        FollowCamera(middleRow, 0);
        FollowCamera(bottomRow, -RowSpacing);
    }

    //Purpose: Keep a row at a fixed height relative to the camera; each piece bobs slightly out of step with the next
    private void FollowCamera(Transform[] row, float heightAboveCamera)
    {
        for (var i = 0; i < row.Length; i++)
        {
            row[i].position = new Vector2(row[i].position.x,
                cameraTransform.position.y + heightAboveCamera + bobHeight * Mathf.Sin(i + Time.time));
        }
    }
}
