

### debit 文本算式编辑器

debit 是一个轻量级的文本算式编辑器，无需切换到计算器。




![截图](https://github.com/classronin/picx-images-hosting/raw/master/debit_kb86ZuUkyo.lwbxx3hp5.gif)


下载Windows便捷版 [debit.zip](https://github.com/classronin/debit/releases/latest/download/debit.zip)



主要功能
- 按下 = 键，将当前行替换为表达式与计算结果。
- 例如输入 10+20= 会立即变为 10+20=30。
- 支持浅色和深色主题，可在设置菜单中切换。
- 支持将字母 x、X 智能识别为乘号。
- 支持中英文混合算式，例如 我借5+烟5= 会计算为 10。
- 退出时自动保存窗口大小、位置、字体大小和打开的文件，下次启动恢复。

---

源码依赖和打包命令
开发依赖
- .NET SDK 8.0 或更高版本
- 无需额外 NuGet 包（项目使用 WPF 内置库）

开发还原
```
dotnet restore
```

打包为独立可执行文件（自包含，用户无需安装 .NET）
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```
>图标已在 debit.csproj 中通过 <ApplicationIcon>icon.ico</ApplicationIcon> 设置，打包后会自动附加到 exe。
>生成的文件位于 publish 文件夹，发布时需将整个 publish 文件夹内的所有文件一起分发（或压缩为 ZIP）。

可选清理（发布前清理旧输出）
```
dotnet clean
```


