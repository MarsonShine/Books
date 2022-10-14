#!/bin/bash

function toUpper() {
    echo ${1} | tr 'a-z' 'A-Z'
}

function printit() {
    echo -n "Your choice is " # -n 不换行处理
    toUpper ${1}
}



echo "This program will print your choice!"
case ${1} in 
    "one")
        printit ${1}; 
        ;;
    "two")
        printit ${1};
        ;;
    "three")
        printit ${1};
        ;;
    *)
        echo "Useage ${0} {one|two|three}"
        ;;
esac