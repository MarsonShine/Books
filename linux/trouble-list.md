# Linux,CentOS 出现的问题记录

## Supervisor

重载配置节点

1. 在 supervisor 配置文件目录，一般在 ` /etc/supervisor/conf.d/` 目录下，增加配置文件 dotnet.conf

   ```bash
   [program:dotnet]
   command=dotnet netcore.dll --urls "http://*:7799" ; 
   directory=//usr/dotnet/app/ ;
   autorestart=true ;  
   stderr_logfile=/var/log/app.err.log ; 
   stdout_logfile=/var/log/app.out.log ; 
   environment=ASPNETCORE_ENVIRONMENT=Production ; 
   user=root ;  
   stopsignal=INT
   ```

2. 执行 supervisord 重载配置文件

   ```
   supervisorctl -c /etc/supervisor/supervisord.conf reload
   ```

   要注意，在执行命令时很有可能会出现以下错误：`error: <class 'socket.error'>, [Errno 2] No such file or directory: file: <string> line: 1`，我这边的情况经常是要杀掉 `supervisord` 运行的进程，这个时候就要执行以下命令

   1. 查找出 supervisord 正在运行的进程： `pgrep -fl supervisord`
   2. 杀掉对应的 pid 进程：`kill -9 PID_Number`
   3. 启动 supervisord：`supervisord -c /etc/supervisor/supervisord.conf`
   4. 再次执行 `supervisorctl -c /etc/supervisor/supervisord.conf reload` 即可

*注意，在编写 supervisor 配置文件时，文本格式为 ASCII 格式。*

参考资料：https://github.com/Supervisor/supervisor/issues/121