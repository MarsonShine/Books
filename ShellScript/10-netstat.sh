#!/bin/bash

echo "Now, I will detect your Linux server's services!"
echo -e "The www, ftp, ssh, and mail(smtp) will be detected! \n"

# 检查各个端口是否存在
testfile=/tmp/sshscript/netstat_checking.txt
netstat -tuln > ${testfile} # 转存结果输出到指定 testfile 文件
testing=$(grep ":80 " ${testfile}) # 查看是否存在 80 端口
if [ "${testing}" != "" ]; then
	echo "WWW is running in your system."
fi
testing=$(grep ":22 " ${testfile}) # 查看 22 端口
if [ $"{testing}" != "" ]; then
	echo "SSH is running in your system."
fi
testing=$(grep ":21 " ${testfile}) # 查看 21 端口
if [ "${testing}" != "" ]; then
	echo "FTP is running in your system."
fi
testing=$(grep ":25 " ${testfile})   # 查看 25 端口
if [ "${testing}" != "" ]; then
	echo "Mail is running in your system."
fi