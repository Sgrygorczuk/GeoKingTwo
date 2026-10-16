using UnityEngine;

// Shared blinking-prompt fade for the menus: fades a color's alpha out to 0, then back in to 1, and repeats.
public static class TextBlink
{
    /**
    * Input: color - the current color; speed - alpha change per second; fadingOut - fading direction, flipped at the ends
    * Output: the color with this frame's alpha
    */
    public static Color Step(Color color, float speed, ref bool fadingOut)
    {
        var step = speed * Time.deltaTime;
        color.a = Mathf.Clamp01(color.a + (fadingOut ? -step : step));
        if (fadingOut && color.a <= 0) { fadingOut = false; }
        else if (!fadingOut && color.a >= 1) { fadingOut = true; }
        return color;
    }
}
