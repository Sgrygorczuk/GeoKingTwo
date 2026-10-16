using UnityEngine;

// All of the game's controls, keyboard and controller (Xbox-style layout on the legacy Input Manager):
//   Move          A/D, arrow keys, left stick, D-pad
//   Change shape  E, A button (button 0)        - also starts the game on the title screen
//   Restart       R, View/Back button (button 6)
//   Back / quit   Escape, B button (button 1)   - menus and credits
public static class GeoInputController
{
    // -1..1; the "Horizontal" axes cover A/D, arrows, the left stick and the D-pad (6th joystick axis on Windows)
    public static float Horizontal => Mathf.Clamp(Input.GetAxisRaw("Horizontal"), -1f, 1f);

    public static bool ChangeShapePressed =>
        Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton0);

    public static bool RestartPressed =>
        Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.JoystickButton6);

    public static bool BackPressed =>
        Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
}
