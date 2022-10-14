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

sum=0
i=0
while [ "${i}" != "100" ]
do
    i=$(($i+1))
    sum=$((${sum}+${i}))
done

echo "the result of '1+2+3+...+100= ${sum}'"