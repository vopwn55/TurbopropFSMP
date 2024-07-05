# TFSMP Server by vopwn55
# Work began: 19/02/2024

import NetConnectedV2
import json
import os
import time
import sys
import pathlib
import re
import secrets
import threading
import socket
import chatfilter
from NetConnectedV2 import services, brackets, events
from NetConnectedV2.colors import *
from colorama import Fore, Back, Style

script_directory = pathlib.Path(__file__).parent.resolve()

settings = None
version = "0.1"

Players = {} #their ip is key, value is [username, typeofplane]
PlayerPositions = {} #username is key, position is value
PlayerLastRecvTime = {} # when we last received data from client (epoch timestamp)
PlayerConnections = {}

def getsetting(settingName, critical=False):
    if not settingName in settings:
        if critical:
            warn("Setting not found: " + settingName)
        return
    return settings[settingName]

class APIPlayer:
    def __init__(self, username, connection):
        self.Username = username
        self.Connection = connection
    
    def GetPlayerData(self):
        if self.Username in PlayerPositions:
            return PlayerPositions[self.Username]
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
            return self.Username in PlayerPositions
        
    def Kick(self, message="You have been kicked. Restart the app to rejoin."):
        self.Connection.sendall(b'{"Message":"Connection validated", "!!VoscriptPluginData":["PopupWindow(Disconnected from server: ' + message.encode() + b',Close)"]}' + "\x1C".encode())
        self.Connection.shutdown(socket.SHUT_WR)


class TFSMPAPI:
    def __init__(self):
        #todo: make all of this work
        self.PlayerConnected = events.Event()
        self.PlayerDisconnected = events.Event()
        self.DataReceived = events.Event()
        self.PlayerData = PlayerPositions
        self.Players = Players

    def GetAPIPlayer(self, username:str):
        if username in self.Players:
            return self.Players[username][2]
        return None
    
    def Chat(self, content:str):
        SendChatMessage("Server", content)
    
PrimaryAPI = TFSMPAPI()

class PluginManager:
    def __init__(self):
        self.plugins = {}

    def LoadAllPlugins(self):
        totalPlugins = 0
        successPlugins = 0
        pluginsFolder = os.path.join(script_directory, "ServersidePlugins")
        for pluginFolder in os.listdir(pluginsFolder):
            if pluginFolder.startswith("!_"):
                continue
            debug(f"Loading plugin {pluginFolder}...")
            totalPlugins += 1
            plugin_path = os.path.join(pluginsFolder, pluginFolder)
            config = os.path.join(plugin_path, "PluginConfig.json")
            if not os.path.exists(config):
                error(f"Failed to load plugin {pluginFolder}: No PluginConfig.json. (Error: 1)")
                continue
            try:
                conffile = json.load(open(config, "r"))
            except:
                error(f"Failed to load plugin {pluginFolder}: PluginConfig.json is corrupted. (Error: 2)")
                continue
            if not "MinimumTFSMPVersion" in conffile:
                error(f"Failed to load plugin {pluginFolder}: PluginConfig.json does not contain a required field 'MinimumTFSMPVersion'. (Error: 3)")
                continue
            minimumVersion = float(conffile["MinimumTFSMPVersion"])
            currentVersion = float(version)
            if currentVersion < minimumVersion:
                error(f"Failed to load plugin {pluginFolder}: This plugin is meant for a higher version of the TFSMP server ({conffile['MinimumTFSMPVersion']}). Please upgrade the TFSMP server or ask the plugin developer for an earlier version of the plugin. (Error: 4)")
                continue
            mainscript = os.path.join(plugin_path, "main.py")
            if not os.path.exists(mainscript):
                error(f"Failed to load plugin {pluginFolder}: No main.py. (Error: 5)")
                continue
            pluginfile = open(mainscript, "r")
            plugincontent = pluginfile.read()
            pluginfile.close()
            globals_dict = {"PrimaryAPI": PrimaryAPI, "FilePath":plugin_path}
            pluginThread = threading.Thread(target=exec, args=(plugincontent, globals_dict))
            # pluginThread.daemon = True
            pluginThread.start()
            successPlugins += 1
        green(f"{successPlugins}/{totalPlugins} plugins loaded successfully.")


# per-request mode

PerRequestAuthenticatedUsers = {}



configpath = os.path.join(script_directory, "config.json")

bold(f"TFS Multiplayer Server v{version}\n")
debugbright("Starting...")
debug("* Loading config file")
if os.path.exists(configpath):
    try:
        settings = json.load(open(configpath))
    except:
        error("Failed to load config file")
        sys.exit(1)
else:
    error("No config file found! Please make a config.json file with settings.")
    sys.exit(1)

hostaddr = getsetting("hostAddress")
hostport = getsetting("hostPort")
if not hostaddr or not hostport:
    error("No host address/port information")
    sys.exit(1)

debug("* Initializing service collector")


MainServiceCollector = services.ServiceCollector()

def getPlayersFromIP(address):
    usernames = []
    for connectedPlayers in Players:
        if type(Players[connectedPlayers]) == list and len(Players[connectedPlayers]) > 0:
            if Players[connectedPlayers][0] == address:
                username = connectedPlayers
                usernames.append(username)
    return usernames

debugbright("Initializing services:")
debug("* PlayerService")
PlayerService = services.BaseService("PlayerService")
def PS_GetServiceBlock(*args):
    servblock = brackets.ServiceBlock("PlayerService")
    servblock.AddField("Players", list(Players.keys()))
    return servblock

PlayerService.GetServiceBlock = PS_GetServiceBlock

debug("* PositionService")
PositionService = services.BaseService("PositionService")
def PosS_GetServiceBlock(*args):
    servblock = brackets.ServiceBlock("PositionService")
    servblock.AddField("Positions", PlayerPositions)
    servblock.AddField("TimestampFormatted", time.strftime("%H:%M:%S"))
    servblock.AddField("TimestampEpoch", time.mktime(time.localtime()))
    servblock.AddField("CurrentServerTime", time.perf_counter())
    if len(PlayerPositions) < 2:
        time.sleep(0.5)
    return servblock
PositionService.GetServiceBlock = PosS_GetServiceBlock

def ValidateVector3(v3string):
    pattern = r'^-?\d+(\.\d+)?,-?\d+(\.\d+)?,-?\d+(\.\d+)?$'

    if re.match(pattern, v3string):
        return True
    else:
        return False


PlaneTypes = ["C-400", "HC-400", "MC-400", "RL-42", "RL-72", "E-42", "XV-40", "PV-40", "InPerson", "4x4", "APC", "FuelTruck", "8x8", "Flatbed", "None"]

def ValidatePlaneType(PlaneType):
    return PlaneType in PlaneTypes

def ValidateState(stateDict:dict):
    defaultState = {
        "Eng1":True,
        "Eng2":True,
        "Eng3":True,
        "Eng4":True,
        "GearDown":True,
        "SigL":True,
        "MainL":False,
        "VTOLAngle":0,
        "PV40Color":"0,0,0",
        "LiveryId":-1,
    }
    for state in stateDict:
        if state in defaultState and type(stateDict[state]) == type(defaultState[state]):
            if type(stateDict[state]) == str and len(stateDict[state]) > 100:
                continue
            defaultState[state] = stateDict[state]
    return defaultState

def PosS_ProcessData(_, address, newdata: brackets.Bracket, perrequest:bool=False):
    players = getPlayersFromIP(address)
    if len(players) == 0:
        error("PositionService: Trying to process data of user that isn't connected")
        return
    if not "PositionService" in newdata:
        error("PositionService: Client did not supply PositionService data")
        return
    for playerUsername in players:
        ownBlock = newdata["PositionService"]
        if not "Position" in ownBlock:
            warn("PositionService: Client did not supply Position")
            return
        if not "PlaneType" in ownBlock:
            warn("PositionService: Client did not supply PlaneType")
            return
        if not "Rotation" in ownBlock:
            warn("PositionService: Client did not supply Rotation")
            return
        if not ValidateVector3(ownBlock["Position"]):
            warn("PositionService: Malformed Vector3 Position")
            return
        if not ValidatePlaneType(ownBlock["PlaneType"]):
            warn("PositionService: Wrong plane type")
            return
        if "State" in ownBlock:
            state = ValidateState(ownBlock["State"])
        else:
            state = ValidateState({})
        if not ValidateVector3(ownBlock["Rotation"]):
            #warn(f"PositionService: Malformed Vector3 Rotation {ownBlock['Rotation']}")
            if playerUsername in PlayerPositions:
                ownBlock["Rotation"]=PlayerPositions[playerUsername][2]
            else:
                ownBlock["Rotation"]="0,0,0"
        # time perf counter is current data receive
        OldRcv = time.perf_counter()
        if playerUsername in PlayerLastRecvTime:
            OldRcv = PlayerLastRecvTime[playerUsername]
            ping = time.perf_counter() - PlayerLastRecvTime[playerUsername]
        else:
            ping = 0
        PlayerLastRecvTime[playerUsername] = time.perf_counter()
        if playerUsername in PlayerPositions and len(PlayerPositions[playerUsername]) > 2:
            oldPosition = PlayerPositions[playerUsername][0]
            oldRotation = PlayerPositions[playerUsername][2]
        else:
            oldPosition = "0,0,0"
            oldRotation = "0,0,0"
        PlayerPositions[playerUsername] = [ownBlock["Position"], ownBlock["PlaneType"], ownBlock["Rotation"], state, oldPosition, oldRotation, OldRcv, time.perf_counter()]
        PrimaryAPI.DataReceived.InvokeEvent(PrimaryAPI.GetAPIPlayer(playerUsername), PlayerPositions[playerUsername])

PositionService.ProcessData = PosS_ProcessData
debug("* ChatService")

ChatService = services.BaseService("ChatService")
ChatMessages = []

def ClearupChat():
    if len(ChatMessages) >= 100:
        del ChatMessages[0]

LastMsgTimestamps = {}
LastMsgContents = {}

def ValidateChatMessage(author:str, message:str):
    if not author in LastMsgTimestamps:
        LastMsgTimestamps[author] = 0
    if not author in LastMsgContents:
        LastMsgContents[author] = ""
    if time.time() - LastMsgTimestamps[author] < 2:
        return False, "You are sending messages too fast!"
    if message.strip().lower() == LastMsgContents[author].strip().lower():
        return False, "Do not repeat the same message!"
    if chatfilter.filterstring(message)[0] != True:
        return False, "Your message was filtered. Please try again."
    LastMsgTimestamps[author] = time.time()
    LastMsgContents[author] = message
    return True, ""

def SendPrivateChatMessage(author, message, target):
    ClearupChat()
    ChatMessages.append({
        "sender":author,
        "isPrivate":True,
        "target":target,
        "message":message
    })

def SendChatMessage(author, messageraw):
    if len(messageraw) > 100:
        messageraw = messageraw[:100]
    message2 = messageraw.strip(" \n")
    message = re.sub(r'[^\x20-\x7E]', '', message2)
    ClearupChat()
    result, msg = ValidateChatMessage(author, message)
    if result:
        ChatMessages.append({
            "sender":author,
            "isPrivate":False,
            "target":"",
            "message":message
        })
    else:
        SendPrivateChatMessage("Server", msg, author)

def GetChatStringForPlayer(player:str, count=40):
    chatstr = ""
    crtCount = 0
    for chat in range(len(ChatMessages) - 1, -1, -1):
        if ChatMessages[chat]["isPrivate"] and ChatMessages[chat]["target"] == player:
            chatstr = f'[{ChatMessages[chat]["sender"]}] {ChatMessages[chat]["message"]}\n' + chatstr
            crtCount += 1
        if not ChatMessages[chat]["isPrivate"]:
            chatstr = f'[{ChatMessages[chat]["sender"]}] {ChatMessages[chat]["message"]}\n' + chatstr
            crtCount += 1
        if crtCount > count:
            return chatstr
    return chatstr
        
def ChatService_GetServiceBlock(self, address):
    players = getPlayersFromIP(address)
    for playerUsername in players:
        servblock = brackets.ServiceBlock("ChatService")
        servblock.AddField("Chat", GetChatStringForPlayer(playerUsername))
        return servblock
        
ChatService.GetServiceBlock = ChatService_GetServiceBlock

def ChatService_ProcessData(_, address, newdata: brackets.Bracket):
    players = getPlayersFromIP(address)
    if len(players) == 0:
        error("ChatService: Trying to process data of user that isn't connected")
        return
    if not "ChatService" in newdata:
        error("ChatService: Client did not supply ChatService data")
        return
    for playerUsername in players:
        ownBlock = newdata["ChatService"]
        if not "Pending" in ownBlock:
            error("ChatService: Client did not supply chat")
            return
        if ownBlock["Pending"] != "":
            SendChatMessage(playerUsername, ownBlock["Pending"])

ChatService.ProcessData = ChatService_ProcessData

debugbright("Initialization complete.")
debugbright("Registering services:")
debug("* PlayerService")
MainServiceCollector.RegisterService(PlayerService)
debug("* PositionService")
MainServiceCollector.RegisterService(PositionService)
debug("* ChatService")
MainServiceCollector.RegisterService(ChatService)
debugbright("Registration complete.")
debugbright("Starting NetConnected server")
debug("* Creating server")
NetServer = NetConnectedV2.CreateNew(hostaddr, hostport)
debug("* Setting up login connector for OnFirstConnectionComplete")
def validate_username(username):
    pattern = r'^[a-zA-Z0-9]{3,20}$'
    
    if re.match(pattern, username):
        return True
    else:
        return False
    

def registerPlayer(address, firstbracket, connection, ncversion):
    try:
        log(f"Incoming connection from {address[0]}")
        if not "Username" in firstbracket:
            warn("registerPlayer: No username")
            return [False, "Username not supplied"]
        if not validate_username(firstbracket["Username"]):
            warn("registerPlayer: Invalid username")
            return [False, '{"Message":"Connection validated", "!!VoscriptPluginData":["PopupWindow(Disconnected from server: Usernames can only contain Latin letters and numbers, and should consist of 3-20 characters.,Close)"]}']
        if not "PlaneType" in firstbracket or not ValidatePlaneType(firstbracket["PlaneType"]):
            warn("registerPlayer: No plane type")
            return [False, "Plane type not supplied"]
        if firstbracket["Username"] in Players:
            warn("registerPlayer: This player is already online")
            return [False, '{"Message":"Connection validated", "!!VoscriptPluginData":["PopupWindow(Disconnected from server: This username is already used. Please restart the app and choose a different username.,Close)"]}']
        apiplayer = APIPlayer(firstbracket["Username"], connection)
        Players[firstbracket["Username"]] = [address, connection, apiplayer]
        PlayerPositions[firstbracket["Username"]] = ["0,2000,0", firstbracket["PlaneType"], "0,0,0", {}]
        try:
            PrimaryAPI.PlayerConnected.InvokeEvent(apiplayer)
        except BaseException as e:
            error("Plugin-handled connect event caused an error to occur: " + e)
        log(f"Connection from {address[0]} accepted as {firstbracket['Username']}")
        if ncversion == 2:
            return [True, '{"Message":"Connection validated", "!!VoscriptPluginData":["PopupWindow(ATTENTION! You are using TFSMP version 1.0 which is outdated. Please upgrade to TFSMP 1.1 ASAP for improved performance and new features. Visit https://vopwn55.xyz/tfsmp,Understood)"]}']
        return [True, '{"Message":"Connection validated", "!!VoscriptPluginData":["PopupWindow(Welcome to the official TFSMP server! Please remember to be civilized and not annoy others. Have fun!,Close)"]}']
    except BaseException as e:
        error(f"Server unable to verify connection from {address[0]} for reason: {e}")
        return [False, "Server unable to verify this connection"]
    
NetServer.OnFirstConnectionComplete.Connect(registerPlayer)

debug("* Setting up disconnect connector for OnDisconnected")
def disconnectPlayer(address):
    try:
        log(f"{address} disconnecting")
        deleteUsernames = []
        for connectedPlayers in Players:
            if type(Players[connectedPlayers]) == list and len(Players[connectedPlayers]) > 0:
                if Players[connectedPlayers][0] == address:
                    username = connectedPlayers
                    deleteUsernames.append(username)
        
        for usrname in deleteUsernames:
            apiplayerFormer = Players[usrname][2]
            try:
                PrimaryAPI.PlayerDisconnected.InvokeEvent(apiplayerFormer)
            except BaseException as e:
                error("Plugin-handled disconnect event caused an error to occur: " + e)
            del PlayerPositions[usrname]
            del Players[usrname]
            del PlayerLastRecvTime[usrname]
            log(f"{address} ({usrname}) disconnected")
        
    except BaseException as e:
        error(f"Server unable to clean up {address[0]} for reason: {e}")
NetServer.OnDisconnected.Connect(disconnectPlayer)

debug("* Setting up poller for OnReceiveDataFromClient")
def ConnectToEvent(address, data):
    MainServiceCollector.UpdateAllServices(address.getpeername(), data)
NetServer.OnReceiveDataFromClient.Connect(ConnectToEvent)

def RespondToEvent(address):
    return MainServiceCollector.PollAllServices(address.getpeername())
NetServer.OnDataRequest.Connect(RespondToEvent)

debugbright("Setting up serverside plugins...")
pluginmanager = PluginManager()
pluginmanager.LoadAllPlugins()

if getsetting("perRequestMode"):
    error("Your server has Per-Request mode ENABLED. This is a long-removed setting from TFSMP that should NEVER be enabled. Please stop the server and disable Per-Request mode immediately.")

green(f"Server running on {hostaddr}:{hostport}")