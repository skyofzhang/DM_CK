using UnityEngine;
using UnityEditor;

public class CreateSnowParticle
{
    public static string Execute()
    {
        // 检查是否已存在
        var existing = GameObject.Find("SnowParticleSystem");
        if (existing != null) return "Already exists: SnowParticleSystem";

        // 创建粒子系统 GameObject
        var go = new GameObject("SnowParticleSystem");
        go.transform.position = new Vector3(0, 15, 3);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.duration = 10f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor = new Color(1f, 1f, 1f, 0.8f);
        main.gravityModifier = 0.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1000;

        var emission = ps.emission;
        emission.rateOverTime = 150;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30, 1, 20);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        // 找 PolarScene 父节点
        var polar = GameObject.Find("PolarScene");
        if (polar != null) go.transform.SetParent(polar.transform);

        // 设置材质
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_ParticleAdd.mat");
        if (mat == null) mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ParticleAdd.mat");
        if (mat == null)
        {
            var guids = AssetDatabase.FindAssets("t:Material ParticleAdd");
            if (guids.Length > 0) mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        if (mat != null)
        {
            renderer.material = mat;
        }
        else
        {
            // 创建简单材质
            var newMat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (newMat.shader == null || newMat.shader.name == "Hidden/InternalErrorShader")
                newMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            newMat.color = new Color(1f, 1f, 1f, 0.8f);
            AssetDatabase.CreateAsset(newMat, "Assets/Materials/Mat_SnowParticle.mat");
            renderer.material = newMat;
        }

        // 标记场景已修改
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        return "Created SnowParticleSystem under PolarScene";
    }
}
