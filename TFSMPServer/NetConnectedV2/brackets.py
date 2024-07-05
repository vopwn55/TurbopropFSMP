import json

class ServiceBlock():
    """ServiceBlock class represents a block of data for a specific service.
    
    This class provides functionality to manage fields within the service block.
    
    Attributes:
        Fields (dict): A dictionary containing field names and their values.
        BlockName (str): Name of the service block.
    
    Methods:
        AddField: Adds a field value to the service block.
        GetField: Retrieves the value of a specified field.
        RemoveField: Removes a field from the service block.
        GetAsDict: Returns the service block data as a dictionary.
        GetAsJSON: Returns the service block data as a JSON-formatted string.
        __init__: Initializes a new ServiceBlock object.
        __str__: Returns the name of the service block.
        __len__: Returns the number of fields in the service block.
        __getitem__: Retrieves the value of a specified field.
        __setitem__: Sets the value of a field.
        __delitem__: Deletes a field.
        __iter__: Iterates over the values of the fields.
        __contains__: Checks if a field exists in the service block.
    """
    Fields = {}
    BlockName = ""

    def __init__(self, name) -> None:
        self.BlockName = name
        self.Fields = {}

    def __str__(self) -> str:
        return self.BlockName
    
    def __len__(self) -> int:
        return len(self.Fields)
    
    def __getitem__(self, key):
        return self.Fields[key]
    
    def __setitem__(self, key, value):
        self.Fields[key] = value

    def __delitem__(self, key):
        del self.Fields[key]

    def __iter__(self):
        return iter(self.Fields.values())
    
    def __contains__(self, item):
        return item in self.Fields
    
    def AddField(self, key, value):
        """Adds a field value to this ServiceBlock. Functionally identical to `ServiceBlock[key] = value`."""
        self.Fields[key] = value

    def GetField(self, key):
        """Retrieves the value of a specified field. Functionally identical to `ServiceBlock[key]`."""
        return self.Fields[key]

    def RemoveField(self, key):
        """Removes a field from the service block. Functionally identical to `del ServiceBlock[key]`."""
        del self.Fields[key]

    def GetAsDict(self):
        """Returns the service block data as a dictionary."""
        return self.Fields

    def GetAsJSON(self):
        """Returns the service block data as a JSON-formatted string."""
        return json.dumps(self.Fields)
    
class Bracket():
    """Represents a set of service blocks to be supplied to the NetConnected server or client.
    
    This class manages a collection of service blocks within a bracket.
    
    Attributes:
        BracketSide (str): Indicates whether the bracket belongs to a server or a client.
        ServiceBlocks (dict): A dictionary containing service block objects.
    
    Methods:
        __str__: Returns a JSON string representation of the bracket.
        __len__: Returns the number of service blocks in the bracket.
        __getitem__: Retrieves a service block by its key.
        __setitem__: Sets a service block in the bracket.
        __delitem__: Deletes a service block from the bracket.
        __iter__: Iterates over the service blocks in the bracket.
        __contains__: Checks if a service block exists in the bracket.
        AddBlock: Adds a service block to the bracket.
        GetBlock: Retrieves a service block by its key.
        RemoveBlock: Removes a service block from the bracket.
        GetAsDict: Returns the bracket data as a dictionary.
        GetAsNetConnectedJSON: Returns the bracket data as a JSON-formatted string compatible with NetConnected.
    """
    BracketSide = "Server"
    ServiceBlocks = {}

    def __init__(self):
        self.ServiceBlocks = {}

    def __str__(self) -> str:
        """DO NOT USE!"""
        return self.GetAsNetConnectedJSON()
    
    def __len__(self) -> int:
        return len(self.ServiceBlocks)
    
    def __getitem__(self, key):
        return self.ServiceBlocks[key]
    
    def __setitem__(self, key, value):
        self.ServiceBlocks[key] = value

    def __delitem__(self, key):
        del self.ServiceBlocks[key]

    def __iter__(self):
        return iter(self.ServiceBlocks.values())
    
    def __contains__(self, item):
        return item in self.ServiceBlocks
    
    def AddBlock(self, block:ServiceBlock):
        """Adds a service block to the bracket."""
        self.ServiceBlocks[block.BlockName] = block

    def GetBlock(self, key):
        """Retrieves a service block by its key."""
        return self.ServiceBlocks[key]

    def RemoveBlock(self, key):
        """Removes a service block from the bracket."""
        del self.ServiceBlocks[key]

    def GetAsDict(self):
        """Returns the bracket data as a dictionary."""
        return self.ServiceBlocks

    def GetAsNetConnectedJSON(self):
        """Returns the bracket data as a JSON-formatted string compatible with NetConnected."""
        Structure = {}
        for ServiceName, ServiceBlock in self.ServiceBlocks.items():
            Structure[ServiceName] = ServiceBlock.GetAsDict()
        return json.dumps(Structure)
