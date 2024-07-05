class Event():
    def __init__(self) -> None:
        self._callbacks = []

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
    def __init__(self) -> None:
        self._callback = None

    def Connect(self, callback):
        if callable(callback):
            self._callback = callback
    
    def InvokeEvent(self, *params):
        if not callable(self._callback):
            return None
        return self._callback(*params)
    
    def Disconnect(self):
        self._callback = None