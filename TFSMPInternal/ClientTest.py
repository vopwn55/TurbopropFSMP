import json
import time
import socket
import random
import threading

from NetConnectedV2 import socketutils

SignonBracket = json.dumps({
    "NCVersion":3,
    "NCClient":True,
    "Username":input("Username: "),
    "PlaneType":input("Plane type: ")
})



time.sleep(2)
with socket.socket() as ClientSocket:
    ClientSocket.connect(("play.vopwn55.xyz", 8880))
    print("Sent signin message")
    ClientSocket.sendall(SignonBracket.encode() + b"\x1C")
    print(SignonBracket.encode() + b"\x1C")
    response = socketutils.read_until_character(ClientSocket, b"\x1C")
    print("NetConnected server response: " + response.decode())
    def send():
        while True:
            time.sleep(1)
            print("Sent dummy normal message")
            DummyBracket = json.dumps({
                "PositionService":{
                    "Position":f"{random.randint(-1000,1000)},{random.randint(100,1000)},{random.randint(-1000,1000)}",
                    "PlaneType":random.choice(["C-400"]),
                    "Rotation":"3,3,3"
                }
            })
            ClientSocket.sendall(DummyBracket.encode() + b"\x1C")
    def read():
        while True:
            response = socketutils.read_until_character(ClientSocket, b"\x1C")
            print("NetConnected server response (after): " + response.decode())
            time.sleep(1)
    snd = threading.Thread(target=send)
    rcv = threading.Thread(target=read)
    snd.start()
    rcv.start()
    rcv.join()
        
        
