#!/bin/bash

network="192.168.1"
# for sitenu in $(seq 1 100)
for sitenu in {1..100}
do
    # 取回环地址
    ping -c 1 -w 1 ${network}.${sitenu} &> /dev/null && result=0 || result=1
    if [ "${result}" == 0 ]; then
        echo "Server ${network}.${sitenu} is UP."
    else 
        echo "Server ${network}.${sitenu} is DOWN."
    fi
done