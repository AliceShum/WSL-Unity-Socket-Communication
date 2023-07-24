import socket
import threading
import json
import struct
import time

localIP = '192.168.110.191' # Server IP address
port = 10086 # Server IP port
this_stype = 100  

def doSomething():
    try:
        # add what you want to do here
        print("========= SUCCESS ========== ")
        
        #发送到服务端
        sendResultToServer("success")
    except Exception as e:
        print("========= ERROR ========== " + str(e))
        sendResultToServer("error")

#4 int, 2 short, 2 short, 4 empty, content    
def getBytesPack(stype, ctype, array_data):   
    
    arr1 = stype.to_bytes(2, 'little')
    arr2 = ctype.to_bytes(2, 'little')
    arr0 = bytes(4)
    arr3 = arr1 + arr2 + arr0 + array_data
    length = len(arr3) + 4
    arr4 = length.to_bytes(4, 'little')
    arr5 = arr4 + arr3
    return arr5

def handleBytesPack(bytes_data):
    head_buffer = bytes_data[0:4]
    pkg_size = int.from_bytes(head_buffer, byteorder='little',signed=False)
    stype_buffer = bytes_data[4:6]
    stype = int.from_bytes(stype_buffer, byteorder='little',signed=False)
    ctype_buffer = bytes_data[6:8]
    ctype = int.from_bytes(ctype_buffer, byteorder='little',signed=False)
    #print("handleBytesPack:" + str(pkg_size) + "   " + str(stype) + "  " + str(ctype))
    if stype == this_stype:
        content_buffer = bytes_data[8:]
        content_str = str(content_buffer, 'utf-8')
        #json_str = json.loads(content_str)
        #command = json_str['command']

        content_str = ''.join(filter(str.isdigit, content_str))
        if content_str:
            print(int(content_str))
        else:
            print("invalid string")

        return int(content_str) 
        
#Send message to server
def sendResultToServer(result):
    stype = 3
    ctype = 1
    data = '{"result":"' + result + '"}'
    send_pack = getBytesPack(stype, ctype, data.encode('utf-8'))
    client.send(send_pack)

def openSocketConnect():
    global client
    client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    client.connect((localIP, port))
    
    #send message to server
    stype = 2
    ctype = 1
    data='{"platform":1,"ipAddr":"' + extractIp() + '","port":0}'
    send_pack = getBytesPack(stype, ctype, data.encode('utf-8'))
    client.send(send_pack) 
    
    #receive server message
    while True:
        rc = client.recv(256) 
        command_str = handleBytesPack(rc)
        print("Receive server msg: need train a picture! Command is: " + str(command_str))
        if command_str:
            doSomething()
        
    client.close()

#Gets Wsl IP address
def extractIp():
    st = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        st.connect(('8.8.8.8', 80))
        IP = st.getsockname()[0]
    except Exception:
        IP = '127.0.0.1'
    finally:
        st.close()
        
    print(IP)
    return IP

if __name__ == "__main__":  
    openSocketConnect()
    