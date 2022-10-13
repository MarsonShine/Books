#!/bin/bash
# 通过$((表达式)) 来进行数值运算
echo -e "you should input 2 numbers, i will multiplying them! \n"
read -p "first number:" first
read -p "second number:" second
total=$((${first}*${second}))
declare -i alterTotal=${first}*${second} # 也可以用 declare 关键字定义变量并赋值
echo -e "\n the result of ${first} x ${second} is ==> ${total}==${alterTotal}"