<p align="center">
  <img src="src/FileShelf.Win/Resources/FileShelfIconNotion.ico" alt="FileShelf logo" width="160">
</p>

<h1 align="center">FileShelf</h1>

<p align="center">
  一个便携式 Windows 文件暂存架，用来在资源管理器窗口或其他应用之间临时暂存文件和文件夹。
</p>

<p align="center">
  <a href="README.md">English</a> | 简体中文
</p>

FileShelf 是一个始终置顶的小型文件暂存架，用来保存你马上还会用到的文件路径。把文件或文件夹拖进去，保持它们可见，需要时再拖到另一个资源管理器窗口或应用中。

FileShelf 只保存文件路径。它不会复制、移动、删除、修改、上传或读取文件内容，也不会管理剪贴板历史。

## 基础用法

1. 启动 `FileShelf.Win.exe`。FileShelf 会以浮动文件夹图标的形式停靠在屏幕边缘附近，同时显示在系统托盘中。
2. 添加文件或文件夹：从资源管理器把它们拖到浮动图标上，或拖到已经打开的暂存架面板中。也可以打开面板后点击 `+` 按钮，手动添加文件或文件夹。
3. 需要使用暂存路径时，双击浮动图标打开暂存架，或左键点击托盘图标。
4. 从 FileShelf 把条目拖到资源管理器或其他应用中。可以拖单个条目、已选条目，或拖暂存架顶部的数量手柄。
5. 成功拖出后，未固定的条目只会从 FileShelf 中移除。原始文件和文件夹仍保留在原位置。

FileShelf 的核心逻辑很简单：收集路径，保持待用，再拖到下一个地方。只有那些拖出或清理后仍想保留的条目，才需要固定。

## 使用感受

| 暂存文件 | 保持待用 | 拖出使用 |
| --- | --- | --- |
| 把文件或文件夹拖到 FileShelf 浮动图标上，或拖到打开的暂存架面板中。 | 已固定条目、成组拖入的条目、已选条目和文件缺失提示会持续显示在暂存架中。 | 把单个条目、已选条目或数量手柄拖到资源管理器或其他应用中。 |

## 运行截图

<p align="center">
  <img src="docs/images/fileshelf-shelf.png" alt="FileShelf 打开的暂存架面板，包含暂存文件、已选条目、固定分组和拖出控件" width="320">
</p>

## 主要功能

- 便携式 Windows 应用：无需安装器、注册表项、Shell 扩展或后台服务；可选开机启动只使用当前用户的 Startup 快捷方式。
- 始终置顶的暂存架，用于临时暂存文件和文件夹。
- 可把文件或文件夹拖入暂存架；同一次拖入的文件会保持为一个分组。
- 可通过标准 Windows 文件拖放，把暂存条目拖回其他位置或应用。
- 成功拖出后，只会从 FileShelf 中移除未固定条目；源文件不会被改动。
- 可固定重要条目，使其在拖出或清理后继续保留。
- 可把已选条目堆叠为一个分组，也可把分组拆回独立条目。
- 可在当前应用会话内恢复最近移除的条目。
- 托盘图标控制：左键打开或收起暂存架，右键打开设置、关于或退出。
- 始终置顶的浮动文件夹图标：双击打开暂存架面板，拖动可调整位置，也可直接把文件拖放到图标上暂存。
- 可配置界面语言、开机启动行为和数据路径。
- 支持中文和英文界面切换。

## 用户指南

### 下载和运行

下载便携式 Windows 构建，解压后运行：

```text
FileShelf.Win.exe
```

无需安装。默认情况下，运行数据会保存在可执行文件旁边，因此整个应用文件夹可以作为便携包移动。

### 日常工作流

把 FileShelf 当作一个临时交接区使用：

1. 放入路径：把文件或文件夹拖到浮动文件夹图标上，或打开暂存架并拖入面板。
2. 保持上下文：在处理其他事情时，让暂存架收起为浮动图标。
3. 取出路径：打开暂存架，把需要的条目、选区、分组或数量手柄拖到目标应用中。

同一次拖入的多个文件会成为一个暂存分组。拖出该分组时，会一起发送分组内的所有路径。如果后续需要分别处理，可在条目右键菜单中拆分分组。

### 控制方式

- 浮动文件夹图标：双击打开暂存架面板。
- 拖动浮动文件夹图标：把图标移动到屏幕上的其他位置。
- 浮动文件夹图标拖放目标：把文件释放到图标上即可暂存；移开则取消本次交互。
- 焦点变化：暂存架面板会收起回浮动图标。
- 托盘左键：打开或收起暂存架面板。
- 托盘右键：打开设置、关于或退出。
- 暂存架 `+` 按钮：手动添加文件或文件夹、选择全部待拖出、恢复最近移除的条目，或清理未固定条目。
- 点击条目：选择该条目，用于批量拖出或条目操作。
- 拖动条目：把该条目或当前选区拖出到其他应用。
- 数量手柄：拖出已选条目；如果没有选中条目，则拖出所有暂存条目。
- 条目右键菜单：打开、在资源管理器中显示、固定/取消固定、堆叠已选条目、拆分分组或移除。

### 安全性和便携性

- FileShelf 永远不会修改源文件。
- 从暂存架移除条目，只会删除 FileShelf 保存的路径元数据。
- 运行状态默认保存在可执行文件旁边的 `FileShelfData` 目录中。
- 设置保存在 `FileShelfData\settings.json`。
- 暂存架元数据保存在 `FileShelfData\shelf.json`。
- 构建元数据会编译进程序集；关于窗口版本文本和更新检查不需要额外的元数据文件。
- FileShelf 不会注册全局热键或鼠标钩子。
- 在设置中启用开机启动时，FileShelf 只会创建 `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\FileShelf.Win.lnk`；关闭开机启动会移除该快捷方式。
- 如果应用崩溃，FileShelf 只会留下自身的便携数据和日志文件。

## 开发者指南

### 环境要求

- Windows
- .NET SDK 10.x
- PowerShell

### 从源码构建

在仓库根目录运行：

```powershell
dotnet restore FileShelf.sln -r win-x64
dotnet build FileShelf.sln -c Release --no-restore
```

开发时运行：

```powershell
dotnet run --project src\FileShelf.Win\FileShelf.Win.csproj
```

创建干净的便携式构建：

```powershell
.\scripts\publish-portable.ps1
```

输出目录为：

```text
artifacts\FileShelf-portable-win-x64
```

发布脚本会从便携输出中移除运行时 `FileShelfData` 状态和 `.pdb` 文件。
如果省略 `-Version`，脚本会使用 `Directory.Build.props` 中的项目版本。

指定应用版本：

```powershell
.\scripts\publish-portable.ps1 -Version 0.5.0
```

启用关于窗口中的 GitHub Releases 更新检查：

```powershell
.\scripts\publish-portable.ps1 -Version 0.5.0 -Repository lartpang/FileShelf
```

版本和仓库元数据会编译进应用程序集。

### 项目结构

- `FileShelf.sln`：供 IDE 和命令行构建使用的解决方案文件。
- `src\FileShelf.Win\`：WPF 应用源码。
- `src\FileShelf.Win\Resources\`：应用标志和图标资源。
- `src\FileShelf.Win\Services\`：设置、托盘、拖放和状态服务。
- `src\FileShelf.Win\Models\`：暂存条目和设置模型。
- `scripts\publish-portable.ps1`：干净便携式发布脚本。
- `.github\workflows\release.yml`：GitHub Actions 发布工作流。
- `reference\`：用于 UI 和功能对齐的外部参考截图与说明。

### GitHub 发布自动化

仓库包含一个工作流：推送版本标签后，会发布便携式 Windows zip 包。

使用语义化版本标签：

```powershell
git tag v0.5.0
git push origin v0.5.0
```

工作流会：

1. 检出带标签的源码。
2. 在 Windows runner 上安装 .NET 10.x。
3. 为 `win-x64` 还原并发布 `FileShelf.Win`。
4. 从标签向程序集写入应用版本，例如 `v0.5.0` -> `0.5.0`。
5. 压缩干净的便携式输出。
6. 创建 GitHub Release 并上传 zip 资源。

你也可以在 GitHub Actions 中使用已有标签手动运行该工作流，例如 `v0.5.0`。

如果创建 Release 时出现权限错误，请打开 GitHub 仓库设置，确认 Actions 可以创建 releases。工作流本身只请求 `contents: write` 权限。

### 开发注意事项

- 保持应用便携：不要添加安装器、注册表写入、Shell 扩展或后台服务；开机启动保持为可选项，并限制为当前用户的 Startup 快捷方式。
- 保持文件暂存架的范围克制：不要添加剪贴板历史或文件内容索引。
- 把暂存文件视为外部用户数据：只保存路径。
- 优先进行小范围 WPF 修改，保持当前以暂存架为核心的工作流。
- 修改后运行：

```powershell
dotnet build FileShelf.sln -c Release --no-restore
.\scripts\publish-portable.ps1
```
