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

<b>5 下载安装包  </u>

```
https://codeload.github.com/xiaobai2023123412412343/CoinConvertBot/zip/refs/heads/master
```

<b>6 安装.NET 6.0    </u>
```
https://dotnet.microsoft.com/en-us/download/dotnet/6.0
```

<b>7 修改配置文件：重命名配置文件   appsettings.Example.json  改成</u>
```
appsettings.json
```

<b>8  安装 .net.sdk  </u>   
```
https://dotnet.microsoft.com/download
```

<b>9 添加NuGet源：   </u>
```
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

<b>10 打开文件目录（示例）：</u>
```
cd C:\Users\Administrator\Downloads\CoinConvertBot-master\CoinConvertBot-master\src\Telegram.CoinConvertBot 
```

<b>11 编译项目：</u>
```
dotnet build
```

<b>12 发布程序：</u>
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

<b>13  打开文件目录（示例）： </u>
```
cd C:\Users\Administrator\Downloads\CoinConvertBot-master\CoinConvertBot-master\src\Telegram.CoinConvertBot\publish
```

<b>14：运行程序： </u>
```
.\Telegram.CoinConvertBot.exe
```


<b>创建自动任务并后台运行： </u>

<b>15：终端输入并打开计划任务：</u>
```
taskschd.msc
```
<b>16：右上角创建任务</u>

<b>17：名称/描述随便，更改用户，输入： </u>
```
system
```
<b>配置改成：Windows Server 2022</u>

<b>18：触发器改成：启动时</u>

<b>19：操作选择要启动的程序，起始于选择程序所在的目录，</u>

<b>如：C:\Users\Administrator\Downloads\CoinConvertBot-master\CoinConvertBot-master\src\Telegram.CoinConvertBot\publish\   </u>

<b>后面必须有个\ 别给删除了 </u>

<b>20：条件没用的全取消</u>

<b>21：设置里记得把自动停止任务关闭</u>

<b> 自动任务 好像没啥意义，手动开启还可以看看日志，看情况考虑是否开启！！！！</u>
