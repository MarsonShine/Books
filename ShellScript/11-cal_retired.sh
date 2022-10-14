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

# 计算日期
declare -i date_dem=$(date --date="${date2}" +%s) # 退伍日期秒数
declare -i date_now=$(date +%s)	# 现在日期的秒数
declare -i date_total_s=$(${date_dem}-${date_now})	# 剩余秒数统计
declare -i date_d=$((${date_total_s}/60/60/24))
if [ "${date_total_s=}" -lt "0" ]; then	# 判断是否已达退伍时间
	echo "You had been demobilization before: " $((-1*${data_d})) " ago"
else
	declare -i date_h=$(($((${date_total_s}-${date_d}*60*60*24))/60/60))
	echo "You will demobilize after ${data_d} days and ${data_h} hours."
fi