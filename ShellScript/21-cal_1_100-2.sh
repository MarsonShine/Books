#!/bin/bash

# for (( 初始值; 条件; 单步 ))
# do
# 	循环体
# done

read -p "Please input a number, I will count for 1+2+...+your_input: " nu

s=0
for ((i=1; i<=${nu}; i++))
do
    s=$((${s}+${i}))
done
echo "The result of '1+2+3+...+${nu}' is ==> ${s}"