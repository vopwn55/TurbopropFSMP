from . import socketutils
from . import brackets
import json
import time
from colorama import Fore, Back, Style

def get_formatted_time():
    formatted_time = time.strftime("[%H:%M:%S %d-%m-%y] ")
    return f"{Fore.LIGHTBLACK_EX}{formatted_time}{Style.RESET_ALL}"

def warn(txt):
    print(get_formatted_time() + Fore.YELLOW + str(txt) + Style.RESET_ALL)

def tip(txt):
    print(Fore.BLUE + "Tip: " + str(txt) + Style.RESET_ALL)

def error(txt):
    print(get_formatted_time() + Fore.RED + str(txt) + Style.RESET_ALL)

class BaseService():
    """Base service to perform an operation to be transferred through a bracket via NetConnected\n\n
    BaseService.UpdateData: Sets a new internal value and moves the one before that to BaseService.LastData\n\n
    BaseService.GetData: Get current internal value\n\n
    BaseService.GetDataDifference: Should represent what changed in the data. NC transfers what is returned by this function\n\n
    BaseService.ProcessData: Should intake new data from NC client transfer and process it accordingly. By default, it sets the internal value to newData"""

    def __init__(self, Name):
        if Name is None:
            self.Name = self.__class__.__name__
        else:
            self.Name = Name
        self.LastData = None
        self.CurrentData = None

    def UpdateData(self, newData):
        """Built-in feature to update data while preserving the previous data"""
        self.LastData = self.CurrentData
        self.CurrentData = newData

    def GetData(self):
        """Built-in feature to get current data"""
        return self.CurrentData
    
    def GetServiceBlock(self, address=None):
        """Called when NC is querying data to send. This should be overridden based on the service implementation"""
        serviceBlock = brackets.ServiceBlock(self.Name)
        serviceBlock.AddField("Data", self.CurrentData)
        return serviceBlock
    
    def GetDataDifference(self, *args, **kwargs):
        """DO NOT USE! This is only provided for backwards-compatibility with an older version of NC."""
        return self.GetServiceBlock(*args, **kwargs)
    
    def ProcessData(self, address, newData):
        """Called when NC received data for this service. This should be overridden based on the service implementation"""
        self.UpdateData(newData)

class ServiceCollector():
    """Used for grouping NetConnected services into a simple interface\n\n
    ServiceCollector.RegisterService: Registers a service for this ServiceCollector\n\n
    ServiceCollector.PollAllServices: Returns a raw bracket for all services in this ServiceCollector"""

    def __init__(self):
        self.Services = []

    def RegisterService(self, service:BaseService):
        """Registers a service for this ServiceCollector"""
        if service not in self.Services:
            self.Services.append(service)
    
    def PollAllServices(self, address):
        """Collects data from all services and returns a NetConnected bracket"""
        PolledData = {}
        for service in self.Services:
            ServiceName = getattr(service, 'Name', service.__class__.__name__)
            try:
                PolledData[ServiceName] = service.GetServiceBlock(service, address)
            except Exception as e:
                error(f"Failed to query {ServiceName} service: {e}")
        Bracket = brackets.Bracket()
        for _, ServiceBracket in PolledData.items():
            Bracket.AddBlock(ServiceBracket)
        return Bracket.GetAsNetConnectedJSON()
    
    def UpdateAllServices(self, address, bracket):
        """Process given bracket dictionary and tells services to process their service block in this bracket if any"""
        for service in self.Services:
            ServiceName = getattr(service, 'Name', service.__class__.__name__)
            try:
                if ServiceName in bracket:
                    service.ProcessData(service, address, bracket)
            except Exception as e:
                error(f"Failed to update {ServiceName} service: {e}")