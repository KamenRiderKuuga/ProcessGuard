# ProcessGuard

> 通过Windows Service保护Windows系统中的进程，使其在被关闭后能够重新启动

💡关于实现的具体依据请查看[通过Windows系统服务守护进程的运行](https://lambda.cyou/posts/Tips-5/)

📕本程序在Windows环境运行，通过Windows系统服务对进程进行守护，提供了操作界面，实现对进程守护服务的配置，具体的功能在界面上具有按钮指示，包含服务的安装，启动，停止，卸载。以及需要守护的进程的配置操作：

![](https://lambda.cyou/assets/img/processguard-1.PNG)

