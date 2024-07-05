# NetConnected v2
# written by vopwn55
# work began on 18th of february 2024

version = 1

import asyncio
import time
import threading
import json
from colorama import Fore, Style
from . import socketutils
from . import events
from websockets.server import serve

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

class NetConnectedServer():
    """The NetConnected server handling connections and brackets."""
    OnFirstConnectionAttempted = events.Event() # Client connected
    """Event representing the client connecting to the server"""
    OnReceiveDataFromClient = events.ReturnableEvent() # Data is received after handshake
    """Event representing client sending a NetConnected Bracket to the server"""
    OnFirstConnectionComplete = events.ReturnableEvent() # Handshake success
    """Event representing client successfully handshaking. Make sure to implement this so the client knows the up-to-date server information and can proceed further."""
    OnDisconnected = events.Event() # Client disconnected
    """Event representing client disconnecting from the server."""

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
        
    def sendall(self, ts):
        self.request.sendall(ts + messagediv.encode())

    async def handle(self, websocket):
        self.request = websocket
        #self.request.settimeout(10)
        try:
            self.OnFirstConnectionAttempted.InvokeEvent(self.client_address)
            firstbracket = socketutils.read_until_character(self.request)
            firsttablebracket = json.loads(firstbracket)
            result = self.Handshake(firsttablebracket)
            if not result:
                self.sendall(b"HTTP/1.1 426 Upgrade Required \nUpgrade: NetConnected\nContent-Length: 18\n\nConnection failed.")
            response = self.OnFirstConnectionComplete.InvokeEvent(self.client_address, firsttablebracket)
            if type(response) == list and response[0] == False:
                self.sendall(self.StrToByte(response[1]))
                self.request.close()
                return
            elif type(response) == list and response[0] == True:
                self.sendall(self.StrToByte(response[1]))
            else:
                self.sendall(b"Accepted")
            async for message in websocket:
                newbracket = socketutils.read_until_character(self.request)
                newtablebracket = json.loads(newbracket)
                sendBack = self.OnReceiveDataFromClient.InvokeEvent(self.client_address, newtablebracket)
                if sendBack:
                    self.sendall(self.StrToByte(sendBack))
                else:
                    self.sendall(b"Acknowledged")
            
        except BaseException as e:
            warn(e)
            try:
               self.sendall(b"HTTP/1.1 426 Upgrade Required \nUpgrade: NetConnected\nContent-Length: 18\n\nConnection failed.")
            except:
               pass
        finally:
            self.OnDisconnected.InvokeEvent(self.client_address)
            self.request.close()

#class ThreadedTCPServer(socketserver.ThreadingMixIn, socketserver.TCPServer):
#    def __init__(self, server_address, handler_class):
#        super().__init__(server_address, handler_class)
#        self.netconnectedserver = handler_class
#        print(handler_class)

#def CreateNewInternal(HOST, PORT, ID):
#    """Internal function to create a server. DOES NOT RETURN A SERVER. Do not use."""
#    server = ThreadedTCPServer((HOST, PORT), NetConnectedServer)
#    with server:
#        server_thread = threading.Thread(target=server.serve_forever)
#        server_thread.daemon = True
#        server_thread.start()
#        print("Server loop running in thread:", server_thread.name)
#        GLOBALFLAGS[ID] = server
#        server_thread.join()

# # Horrible code but whatever ig lol lmfao
# def WaitForServer(ID) -> NetConnectedServer:
#     """Waits for a server with the specified ID to exist, then returns it."""
#     while not ID in GLOBALFLAGS:
#         time.sleep(0.5)
#     return GLOBALFLAGS[ID].netconnectedserver

# def CreateNew(HOST, PORT, ID=None) -> NetConnectedServer:
#     """Creates and starts a new NetConnected server and returns it."""
#     global LastDefaultID
#     LastDefaultID += 1
#     if not ID:
#         ID = LastDefaultID
#     threading.Thread(target=CreateNewInternal, args=[HOST, PORT, ID]).start()
#     ncserver = WaitForServer(ID)
#     return ncserver

async def CreateNewInternal(HOST, PORT) -> NetConnectedServer:
    """Creates and starts a new NetConnected server and returns it."""
    netconnserver = NetConnectedServer()
    async with serve(netconnserver.handle, HOST, PORT):
        await asyncio.Future()

async def CreateNew(HOST, PORT) -> NetConnectedServer:
    """Creates and starts a new NetConnected server and returns it."""
    asyncio.run(CreateNewInternal(HOST,PORT))
    