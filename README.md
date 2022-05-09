# ProcessGuard

> 从系统服务中启动并保护指定的进程，使其在被关闭后重新启动，用于防止程序误关闭，或**用于想让任意应用程序像Windows系统服务一样保持运行的场景**

💡关于实现的具体依据请查看[通过Windows系统服务守护进程的运行](https://lambda.cyou/posts/Tips-5/)



## ⚙配置界面

> 从[Release](https://github.com/KamenRiderKuuga/ProcessGuard/releases)页面可以直接下载程序，启动程序后看到的界面只是一个配置界面，可以在这里配置要守护的进程，启动服务之后可以随时开启或关闭此配置界面

![](https://lambda.cyou/assets/img/processguard-2.PNG)

注：只有在界面点击启动服务，守护服务正常运行后，配置才能生效



### 配置说明

进程名称：用于标识当前配置项的名称，仅用于界面显示

完整路径：可执行文件的完整路径

启动参数：也就是平时启动应用时携带的参数，如不需要携带参数可忽略此项

仅开机自启：在守护服务运行期间只启动一次，用于只需要配置开机启动的场景

启动时最小化：对于有交互界面的程序，配置此项可以让其启动时只在任务栏出现，而不是和平时一样弹出界面

无窗应用：用于控制台类型的应用，对于这些没有交互界面的应用，勾选此项可以让其启动时完全不显示控制台，而作为系统服务启动



### 配置示例

#### 带交互界面的程序

![](https://lambda.cyou/assets/img/processguard-3.PNG)



#### Spring Boot项目

![](https://lambda.cyou/assets/img/processguard-4.PNG)
