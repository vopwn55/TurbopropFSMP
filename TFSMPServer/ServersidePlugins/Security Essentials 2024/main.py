# by vopwn55, started on 13/04/2024
# check config for more info

### Import PrimaryAPI and FilePath ###
import tfsmp
import sys
if not "PrimaryAPI" in globals() or not "FilePath" in globals():
    print("Invalid execution context")
    sys.exit()
PrimaryAPI:tfsmp.TFSMPAPI = globals()["PrimaryAPI"]
FilePath:str = globals()["FilePath"]

lastDataCheck = 0
# anti dos
submittedRequests = {}

# anti lag
lastPlanes = {}
planeSwitchCounts = {}
lastDataRecvTimestamp = {}
lastPosition = {}

### Actual plugin! ###

sys.path.append(FilePath)
import os
from colors import *
warn("This server is protected by Security Essentials 2024!")

def DataReceived(player:tfsmp.APIPlayer, data:dict):
    global lastDataCheck
    # anti dos
    if not player.Username in lastDataRecvTimestamp:
        lastDataRecvTimestamp[player.Username] = time.time()
    if not player.Username in lastPosition:
        lastPosition[player.Username] = data[0]
    if data[0] != lastPosition[player.Username]:
        lastDataRecvTimestamp[player.Username] = time.time()
        lastPosition[player.Username] = data[0]
    if not player.Username in submittedRequests:
        submittedRequests[player.Username] = 1
        return
    submittedRequests[player.Username] += 1
    #print(f"\r{submittedRequests[player.Username]}")
    if time.time() - lastDataCheck > 1:
        lastDataCheck = time.time()
        for playerR in submittedRequests:
            submittedRequests[playerR] = 0
            planeSwitchCounts[playerR] = 0
    if submittedRequests[player.Username] > 35:
        player.Kick()
        error(f"[Security Alert] {player.Username} was kicked for: Attempted DoS")
    if time.time() - lastDataRecvTimestamp[player.Username] > 60:
        player.Kick("You have been AFK for too long!")
        error(f"[Security Alert] {player.Username} was kicked for: AFK")
    # anti lag
    if not player.Username in lastPlanes:
        lastPlanes[player.Username] = "C-400"
    if not player.Username in planeSwitchCounts:
        planeSwitchCounts[player.Username] = 0
    if lastPlanes[player.Username] != data[1]:
        lastPlanes[player.Username] = data[1]
        planeSwitchCounts[player.Username] += 1
    if planeSwitchCounts[player.Username] > 4:
        player.Kick()
        error(f"[Security Alert] {player.Username} was kicked for: Attempted Plane Lag")

PrimaryAPI.DataReceived.Connect(DataReceived)