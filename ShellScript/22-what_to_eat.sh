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

check=$(( ${RANDOM} * ${eatnum}/32676 + 1 ))
echo "your may est ${eat[${check}]}"