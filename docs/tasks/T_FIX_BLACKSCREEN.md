# 任务：T_FIX_BLACKSCREEN — 修复3D场景黑屏

| 属性 | 値 |
|------|-----|
| 任务ID | T_FIX_BLACKSCREEN |
| 负责方 | 主Claude（通过Coplay工具）|
| 优先级 | P0（最高，其他表现层任务依赖于此）|
| 预计时间 | 1-2小时 |
| 状态 | ✅ 已完成 |

---

## 问题背景

Coplay运行时自测（2026-02-24）发现：Play Mode进入游戏时3D场景完全黑屏。
数字/UI可以正常更新（服务器逻辑工作正常），但3D渲染层不可见。

## 可能原因（优先级排序）

1. **Camera Culling Mask**：Main Camera的Culling Mask未包含3D对象所在Layer
2. **URP Pipeline Asset**：Graphics Settings中选择的PipelineAsset不正确（Assets/Settings/目录有多个URPAsset）
3. **Lighting Settings**：Environment>Skybox Material为空，导致天空盒丢失
4. **Camera Far Plane**：Far Plane値太小，场景对象超出视野范围
5. **Camera Position/Rotation**：摄像机位置/旋转错误，没有对准场景

---

## 排查步骤（Coplay执行）

### Step 1：检查Main Camera

```
使用 get_game_object_info("Main Camera")
检查：
- Position：应在场景中央上方俯视角（Y≈10-20，X向Z轴向负方向）
- CullingMask：应包含 Default / Everything（非Nothing）
- Near/Far Plane：Near≈0.1，Far≥100（场景宽度扂20单位）
- Clear Flags：Skybox（不是 Solid Color）
```

### Step 2：检查URP Pipeline Asset

```csharp
// execute_script 运行以下代码
using UnityEngine;
using UnityEngine.Rendering;

var rp = QualitySettings.renderPipeline;
Debug.Log($"当前 RenderPipeline: {(rp != null ? rp.name : "NULL - 使用Legacy渲染")}");

// 同时检查 Graphics Settings
var grp = GraphicsSettings.defaultRenderPipeline;
Debug.Log($"默认 RenderPipeline: {(grp != null ? grp.name : "NULL")}");
```

**预期値**：应有一个包含"URP"的Pipeline Asset名称

### Step 3：检查Lighting

```csharp
using UnityEngine;

Debug.Log($"Skybox: {RenderSettings.skybox?.name ?? "NULL"}");
Debug.Log($"AmbientMode: {RenderSettings.ambientMode}");
Debug.Log($"AmbientIntensity: {RenderSettings.ambientIntensity}");
```

**预期値**：Skybox不为NULL；AmbientIntensity > 0

### Step 4：检查场景对象是否存在

```
使用 list_game_objects_in_hierarchy()
确认场景中有 3D 对象（非仅UI Canvas）
如果场景几乎为空（只有Camera和UI Canvas），说明3D对象未在当前Scene中
```

### Step 5：如发现问题，执行修复

**修复A（CullingMask）**：
```
set_property("Main Camera", "Camera", "cullingMask", "-1")  // -1 = Everything
```

**修复B（URP Asset）**：
```
execute_script → 找到正确的URP Asset路径 →
set_property → QualitySettings选择正确Pipeline
```

**修复C（Skybox）**：
```
set_property场景Lighting → 选择内置Procedural Skybox
或通过execute_script设置：
RenderSettings.skybox = Resources.Load<Material>("DefaultSkybox");
```

**修复D（Camera位置）**：
```
set_transform("Main Camera", position="0,15,-5", rotation="45,0,0")
这是俯视角位置：Y=15高，X=-5偏移，旋转45度向下看
```

---

## 验收标准

- [ ] `capture_scene_object(null)`：看到雪地/建筑/Capsule工人，不是全黑
- [ ] 颜色确认：有蓝色天空 或 深蓝夜空（不是纯黑或纯白）
- [ ] 摄像机视角：能看到场景中央区域，包括炉灶/城门/工位

---

## 完成后操作

1. 截图记录修复前后对比
2. 更新 `docs/progress.md` → T_FIX_BLACKSCREEN: ✅ + 日期

---

*创建：2026-02-24 | 负责：主Claude*
