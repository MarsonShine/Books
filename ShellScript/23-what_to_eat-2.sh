#!/bin/bash

eat[1]="1吃汉堡包"
eat[2]="2肯德基炸鸡"
eat[3]="3彩虹日式便当"
eat[4]="4炒饭"
eat[5]="5猪脚饭"
eat[6]="6鸭腿饭"
eat[7]="7泡面"
eat[8]="8螺蛳粉"
eat[9]="9面包"
eatnum=9

eated=0
# 去掉重复项
while [ "${eated}" -lt 3 ]; do
    check=$(( ${RANDOM} * ${eatnum}/32767 + 1 ))
    mycheck=0
    if [ "${eated}" -ge 1 ]; then
        for i in $(seq 1 ${eated})
        do
            if [ ${eatedcon[$i]} == $check ]; then
                mycheck=1
            fi
        done
    fi
    if [ ${mycheck} == 0 ]; then
        echo "your may eat ${eat[${check}]}"
        eated=$(( ${eated} + 1 ))
        eatedcon[${eated}]=$[check]
    fi
done