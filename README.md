PM.Net是一个类似PM2的系统进程管理器，为C#/netcore制作。

功能特点：

- 使用单一的一个系统服务，去管理多个进程（启动、停止、重启、删除、查看）；
- 保持进程始终有效。如果操作系统重启，或者进程Crash，会自动重启；
- 更新进程程序时，不用手动停止、启动服务。会自动热更新；
- 提供WebUI和RESTful API，可以远程、可视化的管理；
- 提供远程更新服务的API，实现远程部署，命令行部署；
- 提供纯静态的网站部署功能，替代Remote Desktop/SSH/FTP；

纯绿色部署PM.Net，放置到服务器任何位置，然后执行初始化脚本：
```
init.bat
# OR Unix
./init.sh
```

然后就可以使用系统的服务管理进行控制，服务的名字：`pmnet`

与pm2不同的是，PM.Net并没有提供命令行工具，而是使用WebUI进行交互式管理。可以访问这里：http://localhost:12345

然后使用初始用户`admin`，密码`admin`进行登录。

## Web API
Base URL: api/apps

- POST 创建app
  - type: 0 static, 1 general, 2 core
  - name: 必须唯一
  - status: 0 offline, 1 online, 2 starting, 3 stopping
  - description
  - directory: app目录
  - path: app的入口文件
  - urls: listener url 
- GET 获取app列表
- GET /{name} 获取app详情
- PUT /{name} 启动/停止app
  - status: 0 offline, 1 online
- DELETE /{name} 删除app（文件不会被删除）
- POST /{name}/pack 上传程序包，更新app
  - form data
  - file: 程序包
  - full: 是否删除服务器上app所有文件。
