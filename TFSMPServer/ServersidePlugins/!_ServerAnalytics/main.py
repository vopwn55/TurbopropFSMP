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

### Actual plugin! ###

sys.path.append(FilePath)
from colors import *
green("Example plugin running!")

def recvPlayer(apiplayer: tfsmp.APIPlayer):
    log(f"oh someone joined! their name is: {apiplayer.Username}")

PrimaryAPI.PlayerConnected.Connect(recvPlayer)

def leavePlayer(apiplayer: tfsmp.APIPlayer):
    log(f"oh someone left :( their name is: {apiplayer.Username}")

PrimaryAPI.PlayerDisconnected.Connect(leavePlayer)