# Multi-Agent 开发流程

本文定义 DM_CK 项目使用 Multi-Agent 开发特性模块的标准流程。
**触发条件**：用户说"Multi-Agent 流程跑 §XX"或"多 Agent 跑 §XX"时，PM 按本文档执行。
**建立于**：2026-04-20，基于用户确认的 5 项决策。

---

## 角色分工

| 角色 | 载体 | 职责 | 隔离 |
|------|------|------|------|
| **PM** | 主对话 Claude | 读章节 → 拆解 → 分派 → 汇总 → commit | 无（持有项目上下文） |
| **Backend Agent** | `subagent_type: general-purpose` | Node.js 服务端（`Server/src/*`） | `isolation: "worktree"` |
| **Frontend Agent** | `subagent_type: general-purpose` | Unity C# + UI（`Assets/Scripts/*`、`Assets/Prefabs/*`、`Assets/Editor/*`） | `isolation: "worktree"` |
| **Reviewer Agent** | `subagent_type: code-review` 或 `general-purpose` | 对照策划案审前后端 diff，输出 gap 清单 + 定位（前/后/协议） | 只读，不开 worktree |

---

## 决策锁定（不可推翻）

1. **PM = 主对话**：不另 spawn PM Agent，避免冷启动重读 CLAUDE.md/MEMORY.md/策划案浪费 token
2. **复核上限 5 轮**：超出后 PM 停流程，向用户报告当前 gap 和建议
3. **并发 vs 串行由 PM 判断**：
   - 纯后端 + 纯前端且无协议交叉 → 并发（同一消息多个 Agent tool call）
   - 需新协议字段 → 后端先定协议 → 前端按协议实现 → 串行
4. **worktree 隔离**：前后端 Agent 各自独立 worktree；Reviewer 只读不开 worktree
5. **任务粒度 = 一次一个功能模块**（一个 §XX 章节），禁止一次吞整本策划案
6. **MCP 优先 + Console 监控**（2026-04-21 新增）：凡能 UnityMCP/coplay-mcp 完成的操作（Editor 菜单、场景搭建、组件挂载、字段绑定、场景保存、读 Console），PM **必须**通过 MCP 直接执行，禁止让用户手动在 Unity 里点菜单/改 Inspector/Ctrl+S。每一次 MCP 操作（Edit 脚本 / refresh / 跑菜单 / save scene）后立即 `read_console` 过滤 error + warning，发现问题 PM 自己处理，不遗留给用户。用户仅保留「Play Mode 真人观察」这一步不可替代的判断。

---

## 启动前置检查

PM 每次启动 Multi-Agent 流程前必须：

1. `git status`（主 repo）— 若不 clean，要求用户先 commit/stash，暂停流程
2. 查 `MEMORY.md` "待完成" 清单确认目标模块的依赖已完成
3. 打开策划案 `极地生存法则_策划案.md` 定位目标章节行号

---

## 标准流程（7 步）

### Step 1 · PM 读章节
打开 `极地生存法则_策划案.md`，读目标 §XX 全文（含协议表 §19、C# 结构体说明）。

### Step 2 · PM 拆解任务
产出两份清单并写入 `TodoWrite`：
- **后端清单**：新协议字段 / 新 handler / 数据结构迁移 / 难度配置 / schemaVersion 提升
- **前端清单**：C# 数据结构 / UI 脚本 / 预制体 / 订阅新事件 / Editor 工具

### Step 3 · PM 分派 Agent
使用 **Agent 任务书模板**（见下）spawn 对应 Agent。并发时在同一条消息里发多个 tool call。

### Step 4 · Agent 返回
PM 审读结果摘要，注意虚假完成（"已实现 X"但实际未改对应文件）。必要时用 `git -C <worktree-path> diff --stat` 验证。

### Step 5 · Reviewer 复核
输入：
- 策划案目标章节原文
- 前后端 Agent 返回的改动文件清单
- 两个 worktree 分支名

输出（严格格式）：
```
VERDICT: PASS | FAIL
IF FAIL:
  [<BACKEND|FRONTEND|PROTOCOL>] <问题描述>（<文件:行号>）
  ...
```

### Step 6 · FAIL 分支
PM 按 Reviewer 的归属标签把问题分回对应 Agent，附上 Reviewer 原文。回到 Step 5。

### Step 7 · 终止条件
- **PASS** → PM 合 worktree 分支 → main（按"合并策略"）→ 告知用户在主 repo 测试（按"测试路径约定"）→ **等用户测试通过**后更新 `MEMORY.md` "已完成/待完成" → 最终报告
- **5 轮仍 FAIL** → 停流程，向用户报告：已修复项 / 剩余 gap / 建议（是否放弃、是否人工接管）
- **用户测试失败** → 按失败范围判断：小改（<30min）PM 直接在新 worktree 修；大改重开一轮 Multi-Agent 流程

---

## Agent 任务书模板

每次 spawn 的 prompt **必须**包含：

```
## 任务
实现策划案 §XX 的 <前端|后端> 部分。

## 项目上下文
- 根路径：E:\AIProject\DM_CK
- 策划案：极地生存法则_策划案.md（§XX，约第 NNNN 行）
- 项目规范：CLAUDE.md「关键设计决策」（不可推翻）

## 必须实现（来自 PM 拆解）
- [ ] <具体项 1，标文件路径>
- [ ] <具体项 2>
- ...

## 严格禁止
- ❌ 修改 极地生存法则_策划案.md（策划案变更须人类决策）
- ❌ 跨模块改动（仅限本 §XX 相关文件）
- ❌ 自主 commit（由 PM 复核通过后统一 commit）
- ❌ 直接 push origin

## 返回格式
1. 改动文件清单（绝对路径）
2. 每个文件改动摘要（3–5 行）
3. 本次未完成项（如有）及原因
4. 自测发现的潜在风险
```

Reviewer Agent 的 prompt 模板：

```
## 任务
对照策划案 §XX 复核前后端实现。

## 输入
- 策划案章节：极地生存法则_策划案.md §XX（第 NNNN 行起）
- 前端 worktree 分支：<branch-name>
- 后端 worktree 分支：<branch-name>

## 检查项
1. 协议字段前后端对齐（§19 协议表）
2. C# 数据结构字段与 JSON 对齐
3. 策划案参数（HP/CD/价格/时长）是否实现
4. CLAUDE.md「关键设计决策」未违反
5. 服务器权威原则（客户端不提前跳状态）

## 输出（严格格式）
VERDICT: PASS | FAIL
IF FAIL:
  [<BACKEND|FRONTEND|PROTOCOL>] <问题描述>（<文件:行号>）
  ...

只读不改代码。
```

---

## 禁止项汇总

- ❌ Agent 自主 commit（commit 只由 PM 复核通过后做）
- ❌ Agent 修改 `极地生存法则_策划案.md`
- ❌ Agent 直接 `git push`（push 需用户明确授权）
- ❌ 并发 Agent 修改同一文件（PM 拆解时必须保证无交集）
- ❌ Reviewer 自行修改代码（只读输出 gap 清单）

---

## 合并策略

复核 PASS 后，PM 有两种选择：

1. **合进 main**（推荐小改动）：
   ```
   git -C E:\AIProject\DM_CK merge <backend-worktree-branch> --no-ff
   git -C E:\AIProject\DM_CK merge <frontend-worktree-branch> --no-ff
   ```
   先合后端（协议），再合前端，commit message 标 §XX。

2. **留 worktree 分支等用户决定**（推荐大改动）：
   向用户报告分支名和改动摘要，由用户手动合并。

worktree 用完后由 PM 执行 `git worktree remove` 清理（若确认已合并或放弃）。

---

## MCP 自动化规范（PM 合并后动作，2026-04-21 新增）

合并到 main 后，PM **通过 MCP 直接操作主 repo 的 Unity 实例**，不让用户手动点任何菜单。

### 按时机的 MCP 操作清单

| 时机 | MCP 操作 | 目的 |
|---|---|---|
| 合并 main 后 | `refresh_unity` | 触发 Unity 重编译 |
| 编译完成 | `read_console types=['error'] filter_text='CS'` | 检查 CS 编译错误 |
| Editor 脚本新增/改完 | `refresh_unity` → `read_console filter='CS'` | 确保新 Editor 脚本可用 |
| 场景需要搭建 UI | 先写 `Assets/Editor/SetupSectionXX.cs`，再 `execute_menu_item Tools/DrscfZ/Setup Section XX UI` | 一键建 GO + 挂脚本 + 绑 Inspector 字段 |
| 菜单跑完 | `read_console count=30 format='plain'` | 验证 Setup 日志 + 查异常 |
| 改了 TMP 文本/Text | `read_console filter_text='Inter-Regular SDF'` | 检查 emoji fallback 警告 |
| 场景改完 | `manage_scene action='save'`（**UnityMCP 自带**，**禁用** coplay `save_scene`） | 持久化到 .unity 文件 |
| 最后收尾 | `read_console types=['error','warning'] count=30` | 确认 0 警告 0 错误再 commit |

### Console 错误分类 + 即时处理表

| 错误类型 | 原因 | PM 处理 |
|---|---|---|
| `CS0246` | 类型找不到，缺 using 或 namespace | 补 `using DrscfZ.UI;` 等；再 refresh |
| `CS0104` | 类型歧义（如 `using System;` 后 `Object.Destroy`） | 加完整前缀 `UnityEngine.Object.Destroy` |
| `Inter-Regular SDF` fallback | TMP 文本含 emoji，字体缺字符 | 源码 emoji → 中文 + `execute_menu_item Tools/DrscfZ/Clean Scene TMP Emoji` |
| `MissingComponentException` | `GetComponent<T>() ?? AddComponent<T>()` 被 Unity fake-null 坑 | Editor 脚本改用 `if (x == null) x = AddComponent<T>()` |
| `NullReferenceException` | `[SerializeField]` 字段未绑定 / 路径错 | 检查 SerializedObject.FindProperty 的 propName 是否匹配脚本字段名 |

### 典型 PM 自动化闭环（§36 实战）

1. 合并 backend + frontend 到 main
2. `refresh_unity` → `read_console filter=CS` → 若有错（如 §31 遗留 CS0104）立即 Edit 修复 → 再 refresh → 再 read → commit 单独「fix:」
3. 改动涉及 TMP：源码扫 emoji（非注释/非 Debug.Log）→ Edit 批量替换 → 写 `CleanSceneEmoji.cs` → `refresh_unity` → `execute_menu_item` Clean 菜单 → `manage_scene save` → `read_console filter='Inter-Regular SDF'` 验 0 → commit
4. 需场景 UI 搭建：写 `SetupSectionXX.cs`（参考 `RebuildGiftIconBar.cs`/`SetupLobbyAndLoadingUI.cs`）→ `refresh_unity` 编译 OK → `execute_menu_item` Setup 菜单 → `manage_scene save` → `read_console types=[error,warning]` 验 0 → commit（含 `.meta` 补一个 chore: commit）
5. 告知用户 Play Mode 观察（唯一需真人的环节）
6. 用户 PASS → PM 更新 MEMORY.md + `git worktree remove` 清 agent worktree

### 禁止项（MCP 自动化）

- ❌ PM 要求用户"在 Unity 里跑 Tools → XXX"（应该 PM 自己调 `execute_menu_item`）
- ❌ PM 要求用户"Ctrl+S 保存场景"（应该 PM 自己调 `manage_scene action='save'`）
- ❌ PM 要求用户"在 Inspector 里绑定 XX 字段"（应该 PM 写 Editor 脚本用 `SerializedObject` + `FindProperty` + `ApplyModifiedPropertiesWithoutUndo` 自动绑）
- ❌ 用 coplay `save_scene`（路径 bug；用 UnityMCP `manage_scene action='save'`）
- ❌ Editor 脚本用 `GetComponent<T>() ?? AddComponent<T>()` 模式（Unity fake-null 陷阱）
- ❌ MCP 操作后不读 Console（必须每步都验）

---

## 测试路径约定

**PM 通过 MCP 自动化操作主 repo `E:\AIProject\DM_CK` 的 Unity 实例。用户仅负责部署服务器 + Play Mode 真人观察。禁止在 worktree 下打开 Unity 或部署服务器。**

### 原因
1. Unity `Library/Temp/Logs` 只在主 repo 维护，worktree 下缺失 → 切 worktree 触发全量 reimport（30 min+）
2. `deploy.py` 按主 repo 路径读文件推服务器
3. worktree 是 Agent 写代码的临时隔离区，分支会清理

### 测试矩阵（按改动类型）

| 改动类型 | PM 自动化（MCP） | 用户动作 | 前置 |
|---------|--------|---------|------|
| 前端（Unity C# / UI / Prefab / Editor） | `refresh_unity` → `execute_menu_item Setup XX` → `manage_scene save` → `read_console` | Play Mode 观察视觉 | 分支已合并到 main |
| 后端（Node.js `Server/src/*`） | — | `python deploy.py` + PM2 重启 | 分支已合并到 main |
| 协议 / 数据结构跨端 | 前端一套同上 | 服务器部署后开 Play | 两端分支都合并到 main |

### 用户测试反馈分流

用户 Play Mode 观察后反馈给 PM：
- **PASS**：PM 更新 `MEMORY.md` "已完成" + `git worktree remove` 清理 agent worktree，流程结束
- **FAIL（小改 <30min）**：PM 新开 worktree 快修 → Reviewer 复核 → 合并 → 通过 MCP 再跑 Setup → 再测
- **FAIL（大改）**：重开一轮 Multi-Agent 完整流程
- **FAIL（策划案缺陷）**：停流程，由用户决定改策划案还是改实现

### 禁止项
- ❌ 在 worktree 下打开 Unity（Library 缺失会触发完整重导）
- ❌ 从 worktree 执行 `deploy.py`（路径错，推的是 worktree 分支代码，易与主 repo 状态不一致）
- ❌ 合并前让用户测（代码在 worktree 分支，主 repo 看不到）
- ❌ 用户未测试就标 "已完成"（Reviewer PASS ≠ 功能正确，只是静态审查通过）
- ❌ PM 能 MCP 完成却让用户手动（见「MCP 自动化规范」禁止项）

---

## 启动话术（用户）

新对话里用户只需说其中一种：

> 用 Multi-Agent 流程跑 §33 助威模式
> 多 Agent 跑 §33
> Multi-Agent §33

PM 读到本文档后自动进入标准流程，无需再配置参数。
