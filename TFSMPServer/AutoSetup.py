
import textwrap
import importlib.util
import subprocess
import time
import threading
import json
import os
import pathlib

import pip
subprocess.check_call(["python", "-m", "ensurepip", "--default-pip"])
try:
    __import__("curses")
except:
    pip.main(['install', 'curses'])

import curses

script_directory = pathlib.Path(__file__).parent.resolve()

def terminalizeMessage(text):
        return textwrap.fill(text, curses.COLS - 3, replace_whitespace=False, drop_whitespace=False)

serversettings = {
    "hostAddress":"0.0.0.0",
    "hostPort":8880,
    
    "serverName":"",
    "serverDescription":"",
}

try:
    stdscr = curses.initscr()
    if curses.COLS < 30 or curses.LINES < 8:
        curses.endwin()
        input("This program requires at least 30 columns and 8 lines of terminal space. Please resize your terminal window or get a new terminal.\n")

    stdscr = curses.initscr()
    curses.noecho()
    curses.start_color()
    curses.curs_set(0)
    curses.init_pair(1, curses.COLOR_WHITE, curses.COLOR_BLUE)
    progresswindow = curses.newwin(1, curses.COLS - 1, 0, 0)
    setupwindow = curses.newwin(curses.LINES - 6, curses.COLS - 1, 1, 0)

    descheight = 0

    def setprogress(progress):
        maxwidth = min(60, curses.COLS - len("Progress: [] 100% "))
        progresstring = f"Progress: [{'|' * int(progress/100*maxwidth)}{' ' * int((100 - progress)/100*maxwidth)}] {int(progress)}%"
        realstring = progresstring + (" " * (curses.COLS - len(progresstring) - 2))
        progresswindow.addstr(0,0,realstring, curses.color_pair(1))
        progresswindow.refresh()

    def settitle(title):
        setupwindow.addstr(1,0,title + (" " * (curses.COLS - 2 - len(title))), curses.A_BOLD)
        setupwindow.refresh()

    def setdescription(description):
        global descheight
        setupwindow.addstr(3,0," "*(curses.COLS * descheight))
        setupwindow.addstr(3,0,terminalizeMessage(description))
        descheight = terminalizeMessage(description).count("\n") + 2
        setupwindow.refresh()

    def displayoptions(opts:list):
        optionId = 0
        setupwindow.addstr(descheight + 4, 0, "ACTIONS", curses.A_BOLD)
        for option in opts:
            setupwindow.addstr(descheight + 5 + optionId, 0, f"{optionId+1}: {option}")
            optionId += 1
        setupwindow.refresh()
        key = stdscr.getkey()
        try:
            if not int(key) or int(key) > len(opts):
                displayoptions(opts)
                return
        except:
            displayoptions(opts)
            return
        optionId = 0
        setupwindow.addstr(descheight + 4, 0, " " * (curses.COLS - 1))
        for option in opts:
            setupwindow.addstr(descheight + 5 + optionId, 0, " " * (curses.COLS - 1))
            optionId += 1
        return int(key)
    
    def entrybox(demand:str):
        setupwindow.addstr(descheight + 4, 0, "-------", curses.A_BOLD)
        currententry = ""
        setupwindow.addstr(descheight + 5,0,terminalizeMessage(demand + currententry + (" " * (curses.COLS - 5 - len(currententry) - len(demand)))))
        setupwindow.refresh()
        while True:
            key = setupwindow.getkey()
            if key == "\n":
                setupwindow.addstr(descheight + 4, 0, " " * (curses.COLS - 1))
                setupwindow.addstr(descheight + 5,0,terminalizeMessage((" " * (len(currententry) + len(demand)))))
                setupwindow.refresh()
                return currententry
            elif key == "^C":
                raise BaseException()
            elif key == "^H" or key == "\x08":
                currententry = currententry[:-1]
                setupwindow.addstr(descheight + 5,0,terminalizeMessage(demand + currententry + (" " * (curses.COLS - 5 - len(currententry) - len(demand)))))
                setupwindow.refresh()
            else:
                currententry += key
                setupwindow.addstr(descheight + 5,0,terminalizeMessage(demand + currententry + (" " * (curses.COLS - 5 - len(currententry) - len(demand)))))
                setupwindow.refresh()

    showProgressbarSetup = True

    def initProgressbarIndeterminate():
        global showProgressbarSetup
        width = 25
        position = 0
        while True:
            position += 1
            if position > width:
                position = 1
            time.sleep(0.1)
            linesToShow = 4
            if position < linesToShow:
                linesToShow = position
            if position > width - linesToShow:
                linesToShow = width - position
            preSpace = max(position - 4, 0)
            postSpace = width - preSpace + (4 - position)
            if showProgressbarSetup:
                setupwindow.addstr(descheight + 5,0,f'[{" " * preSpace}{"-"*linesToShow}{" " * postSpace}]')
                setupwindow.refresh()
            else:
                setupwindow.addstr(descheight + 5,0," " * (curses.COLS - 1))
                setupwindow.refresh()
                break

    #todo: make setup functional

    def check_installation(package_name):
        spec = importlib.util.find_spec(package_name)
        return spec is not None

    def install_pip():
        subprocess.check_call(["python", "-m", "ensurepip", "--default-pip"])
    
    def installStage():
        global showProgressbarSetup
        setprogress(75)
        settitle("Let's secure your server")
        setdescription("Would you like to install the Security Essentials 2024 plugin?\nSecurity Essentials 2024 provides basic protection against exploits and hackers.")
        se2024 = displayoptions(["Install", "Do not install"])
        setdescription("Would you like to install the Admin Dashboard plugin?\nAdmin Dashboard provides a simple and intuitive way to interact with your server, kick and ban wrongdoers, and see error logs.")
        ad = displayoptions(["Install", "Do not install"])
        setprogress(90)
        settitle("That's all!")
        setdescription("The Setup has gathered all required info to configure the TFSMP server. Choose Finish to begin.")
        displayoptions(["Finish"])
        threading.Thread(target=initProgressbarIndeterminate).start()
        settitle("Configuring the TFSMP server...")
        setdescription("Installing Colorama...")
        pip.main(['install', 'colorama'])
        setdescription("Writing to TFSMP configuration file...")
        json.dump(serversettings, open(os.path.join(script_directory, "config.json"), "w"))
        time.sleep(0.5)
        setdescription("Installing plugins...")
        if os.path.exists(os.path.join(script_directory, "ServersidePlugins", "!_Security Essentials 2024")) and se2024 == 1:
            os.rename(os.path.join(script_directory, "ServersidePlugins", "!_Security Essentials 2024"), os.path.join(script_directory, "ServersidePlugins", "Security Essentials 2024"))
        if os.path.exists(os.path.join(script_directory, "ServersidePlugins", "!_AdminDashboard")) and ad == 1:
            os.rename(os.path.join(script_directory, "ServersidePlugins", "!_AdminDashboard"), os.path.join(script_directory, "ServersidePlugins", "AdminDashboard"))
        time.sleep(1)
        #setdescription("Adding the TFSMP server to startup list...")
        #time.sleep(0.5)
        setprogress(100)
        showProgressbarSetup = False
        time.sleep(0.1)
        settitle("Success!")
        setdescription("The TFSMP server has been configured and is ready for use. Select Quit to close this setup.\nYou can launch the server by running Server.py in the directory of the setup.")
        displayoptions(["Quit"])
        
    
    def networkStage():
        setprogress(45)
        settitle("Let's prepare your server")
        setdescription("What IP address should your server be hosted on?\nLeave this question blank to use 0.0.0.0 (default), or enter your own IP address.")
        ipaddr = entrybox("Use IP: ")
        if ipaddr == "":
            ipaddr = "0.0.0.0"
        
        setdescription("What port should your server be hosted on?\nLeave this question blank to use 8880 (default), or enter your own port.")
        port = entrybox("Use port: ")
        if port == "":
            port = "8880"
        setprogress(60)
        #setdescription("Should the server be launched on startup? Note that this will only work on Windows and Linux systems.")
        #startup = displayoptions(["Yes, add to startup list", "No"])
        startuptext = "No"
        #if startup == 1:
        #    startuptext = "Yes"
        setdescription(f"Does this look good?\n\nIP address: {ipaddr}\nServer description: {port}\nRun server on startup: {startuptext}")
        option = displayoptions(["Yes, keep this", "No, change"])
        if option == 2:
            serversettings["hostAddress"] = ipaddr
            serversettings["hostPort"] = int(port)
            networkStage()
        else:
            installStage()

    
    def welcomeStage():
        setprogress(15)
        settitle("Let's define your server")
        setdescription("What should the name of your server be?")
        servername = entrybox("Server name: ")
        setprogress(30)
        setdescription("What should the description of your server be?")
        serverdesc = entrybox("Server description: ")
        setdescription(f"Does this look good?\n\nServer name: {servername}\nServer description: {serverdesc}")
        option = displayoptions(["Yes, keep this", "No, change"])
        if option == 2:
            welcomeStage()
        else:
            serversettings["serverName"] = servername
            serversettings["serverDescription"] = serverdesc
            networkStage()
    setprogress(0)
    settitle("Welcome")
    setdescription("This setup will guide you through setting up the TFSMP server. Press 1 when you're ready to proceed.")
    displayoptions(["Begin"])
    welcomeStage()
    
    
except BaseException as e:
    print("The Setup cannot continue due to an error: " + e)

curses.echo()
curses.curs_set(1)
curses.endwin()