using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// The level goal: floats and glows; when Geo touches it, Geo stops, the win sound and screen wipe play, and the
// next level loads.
public class CrownController : MonoBehaviour
{
    private Rigidbody2D _rigidbody2D;
    public Light lightFeature;

    private float _startY;          // floating bobs around this height
    private float _startLightRange; // glow pulses around this range

    [Tooltip("Scene loaded when Geo reaches the Crown. Must be in the Build Settings. Empty = decoration only.")]
    public string nextLevelName;
    [Tooltip("Plays the \"LeaveWipe\" screen transition.")]
    public Animator animator;
    public bool floating = true;

    public AudioSource winSFX;

    private void LoadNextLevel()
    {
        SceneManager.LoadScene(nextLevelName);
    }

    void Start()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _startY = transform.position.y;
        _startLightRange = lightFeature.range;
    }

    void Update()
    {
        // Pulse the glow
        lightFeature.range = _startLightRange + 0.2f * Mathf.Cos(Time.time);
    }

    private void FixedUpdate()
    {
        // Bob up and down
        if(floating){_rigidbody2D.position = new Vector2(_rigidbody2D.position.x, _startY + 0.3f * Mathf.Sin(Time.time));}
    }

    /**
    * Input: hitBox
    * Purpose: Geo reached the Crown: stop Geo and finish the level
    */
    private void OnTriggerEnter2D(Collider2D hitBox)
    {
        if (string.IsNullOrEmpty(nextLevelName)) { return; }

        if (hitBox.CompareTag($"Geo"))
        {
            hitBox.gameObject.GetComponent<GeoController>().StopMovement();
            StartCoroutine(Wipe());
        }
    }

    //Purpose: Play the win sound and screen wipe, then load the next level once the wipe has covered the screen
    private IEnumerator Wipe()
    {
        if(!winSFX.isPlaying){
            winSFX.Play(0);
        }
        animator.Play("LeaveWipe");
        yield return new WaitForSeconds(1);
        LoadNextLevel();
    }

#if UNITY_EDITOR
    // Catch a mistyped scene name in the editor instead of when the level is finished
    private void OnValidate()
    {
        if (!string.IsNullOrEmpty(nextLevelName)) { SceneNames.WarnIfNotInBuild(nextLevelName, this); }
    }
#endif
}
