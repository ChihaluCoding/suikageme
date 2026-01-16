using UnityEngine;

public static class SceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureCamera()
    {
        if (Camera.main != null)
        {
            return;
        }

        GameObject camObject = new GameObject("Main Camera");
        Camera cam = camObject.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor = new Color(1f, 0.97f, 0.99f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camObject.tag = "MainCamera";
        camObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureGameManager()
    {
        GameManager existing = Object.FindFirstObjectByType<GameManager>();
        if (existing != null)
        {
            return;
        }

        GameObject manager = new GameObject("GameManager");
        manager.AddComponent<GameManager>();
    }
}
