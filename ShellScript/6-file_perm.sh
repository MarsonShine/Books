#!/bin/bash

# 1. 讓使用者輸入檔名，並且判斷使用者是否真的有輸入字串？
echo -e "Please input a filename, I will check the filename's type and permission. \n\n"
read -p "Input a filename : " filename
test -z ${filename} && echo "You MUST input a filename." && exit 0
# 2. 判斷檔案是否存在？若不存在則顯示訊息並結束腳本
test ! -e ${filename} && echo "The filename '${filename}' DO NOT exist" && exit 0
# 3. 開始判斷檔案類型與屬性
test -f ${filename} && filetype="regulare file"
test -d ${filename} && filetype="directory"
test -r ${filename} && perm="readable"
test -w ${filename} && perm="${perm} writable"
test -x ${filename} && perm="${perm} executable"
# 4. 開始輸出資訊！
echo "The filename: ${filename} is a ${filetype}"
echo "And the permissions for you are : ${perm}"