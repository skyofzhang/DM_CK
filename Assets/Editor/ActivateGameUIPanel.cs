using UnityEngine;
using UnityEditor;

public class ActivateGameUIPanel
{
    [MenuItem("Tools/DrscfZ/Toggle GameUIPanel Active")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }
        var t = canvas.transform.Find("GameUIPanel");
        if (t == null) { Debug.LogError("GameUIPanel not found"); return; }
        bool newState = !t.gameObject.activeSelf;
        t.gameObject.SetActive(newState);
        EditorUtility.SetDirty(t.gameObject);
        Debug.Log("GameUIPanel active = " + newState);
    }
}
