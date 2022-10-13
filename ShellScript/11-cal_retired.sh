#!/bin/bash

# 将时间转换成秒，在进行时间运算

echo "This program will try to calculate :"
echo "How many days before your demobilization date..."
read -p "Please input your demobilization date (YYYYMMDD ex>20150716): " date2
# 校验输入的值是否合规
date_d=$(echo ${date2} |grep '[0-9]\{8\}')
if [ ${date_d} == "" ]; then
	echo "You input the wrong date format...."
	exit 1
fi