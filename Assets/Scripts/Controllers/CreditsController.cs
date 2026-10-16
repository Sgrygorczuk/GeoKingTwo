using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

// Credits screen: scrolls the credits (and the Crown) upward, blinks the exit prompt, and returns to the title
// screen once the credits are off screen or Back (Escape / B button) is pressed.
public class CreditsController : MonoBehaviour
{
    //========================= Text
    [FormerlySerializedAs("credits")]
    public Text creditsText;    // scrolling credits
    public Text exitText;       // blinking exit prompt
    [Tooltip("The Crown, scrolled up together with the credits.")]
    [FormerlySerializedAs("Transform")]
    public Rigidbody2D crownBody;

    [Tooltip("How fast the credits scroll up, in units per second.")]
    public float scrollSpeed = 0.6f;
    [Tooltip("How fast the exit prompt fades in and out (alpha per second; 0.6 = about 1.7s each way).")]
    public float blinkSpeed = 0.6f;
    private bool _fadingOut = true;

    /**
    *Purpose: Check for user Key press or text being off the screen
    */
    private void Update()
    {
        if(creditsText.transform.position.y > 10){
            // Finished scrolling: wait for the mouse buttons to be released, then go back to the title screen
            if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1)) { return; }
            SceneManager.LoadScene(SceneNames.StartScreen);
        }
        else
        {
            var scroll = scrollSpeed * Time.deltaTime;
            crownBody.position = new Vector3(crownBody.position.x, crownBody.position.y + scroll, 0);
            creditsText.transform.position = new Vector2(creditsText.transform.position.x, creditsText.transform.position.y + scroll);
        }

        if (GeoInputController.BackPressed)
        {
            SceneManager.LoadScene(SceneNames.StartScreen);
        }

        exitText.color = TextBlink.Step(exitText.color, blinkSpeed, ref _fadingOut);
    }
}
