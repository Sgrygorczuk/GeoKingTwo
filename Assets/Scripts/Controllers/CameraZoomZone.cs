using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

// While Geo is inside this trigger, the camera zooms to zoomSize; when Geo leaves it zooms back.
[RequireComponent(typeof(BoxCollider2D))]
public class CameraZoomZone : MonoBehaviour
{
    [Tooltip("Camera orthographic size while Geo is inside. Bigger shows more of the level.")]
    public float zoomSize = 7f;
    [Tooltip("Leave empty to use the first Cinemachine camera in the scene.")]
    public CameraZoomController cameraZoom;

    // Geo swaps between a circle and a box collider when it changes shape,
    // so track every Geo collider inside rather than reacting to each enter/exit.
    private readonly HashSet<Collider2D> _geoColliders = new HashSet<Collider2D>();

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Start()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (cameraZoom == null)
        {
            var vcam = FindAnyObjectByType<CinemachineCamera>();
            if (vcam == null)
            {
                Debug.LogWarning($"{name}: no CinemachineCamera in the scene, zoom zone disabled.");
                enabled = false;
                return;
            }
            cameraZoom = vcam.GetComponent<CameraZoomController>();
            if (cameraZoom == null)
                cameraZoom = vcam.gameObject.AddComponent<CameraZoomController>();
        }
    }

    /**
    * Input: hitBox
    * Purpose: Geo entered the zone: zoom the camera to this zone's size
    */
    private void OnTriggerEnter2D(Collider2D hitBox)
    {
        if (!enabled || !hitBox.CompareTag($"Geo")) return;
        if (_geoColliders.Add(hitBox) && _geoColliders.Count == 1)
            cameraZoom.Enter(this);
    }

    /**
    * Input: hitBox
    * Purpose: Geo left the zone: let the camera return to its normal size
    */
    private void OnTriggerExit2D(Collider2D hitBox)
    {
        if (!enabled || !hitBox.CompareTag($"Geo")) return;
        if (_geoColliders.Remove(hitBox) && _geoColliders.Count == 0)
            cameraZoom.Exit(this);
    }

    private void OnDisable()
    {
        if (_geoColliders.Count > 0 && cameraZoom != null)
            cameraZoom.Exit(this);
        _geoColliders.Clear();
    }

    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider2D>();
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.offset, box.size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
