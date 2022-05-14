# ProcessGuard

[English](README.md)

关于实现的具体依据请查看[通过Windows系统服务守护进程的运行](https://lambda.cyou/posts/Tips-5/)

得益于能从Windows系统服务中启动任意进程的能力，围绕这个能力，此程序可以用来：

1. 从Windows系统服务启动带交互界面的程序，并在其被关闭后再次将其启动
2. 将一些程序配置为开机自启
3. 对于控制台类型的应用，包括但不限于`java`，`dotnet`，`node`等类型的程序，可以通过无窗应用的启动方式，将其像系统服务一样部署在Windows系统上

## ⚙配置界面

> 从[Release](https://github.com/KamenRiderKuuga/ProcessGuard/releases)页面可以直接下载程序，启动程序后看到的界面只是一个配置界面，可以在这里配置要守护的进程，启动服务之后可以随时开启或关闭此配置界面

![](https://lambda.cyou/assets/img/processguard-8.PNG)

注：只有在界面点击启动服务，守护服务正常运行后，配置才能生效



## 📕配置说明

**进程名称：** 用于标识当前配置项的名称，仅用于界面显示

**完整路径：** 可执行文件的完整路径

**启动参数：** 也就是平时启动应用时携带的参数，如不需要携带参数可忽略此项

**仅启动一次：** 在守护服务运行期间只启动一次，用于只需要配置开机启动的场景

**最小化：** 对于有交互界面的程序，配置此项可以让其启动时最小化到任务栏，而不是和平时一样弹出界面

**无窗应用：** 用于控制台类型的应用，对于这些没有交互界面的应用，勾选此项可以让其启动时完全不显示控制台，而作为系统服务启动



## 配置示例

### 带交互界面的程序

![](https://lambda.cyou/assets/img/processguard-9.PNG)



### Spring Boot项目

![](https://lambda.cyou/assets/img/processguard-10.PNG)
