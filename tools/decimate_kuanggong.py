"""
Blender 无头模式 — 批量减面脚本
目标：kuanggong 01~05 所有包含角色体模的 FBX
策略：Decimate Collapse ratio=0.30（7k面 → ~2k面）
      保留蒙皮权重 / UV / 动画数据
运行方式：
  blender --background --python decimate_kuanggong.py
"""

import bpy
import os

# ── 配置 ──────────────────────────────────────────────────────────────
BASE = r"E:\AIProject\DM_CK\Assets\Res\DGMT_data\Model_yuanwenjian\部落守卫者"

DECIMATE_RATIO = 0.30   # 保留 30%（7414 → ~2224 面）

# 每个角色目录下需要处理的 FBX 文件名（含角色体模的都要处理）
TARGET_FILENAMES = [
    "Idle.fbx",
    "Attack.fbx",
    "Sitting Dazed.fbx",
    "Standing Melee Attack Downward.fbx",
    "Bankuang_run.fbx",
]

# 武器/道具只有几百面，不需要减，跳过
SKIP_FILENAMES = {
    "pickaxe+3d+model.fbx",
    "pickaxe.fbx",
    "hammer.fbx",
    "hammer2.fbx",
    "chuizi.fbx",
    "chuizi 1.fbx",
}

# ── 主流程 ────────────────────────────────────────────────────────────
results = []

for char_dir in sorted(os.listdir(BASE)):
    char_path = os.path.join(BASE, char_dir)
    if not os.path.isdir(char_path):
        continue

    for fname in os.listdir(char_path):
        if not fname.endswith(".fbx"):
            continue
        if fname in SKIP_FILENAMES:
            print(f"[SKIP weapon] {char_dir}/{fname}")
            continue

        fbx_path = os.path.join(char_path, fname)

        # ── 导入 ──────────────────────────────────────────────────────
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=fbx_path)

        meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
        if not meshes:
            print(f"[SKIP no-mesh] {char_dir}/{fname}")
            continue

        # 面数很少说明是纯动画 FBX（骨架无 mesh），或武器，跳过
        total_before = sum(len(m.data.polygons) for m in meshes)
        if total_before < 500:
            print(f"[SKIP low-poly {total_before}] {char_dir}/{fname}")
            continue

        # ── 减面 ──────────────────────────────────────────────────────
        for obj in meshes:
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)

            mod = obj.modifiers.new("Decimate", 'DECIMATE')
            mod.decimate_type = 'COLLAPSE'
            mod.ratio         = DECIMATE_RATIO
            # 应用修改器（保留蒙皮权重）
            bpy.ops.object.modifier_apply(modifier=mod.name)

            obj.select_set(False)

        total_after = sum(len(m.data.polygons) for m in meshes)

        # ── 导出（覆盖原文件）─────────────────────────────────────────
        bpy.ops.object.select_all(action='SELECT')
        bpy.ops.export_scene.fbx(
            filepath         = fbx_path,
            use_selection    = False,
            embed_textures   = False,
            add_leaf_bones   = False,
            bake_anim        = True,
            bake_anim_use_all_actions = True,
            mesh_smooth_type = 'FACE',
            path_mode        = 'AUTO',
        )

        line = f"[OK] {char_dir}/{fname}: {total_before} -> {total_after} 面 (减少 {100-int(total_after/total_before*100)}%)"
        print(line)
        results.append(line)

print("\n━━━━━ 汇总 ━━━━━")
for r in results:
    print(r)
print(f"\n共处理 {len(results)} 个 FBX 文件")
