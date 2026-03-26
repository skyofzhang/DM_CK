"""
Blender 无头模式 — 批量减面 v2（修正 Unity 坐标轴）
修复：加入 axis_forward='-Z', axis_up='Y'，与 Unity FBX 导入约定一致
"""

import bpy
import os

BASE = r"E:\AIProject\DM_CK\Assets\Res\DGMT_data\Model_yuanwenjian\部落守卫者"
DECIMATE_RATIO = 0.40   # 保留40%，比 v1 的 30% 保守一点，安全优先

SKIP_FILENAMES = {
    "pickaxe+3d+model.fbx", "pickaxe.fbx",
    "hammer.fbx", "hammer2.fbx",
    "chuizi.fbx", "chuizi 1.fbx",
}

results = []

for char_dir in sorted(os.listdir(BASE)):
    char_path = os.path.join(BASE, char_dir)
    if not os.path.isdir(char_path):
        continue

    for fname in os.listdir(char_path):
        if not fname.endswith(".fbx"):
            continue
        if fname in SKIP_FILENAMES:
            continue

        fbx_path = os.path.join(char_path, fname)

        # ── 导入（让 Blender 自动处理原始坐标约定）─────────────────────
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(
            filepath=fbx_path,
            ignore_leaf_bones=True,
            automatic_bone_orientation=False,
            use_manual_orientation=False,
        )

        meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
        if not meshes:
            continue

        total_before = sum(len(m.data.polygons) for m in meshes)
        if total_before < 500:
            continue

        # ── 减面 ─────────────────────────────────────────────────────
        for obj in meshes:
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            mod = obj.modifiers.new("Decimate", 'DECIMATE')
            mod.decimate_type = 'COLLAPSE'
            mod.ratio = DECIMATE_RATIO
            bpy.ops.object.modifier_apply(modifier=mod.name)
            obj.select_set(False)

        total_after = sum(len(m.data.polygons) for m in meshes)

        # ── 导出：axis_forward/up 与 Unity FBX 导入约定对齐 ──────────
        bpy.ops.object.select_all(action='SELECT')
        bpy.ops.export_scene.fbx(
            filepath                  = fbx_path,
            use_selection             = False,
            embed_textures            = False,
            add_leaf_bones            = False,
            # ★ 关键：与 Unity FBX 坐标约定一致
            axis_forward              = '-Z',
            axis_up                   = 'Y',
            apply_unit_scale          = True,
            apply_scale_options       = 'FBX_SCALE_NONE',
            bake_space_transform      = False,
            # 动画
            bake_anim                 = True,
            bake_anim_use_all_actions = True,
            bake_anim_simplify_factor = 1.0,
            # 网格
            mesh_smooth_type          = 'FACE',
            use_mesh_modifiers        = True,
            path_mode                 = 'AUTO',
        )

        line = f"[OK] {char_dir}/{fname}: {total_before} -> {total_after} 面"
        print(line)
        results.append(line)

print("\n━━━━━ 汇总 ━━━━━")
for r in results:
    print(r)
print(f"共处理 {len(results)} 个文件")
