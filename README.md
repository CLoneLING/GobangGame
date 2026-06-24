# 五子棋 - 全能对战

一款基于 C# WinForms 开发的五子棋对战程序，支持**本地双人对战**、**人机对战（简单 AI）** 和 **局域网/互联网联机对战**.

[![Support](https://img.shields.io/badge/Windows-支持-green/?logo=data:image/svg+xml;charset=utf-8;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz48IS0tIFVwbG9hZGVkIHRvOiBTVkcgUmVwbywgd3d3LnN2Z3JlcG8uY29tLCBHZW5lcmF0b3I6IFNWRyBSZXBvIE1peGVyIFRvb2xzIC0tPgo8c3ZnIHdpZHRoPSI4MDBweCIgaGVpZ2h0PSI4MDBweCIgdmlld0JveD0iMCAwIDE2IDE2IiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIGZpbGw9Im5vbmUiPjxwYXRoIGZpbGw9IiNGMzUzMjUiIGQ9Ik0xIDFoNi41djYuNUgxVjF6Ii8+PHBhdGggZmlsbD0iIzgxQkMwNiIgZD0iTTguNSAxSDE1djYuNUg4LjVWMXoiLz48cGF0aCBmaWxsPSIjMDVBNkYwIiBkPSJNMSA4LjVoNi41VjE1SDFWOC41eiIvPjxwYXRoIGZpbGw9IiNGRkJBMDgiIGQ9Ik04LjUgOC41SDE1VjE1SDguNVY4LjV6Ii8+PC9zdmc+)](https://github.com/CLoneLING/WindowsGarbageCleaner)
[![Buildon](https://img.shields.io/badge/Build_on-C%23-green/?logo=dotnet)](https://learn.microsoft.com/zh-cn/dotnet/csharp/)

---

## ✨ 功能特点

- 🎮 **本地双人对战**：两人轮流在同一台电脑上落子.
- 🤖 **人机对战**：AI 基于贪心评分（含防守权重），适合新手练习.
- 🌐 **联机对战**：
  - **房主模式**：创建房间，等待玩家加入.
  - **成员模式**：通过 IP:端口 加入房间.
  - 支持**内网直连**和**公网穿透**（需自行配置端口映射或穿透工具）.
  - 房主可控制开始/重开游戏，双方均可发起悔棋（需对方同意，联机中默认隐藏）.
- 🎨 **美观界面**：标准 15×15 棋盘，带星位标记，最新落子带红点提示.
- 📋 **IP 显示**：房主创建房间后自动显示内网和公网 IP（可点击复制）.
- ♻️ **游戏重置**：任意一方点击“重新开始”即可重置对局（联机中无需对方确认）.

---

## 🚀 如何运行

### 环境要求
- ![logo](data:image/svg+xml;charset=utf-8;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz48IS0tIFVwbG9hZGVkIHRvOiBTVkcgUmVwbywgd3d3LnN2Z3JlcG8uY29tLCBHZW5lcmF0b3I6IFNWRyBSZXBvIE1peGVyIFRvb2xzIC0tPgo8c3ZnIHdpZHRoPSI4MDBweCIgaGVpZ2h0PSI4MDBweCIgdmlld0JveD0iMCAwIDE2IDE2IiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIGZpbGw9Im5vbmUiPjxwYXRoIGZpbGw9IiNGMzUzMjUiIGQ9Ik0xIDFoNi41djYuNUgxVjF6Ii8+PHBhdGggZmlsbD0iIzgxQkMwNiIgZD0iTTguNSAxSDE1djYuNUg4LjVWMXoiLz48cGF0aCBmaWxsPSIjMDVBNkYwIiBkPSJNMSA4LjVoNi41VjE1SDFWOC41eiIvPjxwYXRoIGZpbGw9IiNGRkJBMDgiIGQ9Ik04LjUgOC41SDE1VjE1SDguNVY4LjV6Ii8+PC9zdmc+)  Windows 7 及以上
- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48) 或更高版本.

### 快速开始
1. 下载最新 Release 中的 `GobangGame.exe` 或自行编译.
2. 双击运行，从主菜单选择游戏模式.
3. 享受对弈！

### 编译源码（可选）
- 使用 Visual Studio 2019/2022 打开 `.sln` 文件 
- 确保已安装 NuGet 包 `System.Text.Json` 
- 生成 → 重新生成解决方案 

---

## 🕹️ 使用方法

### 本地双人对战
- 黑方先走，轮流点击棋盘交叉点落子.
- 使用 **悔棋** 按钮撤销上一步（仅限本地模式）.
- 游戏结束后可点击 **新游戏** 重置棋盘.

### 人机对战
- 玩家执黑，AI 执白。
- AI 会尝试防守并进攻（简单模式）。
- **悔棋** 会撤销自己的一步和 AI 的一步（回到玩家回合）.

### 联机对战（房主）
1. 点击 **创建房间**，输入端口号（默认 8888）.
2. 程序会显示本机的内网 IP 和公网 IP（如果可获取）.
3. 将 **公网 IP:端口**（或内网 IP）告诉好友.
4. 等待好友加入，点击 **开始游戏** 开始对局.
5. 对局中点击 **重新开始** 可重置双方棋盘（无需确认）.

### 联机对战（成员）
1. 点击 **加入房间**，输入房主的 IP:端口.
2. 连接成功后等待房主开始游戏.
3. 对局中点击 **重新开始** 同样会重置双方棋盘.

---

## 🌐 联机说明

- **内网联机**：双方在同一局域网下，成员输入房主的内网 IP 即可.
- **外网联机（互联网）**：
  - 房主需拥有**公网 IP** 或在路由器中设置**端口映射**.
  - 若无公网 IP，可使用内网穿透工具（如 [ZeroTier](https://www.zerotier.com/)、[frp](https://github.com/fatedier/frp)、[nat123](http://www.nat123.com/)）将本地端口映射到公网。
  - 成员连接时输入穿透工具分配的公网地址和端口.

> 💡 提示：本机测试可直接使用 `127.0.0.1:端口` 进行双开调试.

---

## 🛠️ 技术栈

- **语言**：C#
- **框架**：.NET Framework 4.8
- **UI**：Windows Forms（双缓冲绘制）
- **网络**：TCP Socket + JSON 序列化（System.Text.Json）
- **AI**：贪心评分 + 简单防守权重

---

## 🤝 贡献

欢迎提交 Issue 或 Pull Request。  
如果你发现了 Bug 或有改进建议，请在 GitHub 上提出.