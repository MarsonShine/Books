#!/bin/bash

case ${1} in 
	"hello")
		echo "Hello, how are you ?"
		;;
	"")
		echo "You MUST input parameters, ex> {${0} someword}"
		;;
	*)	# * 表示任意字符
		echo "Useage ${0} {hello}"
		;;
esac