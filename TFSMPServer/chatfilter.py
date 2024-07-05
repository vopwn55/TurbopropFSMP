from difflib import SequenceMatcher
import time
import os
import pathlib
import re
from colorama import Fore, Style

script_directory = pathlib.Path(__file__).parent.resolve()

blockedwords = []
whitelist = ["sick", "port", "ouresucha", "day", "pass", "last", "rash", "ash", "dash", "wnt", "hit", "soex", "wass"]

with open(os.path.join(script_directory, "chatfilter.txt")) as chatfilter:
    blockedwords = chatfilter.readlines()

def similarity(to_compare:str, to_match:str):
    to_compare = to_compare.strip(" \n")
    to_match = to_match.strip(" \n")
    return SequenceMatcher(None, to_compare, to_match).ratio()

def removesuperspacing(spaced: str):
    unspaced = ""
    for word in spaced.split(" "):
        if len(word) > 2:
            unspaced += word + " "
        else:
            unspaced += word
    return unspaced.strip()

def filterstring(tofilterog: str, aggressiveness=0.3):
    tofilterraw = re.sub(r'[^a-zA-Z\s]', '', tofilterog)
    tofilterraw = removesuperspacing(tofilterraw)
    if aggressiveness < 0:
        aggressiveness = 0
    if aggressiveness > 1:
        aggressiveness = 1
    if len(tofilterraw) < 3:
        return [True]
    for tofilter in tofilterraw.split():
        for index2 in range(len(tofilter)):
            index = index2 - 1
            for word in blockedwords:
                availableSize = min(len(tofilter) - index, len(word))
                if availableSize < 3:
                    return [True]
                reduceaggressiveness = (len(word)-4)*0.1
                if reduceaggressiveness < 0:
                    reduceaggressiveness = 0
                #print(f"comparing: {tofilter[index:index+availableSize]} and {word[:availableSize]}")
                if tofilter[index:index+availableSize].lower() in whitelist:
                    continue
                if similarity(tofilter[index:index+availableSize].lower(), word[:availableSize].lower()) > 1 - aggressiveness + reduceaggressiveness:
                    return [tofilter[index:index+availableSize].strip(" \n"), word.strip(" \n"), 1 - aggressiveness + reduceaggressiveness]
    return [True]

#while True:
#    tofilter = input("To filter: ")
#    p1 = time.perf_counter()
#    result = filterstring(tofilter)
#    if result[0] == True:
#        print(Fore.GREEN + "Not blocked" + Style.RESET_ALL)
#    else:
#        print(f"{Fore.RED}Blocked due to word {result[0]} matching {result[1]} with {result[2]*100}% certainty {Style.RESET_ALL}")
#    p2 = time.perf_counter()
#    print(f"Took {p2-p1} seconds\n")