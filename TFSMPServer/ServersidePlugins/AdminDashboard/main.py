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

import textwrap
import curses
import re
import socket
import os
sys.path.append(FilePath)
from colors import *
from colorama import Style, Back, Fore, init
debug("[AdminDashboard] Hooking into I/O...")

banlist = {}
def RefreshBanList():
    global banlist
    if not os.path.exists(os.path.join(FilePath, "banlist.txt")):
        open(os.path.join(FilePath, "banlist.txt"), "a").close()
    banfile = open(os.path.join(FilePath, "banlist.txt"), "r")
    banlines = banfile.readlines()
    for line in banlines:
        # line format: ip/username
        try:
            data = line.split("/")
            banlist[data[0]] = data[1]
        except:
            pass
    banfile.close()

def Ban(ip="2.2.2.2", username="NoUsernameProvided"):
    global banlist
    banlist[ip] = username
    banfile = open(os.path.join(FilePath, "banlist.txt"), "a")
    banfile.write(f"\n{ip}/{username}")
    banfile.close()
    for player in PrimaryAPI.Players:
        try:
            if PrimaryAPI.Players[player][2].Username == username or PrimaryAPI.Players[player][0][0] == ip:
                PrimaryAPI.Players[player][1].sendall(b'{"Message":"Connection validated", "!!VoscriptPluginData":["PopupWindow(Disconnected from server: You have been banned from this server. You can no longer join this server.,Close)"]}' + "\x1C".encode())
                PrimaryAPI.Players[player][1].shutdown(socket.SHUT_RDWR)
                output("Bankicked " + username)
        except:
            output("Fail")


def commandProcessor(text:str):
    args = text.split(" ")
    if len(args) == 0:
        output(get_formatted_time() + "Enter a command", curses.color_pair(3))
        return
    # sorry for using if statements, i hate myself too
    if args[0] == "^C":
        output("Enter \"shutdown\" to shut down the server.")
        return
    if args[0] == "shutdown":
        exit()
        return
    if args[0] == "usersonline":
        plrcount = len(PrimaryAPI.Players)
        if plrcount == 0:
            output(f"There are {plrcount} users online.")
            return
        if plrcount == 1:
            output(f"There is {plrcount} user online:")
        else:
            output(f"There are {plrcount} users online:")
        for player in PrimaryAPI.Players:
            output(PrimaryAPI.Players[player][2].Username)
        return
    if args[0] == "kick" and len(args) > 1:
        realName = args[1]
        for player in list(PrimaryAPI.Players.keys()):
            if player.startswith(args[1]):
                realName = player
        plr:tfsmp.APIPlayer = PrimaryAPI.GetAPIPlayer(realName)
        if plr:
            plr.Kick()
            PrimaryAPI.Chat("User " + realName + " has been kicked from this server.")
            output("Kicked")
        return
    if args[0] == "ban" and len(args) > 1:
        realName = args[1]
        for player in list(PrimaryAPI.Players.keys()):
            if player.startswith(args[1]):
                realName = player
        plr:tfsmp.APIPlayer = PrimaryAPI.GetAPIPlayer(realName)
        if plr:
            try:
                Ban(ip=PrimaryAPI.Players[plr.Username][0][0], username=plr.Username)
            except Exception as e:
                output("Failed to get user's IP: " + e)
                Ban(username=plr.Username)
                PrimaryAPI.Chat("User " + realName + " has been banned from this server.")
            output("Ban attempted: " + realName)
        return
    if args[0] == "refreshbans":
        RefreshBanList()
        output("Refreshed")
        return
    
    output(get_formatted_time() + "Unknown command", curses.color_pair(3))

init()
try:
    # initialize ui
    stdscr = curses.initscr()
    curses.start_color()
    curses.init_pair(1, curses.COLOR_RED, curses.COLOR_BLACK)
    curses.init_pair(2, curses.COLOR_GREEN, curses.COLOR_BLACK)
    curses.init_pair(3, curses.COLOR_YELLOW, curses.COLOR_BLACK)
    outputwindow = curses.newwin(curses.LINES - 6, curses.COLS - 1, 3, 0)
    outputwindow.scrollok(True)
    cmdinputwindow = curses.newwin(1, curses.COLS - 1, curses.LINES - 1, 0)

    def remove_ansi(text):
        ansi_escape = re.compile(r'\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])')
        return ansi_escape.sub('', text)

    def terminalizeMessage(text):
        return textwrap.fill(text, curses.COLS, replace_whitespace=False, drop_whitespace=False)

    def output(text, opt=curses.A_NORMAL):
        outputwindow.addstr(terminalizeMessage(text) + "\n", opt)
        outputwindow.refresh()

    class OutputInterceptor:
        def write(self, text):
            output(remove_ansi(text).rstrip('\n'))

    stdscr.addstr(0,0,"// AdminConsole // v1.0", curses.A_REVERSE)
    stdscr.addstr(1,0,"Server Output")
    stdscr.addstr(2,0,"+" + ("=" * (curses.COLS - 4)) + "+")
    stdscr.addstr(curses.LINES - 3,0,"+" + ("=" * (curses.COLS - 4)) + "+")
    stdscr.addstr(curses.LINES - 2,0,"Admin Command Input")
    stdscr.addstr(curses.LINES - 1,0,">" + (" " * (curses.COLS - 3)), curses.A_REVERSE)
    stdscr.refresh()
    outputwindow.refresh()
    # box = Textbox(cmdinputwindow)

    sys.stdout = OutputInterceptor()

    def testplayeroutput(APIPlayer: tfsmp.APIPlayer):
        output("New player connected: " + APIPlayer.Username)
        if PrimaryAPI.Players[APIPlayer.Username][0][0] in banlist:
            PrimaryAPI.Players[APIPlayer.Username][1].sendall(b'{"Message":"Connection validated", "!!VoscriptPluginData":["PopupWindow(Disconnected from server: You have been banned from this server. You can no longer join this server.,Close)"]}' + "\x1C".encode())
            PrimaryAPI.Players[APIPlayer.Username][1].shutdown(socket.SHUT_RDWR)

    PrimaryAPI.PlayerConnected.Connect(testplayeroutput)
    RefreshBanList()
    output(get_formatted_time() + "AdminDashboard plugin started.", curses.color_pair(2))
    currententry = ""
    cmdinputwindow.addstr(0,0,"> " + currententry + (" " * (curses.COLS - 5 - len(currententry))),curses.A_REVERSE)
    cmdinputwindow.refresh()
    while True:
        key = cmdinputwindow.getkey()
        if key == "\n":
            output("> " + currententry)
            commandProcessor(currententry)
            currententry = ""
            cmdinputwindow.addstr(0,0,"> " + currententry + (" " * (curses.COLS - 5 - len(currententry))),curses.A_REVERSE)
            cmdinputwindow.refresh()
        elif key == "^C":
            output("Enter \"shutdown\" to shut down the server.")
        elif key == "^H" or key == "\x08":
            currententry = currententry[:-1]
            cmdinputwindow.addstr(0,0,"> " + currententry + (" " * (curses.COLS - 5 - len(currententry))),curses.A_REVERSE)
            cmdinputwindow.refresh()
        else:
            if len(currententry) < curses.COLS - 5:
                currententry += key
                cmdinputwindow.addstr(0,0,"> " + currententry + (" " * (curses.COLS - 5 - len(currententry))),curses.A_REVERSE)
                cmdinputwindow.refresh()
except Exception as e:
    output(get_formatted_time() + f"Something critical happened and the AdminDashboard plugin is no longer running!\nError: {e}", curses.color_pair(1))