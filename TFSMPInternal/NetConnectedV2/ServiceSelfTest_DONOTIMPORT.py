import services
import random
import time

# GetRandomIntService
GetRandomIntService = services.BaseService("GetRandomIntService")
def GetRandomIntService_GetServiceBlock(self):
    ServiceBlock = services.brackets.ServiceBlock()
    ServiceBlock.AddField("RandomNumber", random.randint(1,99999))
    return ServiceBlock

GetRandomIntService.GetServiceBlock = GetRandomIntService_GetServiceBlock

# GetLastPollService
GetLastPollService = services.BaseService("GetLastPollService")
GetLastPollService.LastPolled = time.time()
def GetLastPollService_GetServiceBlock(self):
    CurrentTime = time.time()
    ServiceBlock = services.brackets.ServiceBlock()
    ServiceBlock.AddField("LastPolled", self.LastPolled)
    ServiceBlock.AddField("Ping", CurrentTime - self.LastPolled)
    self.LastPolled = CurrentTime
    return ServiceBlock

# ServiceCollector
ServiceCollector = services.ServiceCollector()
ServiceCollector.RegisterService(GetRandomIntService)
ServiceCollector.RegisterService(GetLastPollService)

while True:
    time.sleep(1)
    print(ServiceCollector.PollAllServices())