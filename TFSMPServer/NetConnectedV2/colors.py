import time
from colorama import Fore, Back, Style


def warn(txt):
    print(get_formatted_time() + Fore.YELLOW + str(txt) + Style.RESET_ALL)

def tip(txt):
    print(Fore.BLUE + "Tip: " + str(txt) + Style.RESET_ALL)

def log(txt):
    print(get_formatted_time() + str(txt))

def error(txt):
    print(get_formatted_time() + Fore.RED + str(txt) + Style.RESET_ALL)

def get_formatted_time():
    formatted_time = time.strftime("[%H:%M:%S %d-%m-%y] ")
    return f"{Fore.LIGHTBLACK_EX}{formatted_time}{Style.RESET_ALL}"

def bold(txt):
    print(Style.BRIGHT + str(txt) + Style.RESET_ALL)

def listindex(index, txt):
    print(Style.BRIGHT + str(index) + ": " + Style.RESET_ALL + str(txt))

def green(txt):
    print(get_formatted_time() + Fore.GREEN + str(txt) + Style.RESET_ALL)

def debug(txt):
    #if getsetting("DebugMode") == True:
    print(get_formatted_time() + Fore.BLUE + str(txt) + Style.RESET_ALL)

def debugbright(txt):
    #if getsetting("DebugMode") == True:
    print(get_formatted_time() + Fore.LIGHTBLUE_EX + Style.BRIGHT + str(txt) + Style.RESET_ALL)