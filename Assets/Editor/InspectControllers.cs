using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class InspectControllers
{
    public static void Execute()
    {
        string[] paths = {
            "Assets/Res/DGMT_data/Model_yuanwenjian/部落守卫者/kuanggong_01/kuanggong_1.controller",
            "Assets/Res/DGMT_data/Model_yuanwenjian/部落守卫者/kuanggong_02/kuanggong_02.controller",
            "Assets/Res/DGMT_data/Model_yuanwenjian/部落守卫者/kuanggong_03/kuanggong_03.controller",
            "Assets/Res/DGMT_data/Model_yuanwenjian/部落守卫者/kuanggong_04/kuanggong_04.controller",
            "Assets/Models/juese/nn_01/kuanggong_05.controller",
        };

        foreach (var path in paths)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null) { Debug.LogError($"[Inspect] NOT FOUND: {path}"); continue; }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"\n=== {ctrl.name} ({path}) ===");

            // Parameters
            sb.Append("  Params: ");
            foreach (var p in ctrl.parameters) sb.Append($"{p.name}({p.type}) ");
            sb.AppendLine();

            // Layers + States
            foreach (var layer in ctrl.layers)
            {
                sb.AppendLine($"  Layer: {layer.name}");
                foreach (var state in layer.stateMachine.states)
                {
                    var s = state.state;
                    string clip = s.motion != null ? s.motion.name : "(null)";
                    sb.AppendLine($"    State: '{s.name}'  clip={clip}");
                }
            }
            Debug.Log(sb.ToString());
        }
    }
}
