using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DiagCamera
{
    public static void Diagnose()
    {
        var cam = Camera.main;
        if (cam == null) { Debug.LogError("[Diag] Main Camera NOT FOUND"); return; }

        Debug.Log($"[Diag] Camera pos: {cam.transform.position}");
        Debug.Log($"[Diag] Camera rot: {cam.transform.eulerAngles}");
        Debug.Log($"[Diag] ClearFlags: {cam.clearFlags}");
        Debug.Log($"[Diag] BackgroundColor: {cam.backgroundColor}");
        Debug.Log($"[Diag] CullingMask: {cam.cullingMask} (binary: {System.Convert.ToString(cam.cullingMask, 2)})");
        Debug.Log($"[Diag] Near/Far: {cam.nearClipPlane} / {cam.farClipPlane}");
        Debug.Log($"[Diag] IsOrtho: {cam.orthographic}, FOV: {cam.fieldOfView}");
        Debug.Log($"[Diag] Enabled: {cam.enabled}");

        // Check URP
        var rp = QualitySettings.renderPipeline;
        var grp = GraphicsSettings.defaultRenderPipeline;
        Debug.Log($"[Diag] QualitySettings RP: {(rp != null ? rp.name : "NULL - Legacy")}");
        Debug.Log($"[Diag] GraphicsSettings defaultRP: {(grp != null ? grp.name : "NULL - Legacy")}");

        // Check Lighting
        Debug.Log($"[Diag] Skybox: {(RenderSettings.skybox != null ? RenderSettings.skybox.name : "NULL")}");
        Debug.Log($"[Diag] AmbientMode: {RenderSettings.ambientMode}");
        Debug.Log($"[Diag] AmbientIntensity: {RenderSettings.ambientIntensity}");

        // Check all cameras
        var allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        Debug.Log($"[Diag] Total cameras: {allCams.Length}");
        foreach (var c in allCams)
        {
            Debug.Log($"[Diag]   Cam '{c.name}': depth={c.depth}, culling={c.cullingMask}, enabled={c.enabled}, clearFlags={c.clearFlags}");
        }

        // Check scene objects layer
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Debug.Log($"[Diag] Active renderers: {renderers.Length}");
        foreach (var r in renderers)
        {
            Debug.Log($"[Diag]   Renderer '{r.gameObject.name}' layer={r.gameObject.layer} layerName={LayerMask.LayerToName(r.gameObject.layer)} enabled={r.enabled} visible={r.isVisible}");
        }
    }
}
