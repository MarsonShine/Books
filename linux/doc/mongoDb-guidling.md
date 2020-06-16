1、启动 MongoDb

```
"C:\Program Files\MongoDB\Server\4.2\bin\mongo.exe"  -- 你的安装路径
```

2、指定数据库

```
"C:\Program Files\MongoDB\Server\4.2\bin\mongo.exe" --dbpath="c:\data\db"
```

3、删除 MongoDb 服务

```
sc.exe delete MongoDB
```

4、常用数据库指令

```
show dbs  -- 显示所有数据库
db	-- 查看当前连接的数据库
use dbname 	-- 切换指定数据库
show collections	-- 显示该库下所有集合
db.collectionName.drop()	-- 删除表明（集合）
db.collectionName.remove({})	-- 删除集合下所有数据
```

5、mongodb 模糊查询

```
db.collectionName.find({propertyName:{$regex:"keywords"}})
db.collectionName.find({propertyName:/parttern/)
```

文档地址：https://docs.mongodb.com/manual/reference/operator/query/regex/#op._S_regex

6、Linux 以服务方式启动 mongodb

```shell
cd /lib/systemd/system
vi mongodb.service

// 以下是编写服务内容
[Unit]  
Description=mongodb  
After=network.target remote-fs.target nss-lookup.target  

[Service]  
Type=forking  
RuntimeDirectory=mongodb
RuntimeDirectoryMode=0751
PIDFile=/var/run/mongodb/mongod.pid
ExecStart=/usr/local/mongodb/bin/mongod --config /usr/local/mongodb/mongodb.conf  
ExecStop=/usr/local/mongodb/bin/mongod --shutdown --config /usr/local/mongodb/mongodb.conf  
PrivateTmp=false  

[Install]  
WantedBy=multi-user.target

// 以上是编写服务类内容 按 esc :wq 退出并保存

// 以下是启动服务
systemctl start mongodb.service	// 启动服务
systemctl stop mongodb.service // 停止服务
// 开机启动
systemctl enable mongodb.service
```

[](https://source.dot.net/#System.Text.Json/System/Text/Json/Serialization/JsonSerializer.Write.String.cs,61)