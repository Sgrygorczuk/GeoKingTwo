using UnityEditor;
using UnityEngine;

public static class SceneObjectCreator
{
    private const string CircleSpritePath =
        "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Circle.png";

    [MenuItem("Tools/Add Physics Circle")]
    private static void AddPhysicsCircle()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        if (sprite == null)
            Debug.LogWarning($"Circle sprite not found at {CircleSpritePath}; SpriteRenderer left empty.");

        var go = new GameObject("Circle");
        go.AddComponent<SpriteRenderer>().sprite = sprite;
        go.AddComponent<CircleCollider2D>();
        go.AddComponent<Rigidbody2D>();

        Finish(go);
    }

    [MenuItem("Tools/Add TestCube")]
    private static void AddTestCube()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "TestCube";
        go.transform.position = new Vector3(0f, 2f, 0f);

        Finish(go);
    }

    [MenuItem("Tools/Add Camera Zoom Zone")]
    private static void AddCameraZoomZone()
    {
        var go = new GameObject("Camera Zoom Zone");
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(10f, 6f);
        go.AddComponent<CameraZoomZone>();

        var view = SceneView.lastActiveSceneView;
        if (view != null)
            go.transform.position = new Vector3(view.pivot.x, view.pivot.y, 0f);

        Finish(go);
    }

    private static void Finish(GameObject go)
    {
        Undo.RegisterCreatedObjectUndo(go, $"Create {go.name}");
        Selection.activeGameObject = go;
    }
}
