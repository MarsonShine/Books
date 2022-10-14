#!/bin/bash

# loop 表达式
# while [ condition ]
# do
#     循环体
# done

# until [ condition ]
# do
#     循环体
# done

while [ "${yn}" != "yes" -a "${yn}" != "YES" ]
do
    read -p "Please input yes/YES to stop this program: " yn
done
echo "OK! you input the correct answer."