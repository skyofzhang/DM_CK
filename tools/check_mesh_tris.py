"""
检查 kuanggong FBX 文件中的网格面数
"""
import bpy
import os

FBX_FILES = [
    r"E:\AIProject\DM_CK\Assets\Res\DGMT_data\Model_yuanwenjian\部落守卫者\kuanggong_01\Idle.fbx",
    r"E:\AIProject\DM_CK\Assets\Res\DGMT_data\Model_yuanwenjian\部落守卫者\kuanggong_01\Attack.fbx",
    r"E:\AIProject\DM_CK\Assets\Res\DGMT_data\Model_yuanwenjian\部落守卫者\kuanggong_01\pickaxe+3d+model.fbx",
]

for fbx_path in FBX_FILES:
    if not os.path.exists(fbx_path):
        print(f"[MISS] {fbx_path}")
        continue

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx_path)

    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    arms   = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']

    total_polys = sum(len(o.data.polygons) for o in meshes)
    total_verts = sum(len(o.data.vertices) for o in meshes)

    print(f"\n=== {os.path.basename(fbx_path)} ===")
    print(f"  Mesh objects : {len(meshes)}")
    print(f"  Armatures    : {len(arms)}")
    print(f"  Total faces  : {total_polys}")
    print(f"  Total verts  : {total_verts}")
    for m in meshes:
        print(f"    [{m.name}] faces={len(m.data.polygons)} verts={len(m.data.vertices)}")

print("\n[DONE]")
