# 任务：T_WORKER_VISUAL — Worker视觉系统

| 属性 | 值 |
|------|-----|
| 任务ID | T_WORKER_VISUAL |
| 负责方 | 子Claude（使用 Coplay MCP工具）|
| 优先级 | P1 |
| 前置依赖 | T_FIX_BLACKSCREEN 完成（场景可见） |
| 预计时间 | 3-5小时 |
| 状态 | ✅ 已完成 |

---

## 目标

实现 Worker（村民）视觉系统，使观众能直观看到「哪些工人在干什么」，驱动参与感。

---

## 详细动画规格

参考：`docs/modules/animation_spec.md` §1 Worker动画系统

---

## 工程文件范围

| 文件 | 操作 |
|------|------|
| `Assets/Scripts/Survival/WorkerManager.cs` | 修改：添加视觉管理逻辑 |
| `Assets/Scripts/Survival/WorkerController.cs` | 修改：添加状态机动画 |
| `Assets/Scripts/Survival/WorkerVisual.cs` | **新建**：Worker视觉控制组件 |
| `Assets/Scripts/UI/WorkerBubble.cs` | **新建**：工人头顶气泡UI |

---

## 实现规范

### 1. Worker预实例化（符合UI预创建规则）

```csharp
// WorkerManager.cs 修改
// 在Scene中预创建 MAX_WORKERS=20 个Worker GameObject
// 初始全部 SetActive(false)
// 收到弹幕 → SetActive(true) → 分配工位 → 启动动画

private const int MAX_WORKERS = 20;
private WorkerController[] _workerPool; // 预创建池

void Awake() {
    _workerPool = new WorkerController[MAX_WORKERS];
    for (int i = 0; i < MAX_WORKERS; i++) {
        // 在Hierarchy预创建的Worker objects中获取引用
        _workerPool[i] = _preCreatedWorkers[i];
        _workerPool[i].gameObject.SetActive(false);
    }
}
```

### 2. Worker颜色区分（不同工位不同颜色）

| 指令 | 材质颜色 | 气泡图标 |
|------|---------|---------|
| 1 采食物 | `#4488FF`（蓝色）| 🐟 |
| 2 挖煤 | `#666666`（深灰）| ⛏ |
| 3 挖矿 | `#88CCFF`（冰蓝）| 🪨 |
| 4 生火 | `#FF6820`（橙红）| 🔥 |
| 6 打怪 | `#FF2200`（鲜红）| ⚔️ |

### 3. 工位坐标（3D世界坐标）

```csharp
// WorkerManager.cs
static readonly Dictionary<int, Vector3> WORK_POSITIONS = new() {
    { 1, new Vector3(-8, 0, -5) },  // 鱼塘
    { 2, new Vector3(-4, 0,  8) },  // 煤矿
    { 3, new Vector3( 6, 0,  7) },  // 矿山
    { 4, new Vector3( 0, 0, -3) },  // 炉灶
    { 6, new Vector3( 8, 0, -6) },  // 城门
};
static readonly Vector3 IDLE_CENTER = new Vector3(0, 0, 0); // 待机区
```

### 4. WorkerController 状态机

```csharp
public class WorkerController : MonoBehaviour {
    public enum State { Idle, Move, Work, Special, Frozen }

    private State _state = State.Idle;
    private Vector3 _targetPos;
    private float _workEndTime;
    private float _moveSpeed = 3f;

    // Bob动画（Idle状态）
    void Update() {
        switch (_state) {
            case State.Idle:
                // Y轴Bob：上下浮动
                float bobY = Mathf.Sin(Time.time * Mathf.PI * 2) * 0.05f;
                transform.localPosition = _basePos + Vector3.up * bobY;
                break;
            case State.Move:
                // 移向目标
                transform.position = Vector3.MoveTowards(
                    transform.position, _targetPos, _moveSpeed * Time.deltaTime);
                transform.LookAt(_targetPos.WithY(transform.position.y));
                if (Vector3.Distance(transform.position, _targetPos) < 0.1f)
                    TransitionTo(State.Work);
                break;
            case State.Work:
                // Z轴摆动模拟工作
                float swing = Mathf.Sin(Time.time * Mathf.PI * 2) * 30f;
                transform.localRotation = Quaternion.Euler(0, 0, swing);
                break;
        }
    }

    public void AssignWork(int cmdType) {
        _targetPos = WorkerManager.WORK_POSITIONS[cmdType];
        SetColor(WorkerManager.GetColorForCmd(cmdType));
        SetBubbleIcon(WorkerManager.GetIconForCmd(cmdType));
        TransitionTo(State.Move);
    }
}
```

### 5. 气泡World Space UI

```csharp
// WorkerBubble.cs（挂在每个Worker的头顶子对象上）
// Canvas: World Space，大小64×64，位置(0, 1.2, 0)相对于Worker
// 始终朝向相机（Billboard效果）

void Update() {
    transform.LookAt(Camera.main.transform);
    transform.Rotate(0, 180, 0); // 翻转使文字正向
}
```

---

## 服务器消息处理

WorkerManager.cs 监听 NetworkManager 的消息：
```csharp
// 已有的 OnGameStateUpdate 中，读取 barrage 相关数据
// barrage_cmd 数据格式：{ cmd: 1, userId: "xxx", nickname: "村民甲" }
// 每收到一条有效弹幕 → 激活一个空闲Worker → AssignWork(cmd)
```

---

## 参考代码

参考路径：`D:/claude-dm/cehua-doc/xgrj/极寒之夜资源素材/cursor_exoprt/Assets/Scripts/`
- 找 Worker 状态机相关代码作为实现参考
- 特别关注：工人移动逻辑、工位分配逻辑

---

## 验收标准

- [ ] Play Mode中，推送10条cmd=1弹幕后，能看到1-10个蓝色Capsule向鱼塘移动
- [ ] Worker到达工位后开始摆动（工作动画）
- [ ] Worker头顶显示正确的emoji气泡（🐟/⛏/🪨/🔥/⚔️）
- [ ] 发送666弹幕后所有Worker发金色光晕3秒
- [ ] 最多20个Worker同时活跃

## 完成后操作

1. `capture_scene_object()` 截图确认Worker可见
2. 更新 `docs/progress.md` → T_WORKER_VISUAL: ✅

---

*创建：2026-02-24 | 负责：子Claude*
