from abc import ABC, abstractmethod

class Event():
    _callbacks = []

    def InvokeEvent(self, *params):
        for cb in self._callbacks:
            if callable(cb):
                cb(*params)

    def Connect(self, callback):
        self._callbacks.append(callback)
        return len(self._callbacks)

    def DisconnectById(self, callbackid):
        self._callbacks.pop(callbackid)

    def DisconnectByCallback(self, callback):
        self._callbacks.remove(callback)

        
class ReturnableEvent():
    _callback = None

    def Connect(self, callback):
        if callable(callback):
            self._callback = callback
    
    def InvokeEvent(self, *params):
        if not callable(self._callback):
            return None
        return self._callback(*params)
    
    def Disconnect(self):
        self._callback = None

class APIPlayer(ABC):
    def __init__(self, username: str, connection):
        self.Username = username
        self.Connection = connection
    
    @abstractmethod
    def GetPlayerData(self):
        pass
        
    @abstractmethod
    def IsConnected(self, precise=True):
        pass
        
    @abstractmethod
    def Kick(self):
        pass


class TFSMPAPIPlayer(APIPlayer):
    def GetPlayerData(self):
        if self.Username in self.PlayerData:
            return self.PlayerData[self.Username]
        else:
            return None
        
    def IsConnected(self, precise=True):
        if precise:
            try:
                self.Connection.recv(0)
                return True
            except:
                return False
        else:
            return self.Username in self.PlayerData
        
    def Kick(self):
        self.Connection.shutdown()


class TFSMPAPI:
    def __init__(self):
        #todo: make all of this work
        self.PlayerConnected = Event()
        self.PlayerDisconnected = Event()
        self.PlayerData = {}
        self.Players = {}

    def GetAPIPlayer(self, username:str):
        if username in self.Players:
            return self.Players[username][2]
        return None
