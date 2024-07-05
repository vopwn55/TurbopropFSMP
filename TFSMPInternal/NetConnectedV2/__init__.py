# NetConnected v2
# written by vopwn55
# work began on 18th of february 2024

version = 3

import traceback
import socketserver
import time
import threading
import json
import hashlib
from colorama import Fore, Style
from . import socketutils
from . import events

enableprint = False

def dprint(txt):
    if enableprint:
        print(txt)

def warn(message):
    print(f"{Fore.YELLOW}{message}{Style.RESET_ALL}")


messagediv = "\x1C"

LastDefaultID = 0
GLOBALFLAGS = {
}

class ServerParams():
    def __init__(self):
        self.Items = {}
    
    def __getitem__(self, key):
        return self.Items[key]
    
    def __setitem__(self, key, value):
        self.Items[key] = value

class NetConnectedServer(socketserver.StreamRequestHandler):
    """The NetConnected server handling connections and brackets."""
    OnFirstConnectionAttempted = events.Event() # Client connected
    """Event representing the client connecting to the server"""
    OnReceiveDataFromClient = events.Event() # Data is received after handshake
    """Event representing client sending a NetConnected Bracket to the server"""
    OnFirstConnectionComplete = events.ReturnableEvent() # Handshake success
    """Event representing client successfully handshaking. Make sure to implement this so the client knows the up-to-date server information and can proceed further."""
    OnDisconnected = events.Event() # Client disconnected
    """Event representing client disconnecting from the server."""
    OnDataRequest = events.ReturnableEvent()
    """Event requesting data from server to send to client"""

    def Handshake(self, bracketInitial: dict):
        if not "NCVersion" in bracketInitial:
            return False
        if bracketInitial["NCVersion"] != version:
            return False
        if not "NCClient" in bracketInitial:
            return False
        return True
    
    def StrToByte(self, strtoconvert: str):
        if type(strtoconvert) == str:
            return strtoconvert.encode()
        else:
            return strtoconvert
        
    def sendall(self, ts:bytes):
        if type(ts) == str:
            ts = ts.encode()
        self.request.sendall(ts + messagediv.encode())
        

    def loopsend(self):
        try:
            while True:
                sendBack = self.OnDataRequest.InvokeEvent(self.connection)
                if sendBack:
                    self.sendall(self.StrToByte(sendBack))
                else:
                    self.sendall(b"Acknowledged")
                time.sleep(1/20)
        except:
            pass

    def looprecv(self):
        while True:
            try:
                httpresp2 = socketutils.read_until_character(self.request)
                newbracket = httpresp2.split(b"\n\n")
                if len(newbracket) < 2:
                    newbracket = httpresp2.split(b"\r\n\r\n")[-1]
                else:
                    newbracket = newbracket[-1]
                newtablebracket = json.loads(newbracket)
                self.OnReceiveDataFromClient.InvokeEvent(self.connection, newtablebracket)
            except json.decoder.JSONDecodeError:
                continue
            except:
                break
        

    def handle(self):
        self.request.settimeout(10)
        try:
            self.OnFirstConnectionAttempted.InvokeEvent(self.client_address)
            httpresp = socketutils.read_until_character(self.request)
            firstbracket = httpresp.split(b"\n\n")
            if len(firstbracket) < 2: #only one
                firstbracket = httpresp.split(b"\r\n\r\n")[-1]
            else:
                firstbracket = firstbracket[-1]
            firsttablebracket = json.loads(firstbracket)
            result = self.Handshake(firsttablebracket)
            if not result:
                self.sendall(b"HTTP/1.1 426 Upgrade Required \nUpgrade: NetConnected\nContent-Length: 18\n\nConnection failed.")
            response = self.OnFirstConnectionComplete.InvokeEvent(self.client_address, firsttablebracket, self.request, firsttablebracket["NCVersion"])
            if type(response) == list and response[0] == False:
                self.sendall(self.StrToByte(response[1]))
                self.request.close()
                return
            elif type(response) == list and response[0] == True:
                self.sendall(self.StrToByte(response[1]))
            else:
                self.sendall(b"Accepted")
            try:
                looprecv = threading.Thread(target=self.looprecv)
                loopsend = threading.Thread(target=self.loopsend)
                looprecv.start()
                loopsend.start()
                loopsend.join()
            except:
                pass
            
        except BaseException as e:
            warn(e)
            #traceback.print_exc()
            try:
               self.sendall(b"HTTP/1.1 426 Upgrade Required \nUpgrade: NetConnected\nContent-Length: 18\n\nConnection failed.")
            except:
               pass
        finally:
            self.OnDisconnected.InvokeEvent(self.client_address)
            self.request.close()


class ThreadedTCPServer(socketserver.ThreadingMixIn, socketserver.TCPServer):
    def __init__(self, server_address, handler_class):
        super().__init__(server_address, handler_class)
        self.netconnectedserver = handler_class
        print(handler_class)

def CreateNewInternal(HOST, PORT, ID):
    """Internal function to create a server. DOES NOT RETURN A SERVER. Do not use."""
    server = ThreadedTCPServer((HOST, PORT), NetConnectedServer)
    with server:
        server_thread = threading.Thread(target=server.serve_forever)
        server_thread.daemon = True
        server_thread.start()
        print("Server loop running in thread:", server_thread.name)
        GLOBALFLAGS[ID] = server
        server_thread.join()

# Horrible code but whatever ig lol lmfao
def WaitForServer(ID) -> NetConnectedServer:
    """Waits for a server with the specified ID to exist, then returns it."""
    while not ID in GLOBALFLAGS:
        time.sleep(0.5)
    return GLOBALFLAGS[ID].netconnectedserver

def CreateNew(HOST, PORT, ID=None) -> NetConnectedServer:
    """Creates and starts a new NetConnected server and returns it."""
    global LastDefaultID
    LastDefaultID += 1
    if not ID:
        ID = LastDefaultID
    threading.Thread(target=CreateNewInternal, args=[HOST, PORT, ID]).start()
    ncserver = WaitForServer(ID)
    return ncserver