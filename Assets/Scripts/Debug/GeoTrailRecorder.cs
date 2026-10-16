#if UNITY_EDITOR
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// TEMPORARY playtest recorder (editor only). While the "GeoTrailRecorder.Enabled" EditorPref is on, logs Geo's
// position, velocity, shape and color every physics step to <project>/Temp/GeoTrail.csv, plus a line whenever
// Geo's color changes (a pickup). Also logs every rendered frame's duration to <project>/Temp/GeoFrames.csv
// (frame-rate check). Delete this file when done.
public class GeoTrailRecorder : MonoBehaviour
{
    private const string PrefKey = "GeoTrailRecorder.Enabled";
    private static readonly string FilePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "GeoTrail.csv"));
    private static readonly string FramesPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "GeoFrames.csv"));

    private GeoController _geo;
    private Rigidbody2D _rigidbody2D;
    private StringBuilder _buffer;
    private StringBuilder _frames;
    private Color _lastColor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (!UnityEditor.EditorPrefs.GetBool(PrefKey, false)) { return; }
        File.WriteAllText(FilePath, "time,scene,event,x,y,vx,vy,shape,r,g,b\n");
        File.WriteAllText(FramesPath, "realtime,scene,frame_ms,focused,x,y\n");
        SceneManager.sceneLoaded += (scene, mode) => Attach();
        Attach();
    }

    private static void Attach()
    {
        var geo = FindAnyObjectByType<GeoController>();
        if (geo != null && geo.GetComponent<GeoTrailRecorder>() == null) { geo.gameObject.AddComponent<GeoTrailRecorder>(); }
    }

    private void Awake()
    {
        _geo = GetComponent<GeoController>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _buffer = new StringBuilder();
        _frames = new StringBuilder();
        _lastColor = _geo.spriteRenderer.color;
        Write("start");
    }

    // One line per rendered frame; "focused" lets frames from while the editor was in the background be ignored
    private void Update()
    {
        var p = _rigidbody2D.position;
        _frames.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F4},{1},{2:F2},{3},{4:F2},{5:F2}",
            Time.realtimeSinceStartup, gameObject.scene.name, Time.unscaledDeltaTime * 1000f, Application.isFocused ? 1 : 0, p.x, p.y));
    }

    private void FixedUpdate()
    {
        var color = _geo.spriteRenderer.color;
        if (color != _lastColor) { Write("color"); _lastColor = color; }
        Write("step");
        if (_buffer.Length > 32000 || _frames.Length > 32000) { Flush(); }
    }

    private void Write(string evt)
    {
        var p = _rigidbody2D.position;
        var v = _rigidbody2D.linearVelocity;
        var c = _geo.spriteRenderer.color;
        _buffer.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F3},{1},{2},{3:F3},{4:F3},{5:F3},{6:F3},{7},{8:F3},{9:F3},{10:F3}",
            Time.time, gameObject.scene.name, evt, p.x, p.y, v.x, v.y, _geo.shapeIndex, c.r, c.g, c.b));
    }

    private void Flush()
    {
        File.AppendAllText(FilePath, _buffer.ToString());
        _buffer.Clear();
        File.AppendAllText(FramesPath, _frames.ToString());
        _frames.Clear();
    }

    private void OnDisable() { Flush(); }
}
#endif
