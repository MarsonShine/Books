#!/bin/bash
echo -e "i will use 'touch' command to create 3 files."
read -p "please input your filename: " fileuser

# 为了避免使用者随意按 enter
filename=${fileuser:-"filename"}

date1=$(date --date='2 days ago' +%Y%m%d) # 前两天的日期
date2=$(date --date='1 days ago' +%Y%m%d) # 前1天的日期
date3=$(date +%Y%m%d)
file1=${filename}${date1}                  # 底下三行在设置文件名
file2=${filename}${date2}
file3=${filename}${date3}

# touch 创建文件
touch "${file1}"                      
touch "${file2}"
touch "${file3}"