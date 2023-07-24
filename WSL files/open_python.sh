#!/usr/bin/expect
# expect -f /usr/bin/open_python.sh
set timeout -1
spawn su  root
expect "Password:"
send "admin\r"
expect "#"
send "conda activate test\r"
send "cd /usr/bin\r"
send "python mytest.py \r"

interact

#expect eof
#exit