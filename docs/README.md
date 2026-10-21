# 使用与兼容性说明

**此项目由 AI 构建，使用 OpenAI Codex 生成和迭代，由 piwric77 维护与发布。**

## 开始使用

从 Releases 下载安装程序，退出旧版 KeyFlow 后安装。单击、双击、长按分别填写网址、程序或快捷方式，点击“保存并生效”。关闭窗口会保持后台运行；隐藏托盘后再次运行 KeyFlow 可找回设置。

## 联想 SmartKey

保留联想 Fn 服务，在偏好设置中启用“联想 SmartKey 兼容接管”并保存。按实体键检查接收计数和动作；创建兼容窗口成功不等于本机实体键已接管。若原厂 SmartKey 窗口占用，工具会提示冲突，不会停止 Fn 服务或结束原厂进程。

兼容接口来自 Fn and Function Keys 2.0.13.10 的静态分析：SmartKey / SmartEngine 路径消息为 0x13A2，自定义键路径为 0x13A3。该旧接口只有点击消息，只支持单击和双击；Copilot / F23 路径保留长按功能。某些设备可能走 AI Now、XiaoLe 等其他优先入口，需逐机验证。

## 完整资料

Release 附件 [KeyFlow-3.1.0-beta.1-Source.zip](https://github.com/piwric77/KeyFlow/releases/download/v3.1.0-beta.1/KeyFlow-3.1.0-beta.1-Source.zip) 包含完整源码、安装脚本、详细使用说明和静态逆向报告。

## 验证范围

13 项逻辑和接口测试通过，另检查了 WinUI 窗口、深浅色主题、键盘钩子、托盘、暂停与恢复、单实例窗口找回。625 个安装包应用文件逐一通过解包哈希校验。隔离安装验证关闭系统卸载登记；标准卸载登记、开始菜单入口、实体 SmartKey 和其他 Fn 功能保留仍需实际环境验证。
