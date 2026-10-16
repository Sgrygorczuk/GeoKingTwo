using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

// Lives on the Cinemachine camera and eases its orthographic size toward the
// most recently entered CameraZoomZone, or back to the level's default size.
[RequireComponent(typeof(CinemachineCamera))]
public class CameraZoomController : MonoBehaviour
{
    [Tooltip("Roughly how long, in seconds, the camera takes to reach a new size.")]
    public float smoothTime = 0.5f;

    private CinemachineCamera _vcam;
    private float _defaultSize;
    private float _velocity;
    private readonly List<CameraZoomZone> _activeZones = new List<CameraZoomZone>();

    private void Awake()
    {
        _vcam = GetComponent<CinemachineCamera>();
        _defaultSize = _vcam.Lens.OrthographicSize;
    }

    /**
    * Input: zone
    * Purpose: Geo entered a zone: it becomes the size the camera eases toward
    */
    public void Enter(CameraZoomZone zone)
    {
        _activeZones.Remove(zone);
        _activeZones.Add(zone);
    }

    /**
    * Input: zone
    * Purpose: Geo left a zone: fall back to the previous zone, or the default size
    */
    public void Exit(CameraZoomZone zone)
    {
        _activeZones.Remove(zone);
    }

    private void LateUpdate()
    {
        float target = _activeZones.Count > 0 ? _activeZones[_activeZones.Count - 1].zoomSize : _defaultSize;

        var lens = _vcam.Lens;
        lens.OrthographicSize = Mathf.SmoothDamp(lens.OrthographicSize, target, ref _velocity, smoothTime);
        _vcam.Lens = lens;
    }
}
