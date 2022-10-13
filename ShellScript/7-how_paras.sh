#!/bin/bash

# 参数格式如下
# /path/to/scriptname  opt1  opt2  opt3  opt4 
#        $0             $1    $2    $3    $4
# 要注意，第一个参数是脚本的名称，而真正的参数是从 $1 开始的
echo "The script name is        ==> ${0}"
echo "Total parameter number is ==> $#"
# 判断
[ "$#" -lt 2 ] && echo "The number of parameter is less than 2.  Stop here." && exit 0
echo "Your whole parameter is   ==> '$@'"
echo "The 1st parameter         ==> ${1}"
echo "The 2nd parameter         ==> ${2}"