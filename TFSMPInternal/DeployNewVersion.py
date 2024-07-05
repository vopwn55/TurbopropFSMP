import requests
import os
import time
import argparse
import hashlib
import re
from Colors import *

def VersionToHash(versionname):
    return hashlib.blake2b(versionname.encode(), digest_size=16).hexdigest()

def IsValidDate(date):
    try:
        time.strptime(date, '%d/%m/%Y')
        return True
    except:
        return False

parser = argparse.ArgumentParser(
                    prog='TFS Multiplayer Deploy Tool v0.0',
                    description='Deploy new TFSMP versions. This is an internal TFSMP tool. Not for public use.',
                    epilog="Run DeployNewVersion.py --help for help")
parser.add_argument("-v", "--version", help="Specify new deploy version name", required=True)
parser.add_argument("-e", "--expirydate", help="Set when the deploy should expire (dd/mm/yyyy)", required=True)
parser.add_argument("-f", "--filedeploy", help="Set the file to deploy", required=True)
parser.add_argument("-x", "--externaldownload", help="Specify whether the download link should be external")

args = parser.parse_args()

bold("TFS Multiplayer Deploy Tool v0.0")
debug("This is an internal TFSMP tool. Not for public use.")
debugbright("Downloading https://vopwn55.xyz/tfsmp/DeployHistory.txt")
DeployHistoryReq = None
DeployHistory = ""
try:
    DeployHistoryReq = requests.get("https://vopwn55.xyz/tfsmp/DeployHistory.txt")
    if DeployHistoryReq.status_code:
        debugbright(f"Downloaded successfully. Status code: {DeployHistoryReq.status_code}")
        if DeployHistoryReq.status_code == 404:
            warn("DeployHistory.txt doesn't exist, most likely due to wiped deploys/first deploy. A new DeployHistory.txt will be created.")
            DeployHistory = "Version - DeployTime - ExpireTime - VersionHash - DownloadLink\n"
        else:
            DeployHistoryReq.encoding = "utf-8"
            DeployHistory = DeployHistoryReq.text
    else:
        warn("Failed to download DeployHistory, cannot continue.")
except:
    warn("Failed to download DeployHistory, cannot continue.")

if IsValidDate(args.expirydate):
    versionhash = VersionToHash(args.version)
    deploydata = f"{args.version} - {time.strftime('%d/%m/%Y %H:%M:%S')} - {args.expirydate} - {versionhash}"
    if args.externaldownload:
        deploydata += " - " + args.externaldownload
    else:
        deploydata += " - https://vopwn55.xyz/tfsmp/" + os.path.basename(args.filedeploy)
    debugbright("Deploying: " + deploydata)
    deploydata += "\n"
    DeployNewFile = DeployHistory.replace("\r","") + deploydata.replace("\r","")
    DeployLocalFile = open("C:/Users/User/Documents/TFSMPInternal/DeployHistory.txt", "w")
    DeployLocalFile.truncate()
    DeployLocalFile.write(DeployNewFile)
    DeployLocalFile.close()
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    try:
        debugbright("Deploying new DeployHistory")
        os.system(f"scp -p -P [REDACTED] C:/Users/User/Documents/TFSMPInternal/DeployHistory.txt root@[REDACTED]:/var/www/html/tfsmp/")
        debugbright("Deploying new File")
        os.system(f"scp -p -P [REDACTED] {args.filedeploy} root@[REDACTED]:/var/www/html/tfsmp/")
        green("Deploy successful!")
    except:
        warn("Failed to deploy something. Check logs")
else:
    warn("Invalid date")