# PCL Nexa 2.x 设置系统最终 IA

## 0. IA 固定原则

### 设置系统原则

```text
IA ≠ 当前实现状态
```

功能尚未实现时：

```text
选项正常存在
↓
Disabled
↓
标注「尚未可用」
```

不得因为功能尚未完成而：

- 临时删除选项
- 改变页面归属
- 新建临时页面
- 后期完成后重新调整 IA
- 将高级功能随意堆入“高级”页面

### UI 原则

当前窗口继续采用：

> **紧凑、小巧、高信息密度。**

因此设置页：

- 不设置 Hero Header
- 不设置顶部大标题留白
- 不设置模式切换栏
- 不设置账户入口
- 页面打开后立即显示设置内容
- 一级导航保持窄且稳定
- Section 之间使用较小间距
- 说明只在需要解释风险或行为时出现
- 未来宽窗口仅改变布局，不改变 IA

### 开发者选项

不设置：

```text
新手模式
高级模式
开发者模式
```

只存在一个：

```text
开发者选项    [开 / 关]
```

关闭时隐藏开发者级项目。

开启后：

- 原有页面出现额外 Developer Section
- 不新增另一套设置体系
- 不改变普通设置的位置
- 不改变页面导航层级

---

# 1. 全局设置 IA

最终固定为 **9 个一级页面**：

```text
设置
│
├─ 1. 通用
├─ 2. 外观
├─ 3. 游戏
├─ 4. Java
├─ 5. 下载与网络
├─ 6. 存储与迁移
├─ 7. N Cloud
├─ 8. 隐私与诊断
└─ 9. 更新与高级
```

明确不存在：

```text
× 账户与身份
× 联机与服务器
× 插件与扩展
× 用户模式
```

其中：

- 账户管理属于主页
- 联机属于后期 Sidecar
- 插件属于后期 Sidecar
- 开发者能力由 Developer Options 控制

---

# 2. 通用

```text
通用
│
├─ 语言与区域
│  ├─ 界面语言
│  ├─ 格式区域
│  └─ 跟随系统
│
├─ 启动器行为
│  ├─ 开机启动
│  ├─ 单实例运行
│  ├─ 启动时行为
│  ├─ 最小化行为
│  ├─ 关闭按钮行为
│  └─ 后台运行
│
├─ 交互
│  ├─ 启动提示
│  ├─ 通知
│  ├─ 操作确认
│  └─ 剪贴板内容检测
│
└─ 系统集成
   ├─ 文件关联
   ├─ nexa:// URI
   ├─ Windows Jump List
   └─ 系统通知操作
```

---

# 3. 外观

```text
外观
│
├─ 主题
│  ├─ 系统 / 浅色 / 深色
│  ├─ 强调色
│  ├─ 自定义主题
│  └─ Launcher Logo
│
├─ 背景
│  ├─ 背景来源
│  ├─ 背景适配
│  ├─ 背景透明度
│  ├─ 彩色背景
│  └─ 视频背景自动暂停
│
├─ 窗口
│  ├─ Launcher 透明度
│  ├─ 模糊
│  ├─ 模糊强度
│  ├─ 模糊采样
│  └─ 锁定窗口大小
│
├─ 动画
│  ├─ 启用动画
│  ├─ UI 动画帧率
│  ├─ 减少动态效果
│  └─ 超低功耗模式
│
└─ 背景音乐
   ├─ 启用背景音乐
   ├─ 启动时播放
   ├─ 自动播放
   ├─ 随机播放
   ├─ 音量
   └─ 系统媒体控制
```

开发者选项开启：

```text
开发者
├─ UI Renderer 信息
├─ Layout / Paint 调试
├─ Animation Scheduler
└─ UI 性能指标
```

---

# 4. 游戏

这里定义：

> **所有实例的默认 Minecraft Launch Policy。**

版本自己的覆盖放在实例设置。

```text
游戏
│
├─ 默认资源配置
│  ├─ 内存策略
│  ├─ 默认内存
│  ├─ GPU / Renderer
│  └─ 进程优先级
│
├─ 游戏窗口
│  ├─ 默认窗口模式
│  ├─ 默认宽度
│  ├─ 默认高度
│  └─ 默认窗口标题
│
├─ 启动行为
│  ├─ Minecraft 启动后 Nexa 行为
│  ├─ 游戏退出后 Nexa 行为
│  ├─ 启动提示
│  ├─ 自动修复
│  └─ Game Resource Handoff
│
├─ 实例
│  ├─ 默认实例隔离
│  ├─ 默认游戏目录策略
│  └─ Assets 验证策略
│
├─ 智能启动
│  ├─ Launch Preflight
│  ├─ 资源预算检查
│  ├─ Compatibility Check
│  └─ 自动 Repair Plan
│
└─ 默认高级启动
   ├─ JVM Arguments
   ├─ Game Arguments
   ├─ Wrapper Command
   └─ Pre-launch Command
```

开发者：

```text
开发者
├─ 默认 Renderer Backend
├─ Native Compatibility
├─ System GLFW
├─ Force X11 on Wayland
├─ LWJGL Unsafe Agent
└─ Effective Launch Template
```

---

# 5. Java

```text
Java
│
├─ 自动策略
│  ├─ 自动选择兼容 Java
│  ├─ 自动安装缺失 Java
│  ├─ 首选 Java
│  ├─ 首选发行版
│  └─ Java 兼容性检查
│
├─ 已安装 Java
│  └─ Runtime List
│     ├─ Version
│     ├─ Vendor
│     ├─ Architecture
│     ├─ Path
│     └─ Status
│
└─ Java 管理
   ├─ 扫描本机 Java
   ├─ 添加自定义 Java
   ├─ 下载 Java
   ├─ 验证 Java
   └─ 删除托管 Java
```

Runtime Detail：

```text
Java Runtime
├─ 版本
├─ Vendor
├─ Architecture
├─ Path
├─ Compatibility
├─ Managed / External
│
├─ 设为首选
├─ 验证
├─ 打开目录
└─ 删除
```

开发者：

```text
开发者
├─ JVM Runtime Probe
├─ Runtime Capabilities
├─ Detected Modules
└─ Raw Runtime Information
```

---

# 6. 下载与网络

```text
下载与网络
│
├─ 下载
│  ├─ 下载源策略
│  ├─ 下载线程
│  ├─ 下载速度限制
│  ├─ 后台下载
│  ├─ 自动重试
│  └─ 自动安装依赖
│
├─ 内容来源
│  ├─ Mojang
│  ├─ Modrinth
│  ├─ CurseForge
│  ├─ 镜像源
│  └─ 来源优先级
│
├─ 网络
│  ├─ 首选 IP 栈
│  ├─ DNS over HTTPS
│  ├─ 网络自动检测
│  └─ 网络故障诊断
│
└─ 代理
   ├─ 不使用
   ├─ 跟随系统
   └─ 自定义
      ├─ 地址
      ├─ 用户名
      └─ 密码
```

开发者：

```text
开发者
├─ Endpoint 状态
├─ Network Probe
├─ Mirror Resolution
├─ Retry Trace
└─ Download Diagnostics
```

---

# 7. 存储与迁移

```text
存储与迁移
│
├─ 数据位置
│  ├─ Minecraft Library
│  ├─ 实例目录
│  ├─ 默认实例位置
│  ├─ Java Runtime Store
│  ├─ 下载缓存
│  └─ 临时目录
│
├─ 空间使用
│  ├─ 实例逻辑占用
│  ├─ 实际磁盘占用
│  ├─ Cache
│  ├─ Snapshot
│  └─ 已节省空间
│
├─ 存储优化
│  ├─ CAS
│  ├─ Deduplication
│  ├─ Hardlink / Reflink
│  ├─ Copy-on-Write
│  └─ Integrity Verification
│
├─ 清理
│  ├─ 下载缓存
│  ├─ 临时文件
│  ├─ 无引用内容
│  ├─ 旧 Snapshot
│  └─ 存储优化建议
│
├─ Snapshot
│  ├─ 自动 Snapshot
│  ├─ 保留数量
│  ├─ 最大占用
│  └─ 自动清理策略
│
├─ 迁移
│  ├─ PCL
│  ├─ Prism
│  ├─ Modrinth
│  ├─ CurseForge
│  ├─ HMCL
│  ├─ XMCL
│  ├─ ATLauncher
│  └─ 普通 Minecraft 目录
│
└─ Portable
   ├─ Portable Mode
   ├─ 搬家检查
   └─ 路径依赖检查
```

---

# 8. N Cloud

```text
N Cloud
│
├─ 状态
│  ├─ Cloud Account
│  ├─ Plan
│  ├─ 已用空间
│  └─ 可用空间
│
├─ 同步
│  ├─ Minecraft Options
│  ├─ Server List
│  ├─ Resource Packs
│  ├─ Command History
│  ├─ Creative Hotbars
│  ├─ Screenshots
│  └─ Instance Metadata
│
├─ 备份
│  ├─ 自动备份
│  ├─ Worlds
│  ├─ Config
│  ├─ Instance Metadata
│  └─ Thin Backup
│
├─ 同步冲突
│  ├─ 自动解决
│  ├─ 使用较新版本
│  └─ 每次询问
│
└─ 设备
   ├─ 当前设备
   ├─ 已连接设备
   └─ 设备同步状态
```

---

# 9. 隐私与诊断

```text
隐私与诊断
│
├─ 数据与隐私
│  ├─ 使用体验计划
│  ├─ Crash Report
│  └─ Diagnostic Data
│
├─ 日志
│  ├─ 日志等级
│  ├─ 日志保留
│  ├─ 最大日志数量
│  ├─ 打开日志目录
│  └─ 导出日志
│
├─ 隐私保护
│  ├─ 自动脱敏
│  ├─ 安全复制
│  └─ Diagnostic Bundle Privacy
│
├─ Minecraft 诊断
│  ├─ Crash Analyzer
│  ├─ Launch Diagnostics
│  ├─ Resource Exhaustion
│  └─ Recent-change Diagnosis
│
├─ 系统诊断
│  ├─ Windows Event Correlation
│  ├─ GPU / TDR
│  ├─ WHEA
│  └─ Native Crash Correlation
│
├─ 内容可信
│  ├─ Content Provenance
│  ├─ Hash Verification
│  ├─ Modified File Warning
│  └─ Unknown JAR Policy
│
└─ AI 诊断
   ├─ 启用 AI
   ├─ Provider
   ├─ Model
   ├─ Reasoning
   ├─ Token Budget
   └─ 数据共享范围
```

开发者：

```text
开发者
├─ Realtime Log
├─ XSR Operation Trace
├─ State Trace
├─ Launch Trace
└─ Raw Diagnostic Data
```

---

# 10. 更新与高级

```text
更新与高级
│
├─ Nexa 更新
│  ├─ 自动检查更新
│  ├─ 更新通道
│  │  ├─ Stable
│  │  ├─ Beta
│  │  └─ Preview / Nightly
│  ├─ 后台下载
│  └─ Release Notes
│
├─ 更新安全
│  ├─ Signature Verification
│  ├─ Atomic Update
│  ├─ Last Known Good
│  ├─ Auto Rollback
│  └─ Launcher Safe Mode
│
├─ 系统兼容
│  ├─ 硬件加速
│  ├─ 系统兼容策略
│  └─ Compatibility Workaround
│
├─ 自动化
│  ├─ nexa:// Deep Link
│  ├─ CLI Integration
│  └─ Command Palette Integration
│
├─ 实验功能
│  ├─ Experimental Homepage
│  ├─ Next Render Backend
│  ├─ Launch Shortcuts
│  └─ Minecraft AI Repair
│
├─ 设置数据
│  ├─ 导入设置
│  ├─ 导出设置
│  └─ 恢复默认
│
└─ 开发者选项
   └─ [ Off / On ]
```

开发者选项开启后追加：

```text
开发者
├─ Debug Mode
├─ Debug Delay
├─ Debug Animation
├─ Debug Skip Copy
│
├─ XSR State Inspector
├─ XSR Operation Log
├─ Renderer Diagnostics
├─ Runtime Diagnostics
│
├─ Raw Settings
├─ 打开 settings.json
└─ Internal Feature Flags
```

---

# 11. 实例详情 IA

实例详情固定为：

```text
实例
│
├─ 1. 概览
├─ 2. 内容
├─ 3. 世界
├─ 4. 服务器
├─ 5. 文件
├─ 6. 截图
├─ 7. 变化与恢复
├─ 8. 诊断
└─ 9. 设置
```

---

# 12. 概览

```text
概览
├─ 名称 / 图标
├─ Minecraft
├─ Loader
├─ Modpack
├─ Java
├─ 最近启动
├─ 游戏时间
├─ Instance Health
├─ Resource Summary
│
├─ 启动
├─ 修改版本
├─ 打开目录
└─ 更多操作
```

## 修改版本

`修改版本` 是 **独立页面**。

```text
实例概览
   ↓
修改版本
   ↓
Version Modification Page
```

不属于：

```text
× 实例设置
× 游戏版本设置
× Loader 设置
```

该页面保持现有已完成实现。

---

# 13. 内容

```text
内容
│
├─ Mods
├─ Resource Packs
├─ Shaders
├─ Data Packs
│
├─ 已安装
├─ 可更新
├─ 禁用
└─ 冲突 / 异常
```

内容安装和内容管理都进入该页。

---

# 14. 世界

```text
世界
│
├─ World List
├─ Last Played
├─ Minecraft / DataVersion
├─ Size
├─ Health
│
├─ 打开
├─ 备份
├─ Snapshot
├─ Duplicate
└─ Delete
```

未来 World Guardian 继续填入这里，不新建一级页。

---

# 15. 服务器

```text
服务器
│
├─ Server List
├─ 默认服务器
├─ Compatibility Status
├─ Environment Match
└─ Quick Play
```

这里只管理 **这个实例里的 Minecraft Server Environment**。

不包含 Nexa 联机系统。

---

# 16. 文件

```text
文件
│
├─ Instance Root
├─ Config
├─ Logs
├─ Crash Reports
├─ Saves
├─ Mods
├─ Resource Packs
└─ Other
```

属于实例文件浏览器。

---

# 17. 截图

```text
截图
│
├─ Gallery
├─ Timeline
├─ Folder
├─ Copy
├─ Share
└─ Delete
```

---

# 18. 变化与恢复

这是 Nexa 核心实例功能之一。

```text
变化与恢复
│
├─ Change Timeline
├─ Last Known Good
├─ Snapshots
├─ Update History
│
├─ Diff
├─ Undo
├─ Restore
└─ Rollback
```

未来所有实例修改统一进入：

```text
Instance Change Journal
```

---

# 19. 诊断

```text
诊断
│
├─ 当前状态
├─ Launch Preflight
├─ Launch History
├─ Crash History
│
├─ 启动性能
├─ 最近变化分析
├─ 依赖 / 冲突
├─ Resource Analysis
├─ System Correlation
│
├─ Repair Plan
└─ Diagnostic Bundle
```

---

# 20. 实例设置最终 IA

最终固定为 **10 个 Section**：

```text
实例设置
│
├─ 1. 基本
├─ 2. Java
├─ 3. 资源与性能
├─ 4. 窗口与启动
├─ 5. JVM 与 Hooks
├─ 6. 启动配置
├─ 7. 服务器
├─ 8. 更新与安全
├─ 9. 同步与备份
└─ 10. 高级
```

---

# 21. 实例设置 / 基本

```text
基本
│
├─ 名称
├─ 描述
├─ 图标
├─ 收藏
├─ Tags / Group
├─ Notes
├─ Custom Info
│
├─ 实例目录
├─ Instance Isolation
│
├─ Modpack
│  ├─ Project
│  └─ Version
│
└─ Window Title
   ├─ 继承全局
   └─ 自定义
```

---

# 22. 实例设置 / Java

统一三态：

```text
Java
├─ 继承全局
├─ 自动
└─ 指定 Runtime
```

内容：

```text
Java
│
├─ Java Policy
├─ Selected Runtime
├─ Runtime Information
├─ Compatibility
│
├─ 扫描
├─ 下载
└─ 验证
```

高级：

```text
忽略 Java 兼容性检查
```

---

# 23. 实例设置 / 资源与性能

```text
资源与性能
│
├─ 内存
│  ├─ 继承全局
│  ├─ 自动
│  └─ 自定义
│
├─ Resource Estimator
│  ├─ Recommended Xmx
│  ├─ RAM Estimate
│  ├─ Startup Peak
│  ├─ Commit Estimate
│  └─ Historical Calibration
│
├─ GPU
│  ├─ 自动
│  ├─ 指定 GPU
│  ├─ Renderer
│  ├─ VRAM Estimate
│  └─ iGPU Shared Memory
│
├─ Hardware Advice
│  ├─ Shader
│  ├─ Resource Pack
│  ├─ Render Distance
│  └─ Risk
│
├─ Process Priority
└─ Game Resource Handoff
```

---

# 24. 实例设置 / 窗口与启动

```text
窗口与启动
│
├─ 游戏窗口
│  ├─ 继承全局
│  ├─ Window Mode
│  ├─ Width
│  └─ Height
│
├─ 标题
│  ├─ 使用全局标题
│  └─ 自定义
│
├─ 启动时
│  ├─ Nexa 保持显示
│  ├─ 最小化
│  ├─ 隐藏
│  └─ 关闭
│
└─ 游戏结束
   └─ Nexa 恢复行为
```

---

# 25. 实例设置 / JVM 与 Hooks

```text
JVM 与 Hooks
│
├─ JVM
│  ├─ JVM Arguments
│  ├─ Game Arguments
│  ├─ Classpath Head
│  └─ Environment Variables
│
├─ Wrapper
│  └─ Wrapper Command
│
├─ Pre-launch
│  ├─ Command
│  └─ Wait for Completion
│
├─ Post-exit
│  └─ Command
│
└─ Native Compatibility
   ├─ Disable JLW
   ├─ Disable RW
   ├─ Debug Log4j
   ├─ Disable LWJGL Unsafe Agent
   ├─ System GLFW
   └─ Force X11 on Wayland
```

---

# 26. 实例设置 / 启动配置

这里承载 Nexa Overlay Architecture。

```text
启动配置
│
├─ Profiles
│  ├─ Default
│  ├─ Performance
│  ├─ Screenshot
│  ├─ Multiplayer
│  └─ Debug
│
├─ Profile Override
│  ├─ Mods
│  ├─ Resource Packs
│  ├─ Shader
│  ├─ Config
│  ├─ JVM
│  ├─ Memory
│  └─ Server
│
├─ Temporary Overlay
│  ├─ 临时 Mods
│  ├─ 临时 Resource Packs
│  ├─ 临时 Shader
│  ├─ 临时 Config
│  └─ 临时 JVM Arguments
│
└─ Safe Launch
   ├─ 禁用最近新增 Mods
   ├─ Disable Shader
   ├─ Disable Resource Packs
   ├─ Disable Hooks
   └─ Disable Custom JVM Args
```

---

# 27. 实例设置 / 服务器

不负责账户管理。

```text
服务器
│
├─ Quick Play
│  ├─ None
│  └─ Server
│
├─ 默认服务器
│  └─ Address
│
└─ 服务器认证
   ├─ Requirement
   ├─ Auth Server
   ├─ Register URL
   ├─ Display Name
   └─ Lock Authentication Settings
```

原则：

> **主页决定使用哪个账户，实例只描述该环境怎样启动。**

---

# 28. 实例设置 / 更新与安全

```text
更新与安全
│
├─ 内容更新
│  ├─ 自动检查
│  ├─ Update Policy
│  └─ Source Lock
│
├─ Update Impact
│  ├─ Dependency Impact
│  ├─ Addon Compatibility
│  ├─ Loader Compatibility
│  └─ Risk Assessment
│
├─ Safe Update
│  ├─ 更新前 Snapshot
│  └─ Failure Rollback
│
├─ Launch Preflight
│  ├─ Critical → Block
│  ├─ Warning → Ask
│  └─ Ignore Policy
│
├─ World Guardian
│  ├─ Upgrade Protection
│  ├─ Downgrade Protection
│  ├─ Auto Snapshot
│  └─ World Health
│
├─ 文件完整性
│  ├─ Assets Verification
│  ├─ Libraries Verification
│  └─ Content Integrity
│
└─ Content Provenance
   ├─ Source
   ├─ SHA-256
   ├─ Modified Status
   └─ Unknown Content Policy
```

---

# 29. 实例设置 / 同步与备份

所有项目支持：

```text
继承全局 / 实例覆盖
```

结构：

```text
同步与备份
│
├─ N Cloud Sync
│  ├─ Game Options
│  ├─ Servers
│  ├─ Resource Packs
│  ├─ Command History
│  ├─ Hotbars
│  └─ Metadata
│
├─ Backup
│  ├─ Worlds
│  ├─ Config
│  ├─ Metadata
│  └─ Screenshots
│
├─ Thin Backup
│  ├─ Manifest
│  ├─ Lockfile
│  └─ Non-reproducible Data
│
└─ Offline Readiness
   ├─ Account
   ├─ Java
   ├─ Minecraft Client
   ├─ Libraries
   ├─ Assets
   ├─ Loader
   ├─ Mods
   └─ Prepare Offline
```

---

# 30. 实例设置 / 高级

正常状态下：

```text
高级
│
├─ Ignore Compatibility
├─ Disable Asset Verification
└─ Other Compatibility Overrides
```

开发者选项开启后：

```text
开发者
│
├─ Effective State
├─ Effective Launch Plan
├─ Effective JVM Command
├─ Effective Classpath
├─ Environment Variables
├─ Native Libraries
│
├─ Manifest
├─ Lockfile
├─ Raw Instance Metadata
│
├─ Launch Trace
├─ XSR State
└─ Operation History
```

---

# 31. 设置继承模型

后续所有可覆盖设置统一使用：

```text
Builtin Default
        ↓
Global Setting
        ↓
Instance Override
        ↓
Profile Overlay
        ↓
Temporary Overlay
        ↓
Effective Value
```

UI 统一表现：

```text
● 继承全局
○ 自动
○ 自定义
```

不是所有设置都必须有三种状态，但所有支持 override 的设置都必须遵循这一模型。

---

# 32. 未实现功能的 UI 规则

IA 从现在起保持稳定。

尚未实现：

```text
Resource Estimator
CAS
N Cloud
World Guardian
Safe Launch
Overlay
Preflight
Change Journal
Lockfile
...
```

都保留在最终位置。

统一显示：

```text
资源预算器

根据实例、Mod、资源包和硬件预测运行资源需求。

[ 尚未可用 ]
```

或者：

```text
自动创建更新前快照          ○
                              尚未可用
```

禁止使用：

```text
即将推出
敬请期待
Coming Soon
```

作为独立页面。

一个尚未实现的功能 **只能是禁用的最终控件，而不是临时 IA**。

---

# 33. 最终 IA 总览

```text
Nexa Settings
├─ 通用
├─ 外观
├─ 游戏
├─ Java
├─ 下载与网络
├─ 存储与迁移
├─ N Cloud
├─ 隐私与诊断
└─ 更新与高级
```

```text
Instance
├─ 概览
├─ 内容
├─ 世界
├─ 服务器
├─ 文件
├─ 截图
├─ 变化与恢复
├─ 诊断
└─ 设置
```

```text
Instance Settings
├─ 基本
├─ Java
├─ 资源与性能
├─ 窗口与启动
├─ JVM 与 Hooks
├─ 启动配置
├─ 服务器
├─ 更新与安全
├─ 同步与备份
└─ 高级
```

```text
Independent Instance Actions
└─ 修改版本
```

这四棵结构从 Nexa 2.x 开始应视为 **稳定产品 IA Contract**。

功能可以增加、实现可以逐步完成、Sidecar 可以继续扩展，但现有本体能力不再因为迁移进度重新安排页面。