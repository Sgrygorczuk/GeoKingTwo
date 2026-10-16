using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// Title screen: blinks the "Press E To Start" prompt and quits on Back (Escape / B button).
public class MainScreenController : MonoBehaviour
{
    [FormerlySerializedAs("text")]
    public Text pressStartText;
    [Tooltip("How fast the prompt fades in and out (alpha per second; 0.6 = about 1.7s each way).")]
    public float blinkSpeed = 0.6f;
    private bool _fadingOut = true;

    public void Update()
    {
        pressStartText.color = TextBlink.Step(pressStartText.color, blinkSpeed, ref _fadingOut);

        if (GeoInputController.BackPressed)
        {
            Application.Quit();
        }
    }
}
