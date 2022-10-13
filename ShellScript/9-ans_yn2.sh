#!bin/bash

# 条件判断
# if [ 条件判断 ]; then      if [ 条件判断 ]; then    
#    表达式                      表达式            
# else                      elif [ 条件判断 ]; then
#    表达式                      表达式                       
# fi                        else
#                               表达式
#                           fi
# 当有多个条件时，我们可以用中括号连接
read -p "Please input (Y/N): " yn

if [ "${yn}" == "Y" ] || [ "${yn}" == "y" ]; then
	echo "OK, continue"
	exit 0
fi
if [ "${yn}" == "N" ] || [ "${yn}" == "n" ]; then
	echo "On, interrupt!"
	exit 0
fi
echo "I don't know what your choice is" && exit 0
