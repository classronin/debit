

debit 文本算式编辑器

debit是一个轻量级的文本算式编辑器，旨在让你像写笔记一样快速进行数学计算。在文本中直接输入算式，按下等号即可得到结果，无需切换到计算器。


![截图](https://github.com/classronin/picx-images-hosting/raw/master/debit_e7kGeVn3Yc.5xb8d75779.gif)


下载Windows便捷版 [debit.7z](https://github.com/classronin/debit/releases/latest/download/debit.7z)


主要功能
>按下 = 键，将当前行将替换为表达式与计算结果。
>输入 10+20= 会立即变为 10+20=30
>浅色和暗黑主题，可在设置菜单中切换。
>通过快捷键 Ctrl++ 和 Ctrl+- 或 Ctrl+鼠标滚轮 可随时放大或缩小字体。
>支持多标签页编辑。


依赖
```
uv venv
uv pip install PySide6
uv run main.py
```



打包为独立可执行文件，Nuitka 文件夹模式 打包命令
```
uv venv
uv pip install nuitka PySide6
uv run python -m nuitka --standalone --windows-console-mode=disable --lto=yes --assume-yes-for-downloads --company-name="classronin" --product-name="debit" --file-version="0.0.5.0" --product-version="0.0.5" --copyright="Copyright © 2026 classronin" --windows-icon-from-ico="icon.ico" --output-filename="debit.exe" --enable-plugin=pyside6 --noinclude-dlls=Qt6Network.dll --noinclude-dlls=Qt6Pdf.dll --noinclude-dlls=Qt6Svg.dll --noinclude-dlls=libcrypto-3-x64.dll --include-data-files="SarasaMonoSC-Regular.ttf=./SarasaMonoSC-Regular.ttf" main.py
```


删除网络与未绑定应用的文件/目录
```
iconengines
imageformats
tls
PySide6\QtNetwork.pyd
```
