# 宝塔运行

### 1. 下载release对应平台的包，解压到指定目录
### 2. 重命名`appsettings.Example.json`为`appsettings.json`，并修改配置文件，若没有此文件，可自行新建
> `appsettings.json`说明参见：[appsettings.json](appsettings.md)
### 3. 为二进制文件`Telegram.CoinConvertBot`增加可执行权限
### 4. `宝塔应用管理器`或`Supervisor管理器`添加应用
> 应用名称：CoinConvertBot  
> 应用环境：无 （`Supervisor管理器`无此项）  
> 执行目录：/xxx (你解压文件的目录)  
> 启动文件：/xxx/Telegram.CoinConvertBot  
> 如有其他选项保持默认
>
> -------------------------------------------

<b>电脑编译步骤：</u>

<b>1 首先购买win服务器：Microsoft Windows Server 2022 Base // t3.large // 选择秘钥对（没有就下载一个）</u>

<b>2 创建安全组  全勾选</u>

<b>3 启动实例并链接</u>

<b>4 操作-安全-获取win 密码并登录，用户名默认：</u>
```
Administrator
```

<b>5 下载安装包 浏览器  </u>

```
https://codeload.github.com/xiaobai2023123412412343/CoinConvertBot/zip/refs/heads/master
```

<b>6 安装.NET 6.0  浏览器   </u>
```
https://dotnet.microsoft.com/en-us/download/dotnet/6.0
```

<b>7 修改配置文件：重命名配置文件   appsettings.Example.json  改成</u>
```
appsettings.json
```
<b>改名后修改配置文件里面的数据！！！</u>


<b>8  安装 .net.sdk 浏览器 </u>   
```
https://dotnet.microsoft.com/download
```

<b>9 添加NuGet源： 终端  </u>
```
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

<b>10 打开文件目录（示例）终端：</u>
```
cd C:\Users\Administrator\Downloads\CoinConvertBot-master\CoinConvertBot-master\src\Telegram.CoinConvertBot 
```

<b>11 编译项目 终端：</u>
```
dotnet build
```

<b>12 发布程序 终端 ：</u>
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

<b>13  打开文件目录（示例）终端 ： </u>
```
cd C:\Users\Administrator\Downloads\CoinConvertBot-master\CoinConvertBot-master\src\Telegram.CoinConvertBot\publish
```

<b>14：运行程序并记录日志 终端 ： </u>

<b><del>14.1：日志一次性，重启机器人后日志清零 </del></u>
```
.\Telegram.CoinConvertBot.exe > .\日志.txt 2>&1
```
<b><del>14.2：日志永久保留，重启机器人也可保留之前的记录 </del></u>
```
.\Telegram.CoinConvertBot.exe >> .\日志.txt 2>&1
```
<b>14.3 在 .exe 目录下新建一个文件自动执行,注意名称： </u>
```
一键启动机器人.ps1
```
<b>文本内容可自行修改： </u>
```
cd C:\Users\Administrator\Downloads\CoinConvertBot-master\CoinConvertBot-master\src\Telegram.CoinConvertBot\publish
# 设置 PowerShell 解码程序输出为 UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
# 运行程序并写入文件（覆盖模式，重启机器人日志清零）
.\Telegram.CoinConvertBot.exe 2>&1 | Out-File -FilePath .\日志.txt -Encoding UTF8
```
<b>或者是日志永久保留： </u>
```
cd C:\Users\Administrator\Downloads\CoinConvertBot-master\CoinConvertBot-master\src\Telegram.CoinConvertBot\publish
# 设置 PowerShell 解码程序输出为 UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
# 运行程序并写入文件（追加模式，重启机器人旧的日志保留）
.\Telegram.CoinConvertBot.exe 2>&1 | Out-File -FilePath .\日志.txt -Encoding UTF8 -Append
```
右键 点击文件 “一键启动机器人.ps1” ，选择“以 PowerShell 运行”

---------------------------------------

<b>创建自动任务并后台运行： </u>
---------------------------------

<b>15：终端输入并打开计划任务：</u>
```
taskschd.msc
```
<b>16：右上角创建任务</u>

<b>17：名称/描述随便，更改用户，输入： </u>
```
system
```
<b>确保任务以系统权限运行，配置改成：Windows Server 2022</u>

<b>18：触发器改成：启动时  ，这样每次服务器启动时都会运行此任务。</u>

19：切换到“操作”选项卡，点击“新建”。

20： 在“操作”下拉菜单中选择“启动程序”。

在“程序或脚本”框中输入 
```
powershell.exe
```

21： 在“添加参数(可选)”框中输入 -File 
```
C:\Users\Administrator\Downloads\CoinConvertBot-master\CoinConvertBot-master\src\Telegram.CoinConvertBot\publish\一键启动机器人.ps1
```
确保路径正确指向您的 PowerShell 脚本。

在“起始于(可选)”框中输入脚本所在的目录：
```
C:\Users\Administrator\Downloads\CoinConvertBot-master\CoinConvertBot-master\src\Telegram.CoinConvertBot\publish\
```
<b>22：条件没用的全取消</u>

<b>23：设置里记得把自动停止任务关闭</u>

---------------------------------------

<b>执行自动计划任务时，可先修改日志模式是覆盖模式还是追加模式</u>
