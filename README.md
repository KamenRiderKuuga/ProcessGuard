# ProcessGuard

[中文文档](README-zh.md)

About how it works:

[Subverting Vista UAC in Both 32 and 64 bit Architectures By Pero Matić](https://www.codeproject.com/Articles/35773/Subverting-Vista-UAC-in-Both-32-and-64-bit-Archite)

[Application Compatibility - Session 0 Isolation By Craig Marcho](https://techcommunity.microsoft.com/t5/ask-the-performance-team/application-compatibility-session-0-isolation/ba-p/372361)

With the ability to start a process from the Windows service, we can:

1. Start a program with an interactive interface from a Windows service and restart it after it has been closed
2. Set some programs to start automatically at boot
3. For console applications, including but not limited to `java`, `dotnet`, `node`, etc., they can be deployed on Windows systems as no window like Windows services

## ⚙Configuration Interface

> You can download the program directly from the [Release](https://github.com/KamenRiderKuuga/ProcessGuard/releases) page. The interface you see is just a configuration interface for configuring the processes to be guarded here. After starting the service, you can close the configuration interface

![](https://lambda.cyou/assets/img/processguard-5.PNG)

Note: The configuration can take effect only after the service is started



## 📕Configuration Items

**Process Name:** The name used to identify the current configuration item, only used for interface display

**Full Path:** Full path to executable

**Parameters:** The parameters be carried when starting the application, ignore this if you do not need any parameters

**Start Once:** Only started once during the service running

**Minimize:** For programs with an interactive interface, it can make it minimized to the taskbar when it starts, instead of popping up the interface as usual

**NoWindow:** For console applications, enabling this item can make it start like a windows service, without displaying the console at all



## Configuration Example

### An Interactive Program

![](https://lambda.cyou/assets/img/processguard-6.PNG)



### A Spring Boot Program

![](https://lambda.cyou/assets/img/processguard-7.PNG)
