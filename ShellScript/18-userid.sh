#!/bin/bash

# for var in con1 con2 con3 ...
# do
#     程序员
# done

users=$(cut -d ':' -f1 /etc/passwd)
for username in ${users}
do
    id ${username}
done