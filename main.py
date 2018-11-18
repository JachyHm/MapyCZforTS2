import os
from shutil import copyfile, move
from http.server import BaseHTTPRequestHandler, HTTPServer
import atexit
import time
import requests
import urllib, urllib.request
import math
import sys
import socket, errno
import ctypes
import configparser
from tkinter import *
from tkinter import filedialog
from tkinter import messagebox
from tkinter import ttk
from threading import Thread
from PIL import Image
import tempfile
from collections import OrderedDict
import ico

ICON = ico.ikona

_, ICON_PATH = tempfile.mkstemp()
with open(ICON_PATH, 'wb') as icon_file:
    icon_file.write(ICON)

root = Tk()
root.iconbitmap(default=ICON_PATH)
root.withdraw()

def is_admin():
    try:
        return ctypes.windll.shell32.IsUserAnAdmin()
    except:
        return False

class Main():
    def __init__(self):
        #init promennych
        self.winpath = ""
        self.working_directory_path = ""
        self.logfile_path = ""
        self.debug = False
        self.replaceGoogleImg = False
        self.hostsEditedOK = False
        self.appAlreadyEnded = False
        self.wasMapsGoogleInHosts = False
        self.mapsets = OrderedDict()
        self.currentMapset = ""

        #ziskej puvodni GAPI IP
        self.originalGoogleAPIip = socket.gethostbyname('maps.googleapis.com')

        #precti data z konfigu
        configFile = configparser.RawConfigParser(delimiters=("="))
        configFile.optionxform = str
        try:
            configFile.read(os.path.abspath("MapyCZforTS_settings.ini"))
            self.working_directory_path = configFile['Settings']['working_directory_path']
            self.logfile_path = os.path.join(self.working_directory_path,"MapyCZforTS.log")
            if configFile['Settings']['debug'] == "true" or configFile['Settings']['debug'] == "True":
                self.debug = True
            try:
                configFile['Mapsets']
            except:
                self.log("Konfigurační soubor neobsahoval sekci Mapsets! Generuju základní verzi!")
                root.lift()
                root.attributes('-topmost', True)
                messagebox.showwarning("Chyba!","Konfigurační soubor neobsahoval sekci Mapsets!\nGeneruju základní verzi!")
                root.attributes('-topmost', False)
                configFile_mapsetUpdate = configparser.RawConfigParser(delimiters=("="))
                configFile_mapsetUpdate.optionxform = str
                configFile_mapsetUpdate.read_dict(OrderedDict([
                    ("Mapsets",OrderedDict([
                        ("current","ophoto-m"),
                        ("Základní","base-m:18"),
                        ("Letecká - Bing","ophoto-m:19"),
                        ("Turistická","turist-m:18"),
                        ("Zeměpisná","zemepis-m:18"),
                        ("Zimní","winter-m:18"),
                        ("Historická 1836-1852","army2-m:15"),
                        ("Letecká 2002-2003","ophoto0203-m:20"),
                        ("Letecká 2004-2006","ophoto0406-m:20"),
                        ("Letecká 2010-2012","ophoto1012-m:20"),
                        ("Letecká 2014-2015","ophoto1415-m:20"),
                        ("Letní","turist_aquatic-m:18"),
                        ("Reliéfní","relief-m:15"),
                        ("Apríl 2017","april17-m:18"),
                        ("Apríl 2018","april18-m:18")
                    ]))
                ]))
                configFileHandler = open(os.path.abspath("MapyCZforTS_settings.ini"),"a")
                configFile_mapsetUpdate.write(configFileHandler)
                configFileHandler.close()
                self.mapsets = OrderedDict([
                    ("Základní", {"name":"Základní", "maxZoom":18}),
                    ("ophoto-m",{"name":"Letecká - Bing", "maxZoom":19}),
                    ("turist-m",{"name":"Turistická", "maxZoom":18}),
                    ("zemepis-m",{"name":"Zeměpisná", "maxZoom":18}),
                    ("winter-m",{"name":"Zimní", "maxZoom":18}),
                    ("army2-m",{"name":"Historická 1836-1852", "maxZoom":15}),
                    ("ophoto0203-m",{"name":"Letecká 2002-2003", "maxZoom":20}),
                    ("ophoto0406-m",{"name":"Letecká 2004-2006", "maxZoom":20}),
                    ("ophoto1012-m",{"name":"Letecká 2010-2012", "maxZoom":20}),
                    ("ophoto1415-m",{"name":"Letecká 2014-2015", "maxZoom":20}),
                    ("turist_aquatic-m",{"name":"Letní", "maxZoom":18}),
                    ("relief-m",{"name":"Reliéfní", "maxZoom":15}),
                    ("april17-m",{"name":"Apríl 2017", "maxZoom":18}),
                    ("april18-m",{"name":"Apríl 2018", "maxZoom":18})
                ])
                self.currentMapset = "ophoto-m"
            else:
                if len(configFile['Mapsets']) == 0:
                    self.log("Konfigurační soubor neobsahoval ani jeden mapset! Nastavuji základní letecké mapy.")
                    root.lift()
                    root.attributes('-topmost', True)
                    messagebox.showwarning("Chyba!","Konfigurační soubor neobsahoval ani jeden mapset!\nNastavuji základní letecké mapy.")
                    root.attributes('-topmost', False)
                    configFile_mapsetUpdate = configparser.RawConfigParser(delimiters=("="))
                    configFile_mapsetUpdate.optionxform = str
                    configFile_mapsetUpdate.read_dict(OrderedDict([
                        ("Mapsets",OrderedDict([
                            ("current","ophoto-m"),
                            ("Základní","base-m:18"),
                            ("Letecká - Bing","ophoto-m:19"),
                            ("Turistická","turist-m:18"),
                            ("Zeměpisná","zemepis-m:18"),
                            ("Zimní","winter-m:18"),
                            ("Historická 1836-1852","army2-m:15"),
                            ("Letecká 2002-2003","ophoto0203-m:20"),
                            ("Letecká 2004-2006","ophoto0406-m:20"),
                            ("Letecká 2010-2012","ophoto1012-m:20"),
                            ("Letecká 2014-2015","ophoto1415-m:20"),
                            ("Letní","turist_aquatic-m:18"),
                            ("Reliéfní","relief-m:15"),
                            ("Apríl 2017","april17-m:18"),
                            ("Apríl 2018","april18-m:18")
                        ]))
                    ]))
                    configFileHandler = open(os.path.abspath("MapyCZforTS_settings.ini"),"a")
                    configFile_mapsetUpdate.write(configFileHandler)
                    configFileHandler.close()
                    self.mapsets = OrderedDict([
                        ("Základní", {"name":"Základní", "maxZoom":18}),
                        ("ophoto-m",{"name":"Letecká - Bing", "maxZoom":19}),
                        ("turist-m",{"name":"Turistická", "maxZoom":18}),
                        ("zemepis-m",{"name":"Zeměpisná", "maxZoom":18}),
                        ("winter-m",{"name":"Zimní", "maxZoom":18}),
                        ("army2-m",{"name":"Historická 1836-1852", "maxZoom":15}),
                        ("ophoto0203-m",{"name":"Letecká 2002-2003", "maxZoom":20}),
                        ("ophoto0406-m",{"name":"Letecká 2004-2006", "maxZoom":20}),
                        ("ophoto1012-m",{"name":"Letecká 2010-2012", "maxZoom":20}),
                        ("ophoto1415-m",{"name":"Letecká 2014-2015", "maxZoom":20}),
                        ("turist_aquatic-m",{"name":"Letní", "maxZoom":18}),
                        ("relief-m",{"name":"Reliéfní", "maxZoom":15}),
                        ("april17-m",{"name":"Apríl 2017", "maxZoom":18}),
                        ("april18-m",{"name":"Apríl 2018", "maxZoom":18})
                    ])
                    self.currentMapset = "ophoto-m"
                else:
                    try:
                        self.currentMapset = configFile['Mapsets']['current']
                    except:
                        self.log("Konfigurační soubor neobsahoval poslední použitý mapset!Nastavuji první mapset v konfigurační souboru.")
                        root.lift()
                        root.attributes('-topmost', True)
                        messagebox.showwarning("Chyba!","Konfigurační soubor neobsahoval poslední použitý mapset!\nNastavuji první mapset v konfigurační souboru.")
                        root.attributes('-topmost', False)
                    for mapsetKey in configFile['Mapsets']:
                        if mapsetKey != "current":
                            _url, _maxZoom = configFile['Mapsets'][mapsetKey].split(":")
                            self.mapsets.update({_url:{"name":mapsetKey, "maxZoom":int(_maxZoom)}})
                            if self.currentMapset == "":
                                self.currentMapset = _url

                    configFile.set('Mapsets', 'current', self.currentMapset)

                    # Writing our configuration file to 'example.ini'
                    with open(os.path.abspath("MapyCZforTS_settings.ini"), 'w') as configFileHandler:
                        configFile.write(configFileHandler)
        except:
            self.working_directory_path = os.path.dirname(os.path.abspath(__file__))
            self.logfile_path = os.path.join(self.working_directory_path,"MapyCZforTS.log")
            configFile = configparser.RawConfigParser(delimiters=("="))
            configFile.optionxform = str
            configFile.read_dict(OrderedDict([
                ('Settings',{
                    'working_directory_path':self.working_directory_path,
                    'debug': False
                }), 
                ("Mapsets",OrderedDict([
                    ("current","ophoto-m"),
                    ("Základní","base-m:18"),
                    ("Letecká - Bing","ophoto-m:19"),
                    ("Turistická","turist-m:18"),
                    ("Zeměpisná","zemepis-m:18"),
                    ("Zimní","winter-m:18"),
                    ("Historická 1836-1852","army2-m:15"),
                    ("Letecká 2002-2003","ophoto0203-m:20"),
                    ("Letecká 2004-2006","ophoto0406-m:20"),
                    ("Letecká 2010-2012","ophoto1012-m:20"),
                    ("Letecká 2014-2015","ophoto1415-m:20"),
                    ("Letní","turist_aquatic-m:18"),
                    ("Reliéfní","relief-m:15"),
                    ("Apríl 2017","april17-m:18"),
                    ("Apríl 2018","april18-m:18")
                ]))
            ]))
            configFileHandler = open(os.path.abspath("MapyCZforTS_settings.ini"),"w")
            configFile.write(configFileHandler)
            configFileHandler.close()
            self.mapsets = OrderedDict([
                ("Základní", {"name":"Základní", "maxZoom":18}),
                ("ophoto-m",{"name":"Letecká - Bing", "maxZoom":19}),
                ("turist-m",{"name":"Turistická", "maxZoom":18}),
                ("zemepis-m",{"name":"Zeměpisná", "maxZoom":18}),
                ("winter-m",{"name":"Zimní", "maxZoom":18}),
                ("army2-m",{"name":"Historická 1836-1852", "maxZoom":15}),
                ("ophoto0203-m",{"name":"Letecká 2002-2003", "maxZoom":20}),
                ("ophoto0406-m",{"name":"Letecká 2004-2006", "maxZoom":20}),
                ("ophoto1012-m",{"name":"Letecká 2010-2012", "maxZoom":20}),
                ("ophoto1415-m",{"name":"Letecká 2014-2015", "maxZoom":20}),
                ("turist_aquatic-m",{"name":"Letní", "maxZoom":18}),
                ("relief-m",{"name":"Reliéfní", "maxZoom":15}),
                ("april17-m",{"name":"Apríl 2017", "maxZoom":18}),
                ("april18-m",{"name":"Apríl 2018", "maxZoom":18})
            ])
            self.currentMapset = "ophoto-m"

    #log funkce
    def log(self,msg,prvniRadek=False):
        logfileHandler = open(self.logfile_path, "a")
        print(time.strftime("%H:%M:%S ")+str(msg))
        if prvniRadek and logfileHandler.tell() != 0:
            logfileHandler.write("\n"+time.strftime("%H:%M:%S ")+str(msg)+"\n")
        else:
            logfileHandler.write(time.strftime("%H:%M:%S ")+str(msg)+"\n")
        logfileHandler.close()

    #prepocet WGS na pixely mapy.cz
    def ziskejPixelZWGS(self,wgs_x,wgs_y,zoom):
        velikost_sveta = pow(2, zoom + 8)
        x = (wgs_x + 180) / 360 * velikost_sveta
        y = wgs_y * math.pi / 180
        f = min(max(math.sin(y), -0.9999), 0.9999)
        y = (1 - 0.5 * math.log((1 + f) / (1 - f)) / math.pi) / 2 * velikost_sveta
        return (x, y)
    
    #funkce na obnoveni zalohy pri zavreni aplikace
    def obnovZalohuHosts(self):
        if not self.wasMapsGoogleInHosts:
            self.log("Obnovuji zálohu souboru hosts!")
            if os.path.isfile(self.winpath + "hosts.gmBAK"):
                move(self.winpath + "hosts.gmBAK", self.winpath + "hosts")
                self.log("Záloha hosts byla úspěšně obnovena!")
                return(True)
            else:
                self.log("Jejda! Něco se nepovedlo. Záloha souboru hosts nemohla být obnovena!")
                return(False)
        return(False)

    #parse hosts file and return hosts array
    def parseHosts(self, hosts_path):
        try:
            hostsHandler = open(hosts_path, "r")
            hosts_array = {}
            for line in hostsHandler.read().split("\n"):
                dst_adress, src_adress = line.split(" ")
                if not src_adress in hosts_array:
                    hosts_array[src_adress] = dst_adress
                else:
                    self.log("V souboru hosts se pravděpodobně nachází chyba! Přeskakuji opakovaný výskyt {:s}!".format(src_adress))
            return(hosts_array)
        except:
            return {}

    #check if http port 80 is free
    def checkIfHTTPisFree(self):
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        try:
            s.bind(("127.0.0.1", 80))
        except socket.error as e:
            if e.errno == errno.EADDRINUSE:
                self.log("Kritická chyba! Port 80 je již otevřený jinou aplikací! Nelze pokračovat!")
                root.lift()
                root.attributes('-topmost', True)
                messagebox.showerror("Kritická chyba!","Port 80 je již otevřený jinou aplikací! Nelze pokračovat!")
                root.attributes('-topmost', False)
            elif e.errno in (errno.EACCES, getattr(errno, 'WSAEACCES', errno.EACCES)):
                self.log("Kritická chyba! Systém zakázal přístup k portu 80! Nelze pokračovat!")
                root.lift()
                root.attributes('-topmost', True)
                messagebox.showerror("Kritická chyba!","Systém zakázal přístup k portu 80!\nBuď je používaný jinou aplikací, nebo přístup blokuje antivirový program!\nNelze pokračovat!")
                root.attributes('-topmost', False)
            else:
                pass
                #possible handling other errors with sockets
            return(False)
        s.close()
        return(True)

    #funkce na zalohovani souboru hosts
    def zalohujANastavVlastniHosts(self):
        self.hostsEditedOK = False
        if not os.path.isfile(self.winpath + "hosts.gmBAK"):
                if not "maps.googleapis.com" in self.parseHosts(self.winpath + "hosts"):
                    self.log("Zálohuji soubor hosts!")
                    copyfile(self.winpath + "hosts", self.winpath + "hosts.gmBAK")
                    self.log("Nastavuji maps.googleapis.com na adresu "+str(socket.gethostbyname(socket.gethostname()))+"!")
                    self.wasMapsGoogleInHosts = False
                    try:
                        hosts = open(self.winpath + "hosts", "a")
                        hosts.write("\n"+str(socket.gethostbyname(socket.gethostname()))+" maps.googleapis.com")
                        hosts.close()
                        self.hostsEditedOK = True
                        return(True)
                    except:
                        self.hostsEditedOK = False
                        self.log("Nepovedlo se zapsat do souboru hosts! Nelze pokračovat!")
                        root.lift()
                        root.attributes('-topmost', True)
                        messagebox.showerror("Kritická chyba!","Nepovedlo se zapsat do souboru hosts! Nelze pokračovat!")
                        root.attributes('-topmost', False)
                        return(False)
                elif self.parseHosts(self.winpath + "hosts")["maps.googleapis.com"] in [str(socket.gethostbyname(socket.gethostname())), "127.0.0.1"]:
                    self.wasMapsGoogleInHosts = True
                    root.lift()
                    root.attributes('-topmost', True)
                    if messagebox.askyesno("Co dál?",'V souboru hosts již existuje zápis pro maps.googleapis.com.\n'
                                            'Může jít o duplicitní spuštění aplikace, nebo na vašem počítači běží jiná aplikace, která přesměrovává maps.googleapis.com.'
                                            '\nPřesto spustit?'):
                        root.attributes('-topmost', False)
                        self.hostsEditedOK = True
                        return(True)
                    else:
                        root.attributes('-topmost', False)
                        self.hostsEditedOK = False
                        return(False)
                else:
                    self.log("Kritická chyba! maps.googleapis.com se již v souboru hosts nachází! Nelze pokračovat!")
                    root.lift()
                    root.attributes('-topmost', True)
                    messagebox.showerror("Kritická chyba!","maps.googleapis.com se již v souboru hosts nachází! Nelze pokračovat!")
                    root.attributes('-topmost', False)
                    self.wasMapsGoogleInHosts = True
                    return(False)
        else:
            self.log("Obnovuji nalezený soubor zálohy! Předchozí instance pravděpodobně nebyla správně ukončena!")
            try:
                move(self.winpath + "hosts.gmBAK", self.winpath + "hosts")
            except:
                self.log("Jejda! Něco se nepovedlo. Záloha souboru hosts nemohla být obnovena!")
            if not "maps.googleapis.com" in self.parseHosts(self.winpath + "hosts"):
                self.log("Zálohuji soubor hosts!")
                copyfile(self.winpath + "hosts", self.winpath + "hosts.gmBAK")
                self.log("Nastavuji maps.googleapis.com na adresu "+str(socket.gethostbyname(socket.gethostname()))+"!")
                self.wasMapsGoogleInHosts = False
                try:
                    hosts = open(self.winpath + "hosts", "a")
                    hosts.write("\n"+str(socket.gethostbyname(socket.gethostname()))+" maps.googleapis.com")
                    hosts.close()
                    self.hostsEditedOK = True
                    return(True)
                except:
                    self.hostsEditedOK = False
                    self.log("Nepovedlo se zapsat do souboru hosts! Nelze pokračovat!")
                    root.lift()
                    root.attributes('-topmost', True)
                    messagebox.showerror("Kritická chyba!","Nepovedlo se zapsat do souboru hosts! Nelze pokračovat!")
                    root.attributes('-topmost', False)
                    return(False)
            elif self.parseHosts(self.winpath + "hosts")["maps.googleapis.com"] in [str(socket.gethostbyname(socket.gethostname())), "127.0.0.1"]:
                self.wasMapsGoogleInHosts = True
                root.lift()
                root.attributes('-topmost', True)
                if messagebox.askyesno("Co dál?",'V souboru hosts již existuje zápis pro maps.googleapis.com.\n'
                                        'Může jít o duplicitní spuštění aplikace, nebo na vašem počítači běží jiná aplikace, která přesměrovává maps.googleapis.com.'
                                        '\nPřesto spustit?'):
                    self.hostsEditedOK = True
                    root.attributes('-topmost', False)
                    return(True)
                else:
                    self.hostsEditedOK = False
                    root.attributes('-topmost', False)
                    return(False)
            else:
                self.log("Kritická chyba! maps.googleapis.com se již v souboru hosts nachází! Nelze pokračovat!")
                root.lift()
                root.attributes('-topmost', True)
                messagebox.showerror("Kritická chyba!","maps.googleapis.com se již v souboru hosts nachází! Nelze pokračovat!")
                root.attributes('-topmost', False)
                self.wasMapsGoogleInHosts = True
                return(False)

    #funkce na zpracovani pozadavku GET, nebo POST
    def zpracujPozadavek(self,pozadavek):
        parametry = urllib.parse.parse_qs(pozadavek.path[pozadavek.path.find("?")+1:])
        if self.debug:
            self.log("*#*#*#*#*#*#*#Nový request!*#*#*#*#*#*#*#")
        try:
            y, x = parametry["center"][0].split(",")
            x = float(x)
            y = float(y)
            rozliseniX, rozliseniY = parametry["size"][0].split("x")
            scale = int(parametry["scale"][0])
            rozliseniX = int(rozliseniX)
            rozliseniY = int(rozliseniY)
            zoom = int(parametry["zoom"][0])
            if self.debug:
                self.log("Střed je v bodě {:s}, {:s}!".format(str(x),str(y)))
                self.log("Rozlišení je {:d}x{:d}!".format(int(rozliseniX), int(rozliseniY)))
                self.log("Scale je {:d}!".format(scale))
                self.log("Zoom je {:d}!".format(zoom))
        except:
            pozadavek.send_response(403)
            pozadavek.end_headers()
            self.log("Neplatný požadavek!")
            print("")
        else:
            if not os.path.exists(os.path.join(self.working_directory_path, "mapy_cz_cache")):
                os.makedirs(os.path.join(self.working_directory_path, "mapy_cz_cache"))

            if not os.path.exists(os.path.join(self.working_directory_path, "output_cache")):
                os.makedirs(os.path.join(self.working_directory_path, "output_cache"))

            if not os.path.isfile(os.path.join(self.working_directory_path, "output_cache", "{:s}_{:d}_{:s}-{:s}.jpg".format(self.currentMapset, zoom, str(x).replace(".","_"),str(y).replace(".","_")))):
                #preved souradnice na poradove cislo pixelu z leva nahore 0,0
                pxlX, pxlY = self.ziskejPixelZWGS(x, y, zoom)

                #xove promenne
                pxlOdLevehoKraje = pxlX%256
                stredovyTileX = math.ceil(pxlX/256)
                xOffset = (256-math.floor((rozliseniX/2 - pxlOdLevehoKraje)%256))%256
                pocetTiluDoleva = math.ceil((rozliseniX/2 - pxlOdLevehoKraje + xOffset)/256)
                pocetTiluDoprava = math.ceil((rozliseniX/2 - (256 - pxlOdLevehoKraje))/256)
                cropLeva = xOffset

                #ypsilonove promenne
                pxlOdShora = pxlY%256
                stredovyTileY = math.ceil(pxlY/256)
                yOffset = (256-math.floor((rozliseniY/2 - pxlOdShora)%256))%256
                pocetTiluNahoru = math.ceil((rozliseniY/2 - pxlOdShora + yOffset)/256)
                pocetTiluDolu = math.ceil((rozliseniY/2 - (256 - pxlOdShora))/256)
                cropShora = yOffset

                if self.debug:
                    self.log("Generovany rozsah mapovych ctvercu je {:d}:{:d} - {:d}:{:d}!".format(stredovyTileX - pocetTiluDoleva, stredovyTileY - pocetTiluNahoru, stredovyTileX + pocetTiluDoprava, stredovyTileY + pocetTiluDolu))
                    self.log("Střed se nachází {:f}px zleva a {:f}px shora!".format(pxlOdLevehoKraje, pxlOdShora))
                    self.log("Výsledný obrázek bude ořezán o {:d}px zleva a {:d}px shora!".format(cropLeva, cropShora))

                stazeneCtverce = []
                for y_number in range(stredovyTileY - pocetTiluNahoru, stredovyTileY + pocetTiluDolu+1):
                    for x_number in range(stredovyTileX - pocetTiluDoleva, stredovyTileX + pocetTiluDoprava+1):
                        if not os.path.isfile(os.path.join(self.working_directory_path, "mapy_cz_cache", "{:s}_{:d}_{:d}-{:d}.jpg".format(self.currentMapset,zoom,x_number,y_number))):
                            urllib.request.urlretrieve("https://mapserver.mapy.cz/{:s}/{:d}-{:d}-{:d}".format(self.currentMapset,zoom,x_number,y_number), os.path.join(self.working_directory_path, "mapy_cz_cache", "{:s}_{:d}_{:d}-{:d}.jpg".format(self.currentMapset,zoom,x_number,y_number)))
                        stazeneCtverce.append(os.path.join(self.working_directory_path, "mapy_cz_cache", "{:s}_{:d}_{:d}-{:d}.jpg".format(self.currentMapset,zoom,x_number,y_number)))
                
                pilCtverce = map(Image.open, stazeneCtverce)

                vyslednyObrazek = Image.new("RGB", (rozliseniX+xOffset, rozliseniY+yOffset))

                poradiZLeva = 1
                offsetX = 0
                offsetY = 0
                for obrazek in pilCtverce:
                    if poradiZLeva > pocetTiluDoleva + pocetTiluDoprava + 1:
                        offsetX = 0
                        offsetY += 256
                        poradiZLeva = 1
                    
                    vyslednyObrazek.paste(obrazek,(offsetX,offsetY))

                    offsetX += 256
                    poradiZLeva += 1

                #zjisti sirku a vysku obrazku pro orezani
                #sirka, vyska = vyslednyObrazek.size

                #orez obrazek
                vyslednyObrazek = vyslednyObrazek.crop((cropLeva, cropShora, rozliseniX+cropLeva, rozliseniY+cropShora))

                #zvetsi obrazek podle multiply
                vyslednyObrazek = vyslednyObrazek.resize((rozliseniX*scale,rozliseniY*scale), Image.ANTIALIAS)

                #uloz obrazek
                vyslednyObrazek.save(os.path.join(self.working_directory_path, "output_cache", "{:s}_{:d}_{:s}-{:s}.jpg".format(self.currentMapset, zoom, str(x).replace(".","_"),str(y).replace(".","_"))), "JPEG", quality=75, optimize=True, progressive=True)

            else:
                if self.debug:
                    self.log("Obrázek již existuje! Přeskakuji vytváření!")
            
            #posli response OK!
            pozadavek.send_response(200)
            #budeme posilat obrazek
            pozadavek.send_header('Content-type', 'image/jpeg')
            #velikost nasho souboru
            pozadavek.send_header('Cache-Control', 'public, max-age=86400')
            pozadavek.send_header('Vary', 'Accept-Language')
            pozadavek.send_header('Access-Control-Allow-Origin', '*')
            pozadavek.send_header('Content-Length', os.path.getsize(os.path.join(self.working_directory_path, "output_cache", "{:s}_{:d}_{:s}-{:s}.jpg".format(self.currentMapset, zoom, str(x).replace(".","_"),str(y).replace(".","_")))))
            pozadavek.send_header('X-XSS-Protection', '1; mode=block')
            pozadavek.send_header('X-Frame-Options', 'SAMEORIGIN')
            #odesli hlavicky
            pozadavek.end_headers()
            #posli obrazek
            pozadavek.wfile.write(open(os.path.join(self.working_directory_path, "output_cache", "{:s}_{:d}_{:s}-{:s}.jpg".format(self.currentMapset, zoom, str(x).replace(".","_"),str(y).replace(".","_"))),'rb').read())
            self.log("Obrázek odeslaný OK!")
            if self.debug:
                print("")

    #vrat originalni pozadavek z Google
    def vratOriginalniPozadavek(self,pozadavek):
        try:
            #vrat obsah dotazu na originalni Googleovske API
            googleObrazek = urllib.request.urlopen("http://{:s}{:s}".format(self.originalGoogleAPIip,pozadavek.path)).read()

            #posli response OK!
            pozadavek.send_response(200)
            #budeme posilat obrazek
            pozadavek.send_header('Content-type', 'image/jpeg')
            #velikost nasho souboru
            pozadavek.send_header('Cache-Control', 'public, max-age=86400')
            pozadavek.send_header('Vary', 'Accept-Language')
            pozadavek.send_header('Access-Control-Allow-Origin', '*')
            pozadavek.send_header('Content-Length', len(googleObrazek))
            pozadavek.send_header('X-XSS-Protection', '1; mode=block')
            pozadavek.send_header('X-Frame-Options', 'SAMEORIGIN')
            #odesli hlavicky
            pozadavek.end_headers()
            #posli obrazek
            pozadavek.wfile.write(googleObrazek)
        except:
            self.log("Nastala kritická chyba při zpracování požadavku!")
    
    def appExit(self):
        if not self.appAlreadyEnded:
            self.log("Ukončuji aplikaci na základě požadavku uživatele!")
            self.serverClass.forceStop()
            self.obnovZalohuHosts()
            try:
                root.destroy()
            except:
                pass
            self.appAlreadyEnded = True

    def main(self):
        self.log("Start aplikace!",True)
        self.log("Vyhledávám složku etc!")
        #Najdi Windows\System32\drivers\etc
        self.winpath = os.environ['WINDIR'] + "\\System32\\drivers\\etc\\"
        
        if os.path.isdir(self.winpath):
            self.log("Složka etc nalezena!")
        else:
            self.log("Složka etc nebyla nalezena! Nelze pokračovat!")
            root.lift()
            root.attributes('-topmost', True)
            messagebox.showerror("Kritická chyba!","Složka etc nebyla nalezena! Nelze pokračovat!")
            root.attributes('-topmost', False)
            return(False)

        if not os.path.isfile(self.winpath+"hosts"):
            self.log("Ve složce chybí soubor hosts! Nejde pokračovat!")
            root.lift()
            root.attributes('-topmost', True)
            messagebox.showerror("Kritická chyba!","Ve složce etc chybí soubor hosts! Nejde pokračovat!")
            root.attributes('-topmost', False)
            return(False)

        if not self.checkIfHTTPisFree():
            return(False)

        self.serverClass = HTTP_handler()

        self.log("Nastavuji on close callback!")
        atexit.register(self.appExit)

        if self.zalohujANastavVlastniHosts():
            self.log("OK!")
            return(True)
        else:
            # self.log("Nelze pokračovat, aplikace bude ukončena!")
            return(False)

class Window():
    def closeApplication(self):
        M.appExit()

    def debugTriggered(self):
        M.debug = not M.debug

        try:
            configFile = configparser.RawConfigParser(delimiters=("="))
            configFile.optionxform = str
            configFile.read(os.path.abspath("MapyCZforTS_settings.ini"))
            configFile.set('Settings', 'working_directory_path', M.working_directory_path)

            # Writing our configuration file to 'example.ini'
            with open(os.path.abspath("MapyCZforTS_settings.ini"), 'w') as configFileHandler:
                configFile.write(configFileHandler)
        except:
            M.log("Nepovedlo se uložit změny!")
            M.debug = not M.debug
            self.mainMenuDebugChBoxValue.set(M.debug)
            root.lift()
            root.attributes('-topmost', True)
            messagebox.showinfo(title="Chyba!", message="Nepovedlo se uložit změny!\nOvěřte, zda má aplikace přístup ke konfiguračnímu souboru a zkuste to znovu.")
            root.attributes('-topmost', False)
    
    def chooseWorkingDirectory(self):
        oldWD = M.working_directory_path
        M.working_directory_path = filedialog.askdirectory()

        try:
            configFile = configparser.RawConfigParser(delimiters=("="))
            configFile.optionxform = str
            configFile.read_dict({'Settings':{'working_directory_path': M.working_directory_path, 'debug': M.debug}})
            configFileHandler = open(os.path.abspath("MapyCZforTS_settings.ini"),"w")
            configFile.write(configFileHandler)
            configFileHandler.close()
        except:
            M.log("Nepovedlo se uložit změny!")
            M.working_directory_path = oldWD
            root.lift()
            root.attributes('-topmost', True)
            messagebox.showinfo(title="Chyba!", message="Nepovedlo se uložit změny!\nOvěřte, zda má aplikace přístup ke konfiguračnímu souboru a zkuste to znovu.")
            root.attributes('-topmost', False)
        
        M.logfile_path = os.path.join(M.working_directory_path,"MapyCZforTS.log")

    def turnOnTurnOffTriggered(self):
        M.replaceGoogleImg = not M.replaceGoogleImg
        appdata_path = os.environ["LOCALAPPDATA"]
        M.log("Mažu TS cache mapových podkladů!")
        #win8 + 10
        try:
            deleted = False
            exception = False
            for folder in os.listdir(os.path.join(appdata_path, "Microsoft", "Windows", "INetCache", "IE")):
                folder_path = os.path.join(appdata_path, "Microsoft", "Windows", "INetCache", "IE", folder)
                if os.path.isdir(folder_path):
                    for file in os.listdir(folder_path):
                        if file.find("staticmap") != -1:
                            try:
                                os.unlink(os.path.join(folder_path, file))
                                deleted = True
                            except:
                                exception = True
            if exception:
                M.log("Nepovedlo se vyčistit některé cacheované tily, nové tily se nemusí správně zobrazit!")
                root.lift()
                root.attributes('-topmost', True)
                messagebox.showwarning("Neznámá chyba!", "Nepovedlo se vyčistit některé cacheované tily, nové tily se nemusí správně zobrazit!")
                root.attributes('-topmost', False)
            elif not deleted:
                M.log("Nebyly nalezeny žádné soubory TS cache! Nothing to do!")
        except:
            M.log("Neznámá chyba! Možná systém není Windows 8/10?")
        
        #win7 + XP
        try:
            deleted = False
            exception = False
            for folder in os.listdir(os.path.join(appdata_path, "Microsoft", "Windows", "Temporary Internet Files", "Content.IE5")):
                folder_path = os.path.join(appdata_path, "Microsoft", "Windows", "Temporary Internet Files", "Content.IE5", folder)
                if os.path.isdir(folder_path):
                    for file in os.listdir(folder_path):
                        if file.find("staticmap") != -1:
                            try:
                                os.unlink(os.path.join(folder_path, file))
                                deleted = True
                            except:
                                exception = True
            if exception:
                M.log("Nepovedlo se vyčistit některé cacheované tily, nové tily se nemusí správně zobrazit!")
                root.lift()
                root.attributes('-topmost', True)
                messagebox.showwarning("Neznámá chyba!", "Nepovedlo se vyčistit některé cacheované tily, nové tily se nemusí správně zobrazit!")
                root.attributes('-topmost', False)
            elif not deleted:
                M.log("Nebyly nalezeny žádné soubory TS cache! Nothing to do!")
        except:
            M.log("Neznámá chyba! Možná systém není Windows XP/Vista/7?")

        if M.replaceGoogleImg:
            self.button_text.set("VYPNI MAPY.CZ")
        else:
            self.button_text.set("ZAPNI MAPY.CZ")

    def deleteCache(self):
        M.log("Mažu cache!")
        try:
            for the_file in os.listdir("output_cache"):
                file_path = os.path.join("output_cache", the_file)
                try:
                    if os.path.isfile(file_path):
                        os.unlink(file_path)
                except Exception as e:
                    M.log(e)
        except:
            M.log("Složka output_cache neexistuje. Nothing to do!")
        
        try:
            for the_file in os.listdir("mapy_cz_cache"):
                file_path = os.path.join("mapy_cz_cache", the_file)
                try:
                    if os.path.isfile(file_path):
                        os.unlink(file_path)
                except Exception as e:
                    M.log(e)
        except:
            M.log("Složka mapy_cz_cache neexistuje. Nothing to do!")

        M.log("Cache fuč!")
        root.lift()
        root.attributes('-topmost', True)
        messagebox.showinfo("Hotovo", "Cache úspěšně vymazaná!")
        root.attributes('-topmost', False)

    def mapSetsClicked(self):
        M.currentMapset = self.mapovyPodkladVar.get()
        zoom = M.mapsets[M.currentMapset]["maxZoom"]
        zleva = zoom - 13
        zprava = 21 - zoom
        if zleva > zprava:
            root.lift()
            root.attributes('-topmost', True)
            messagebox.showinfo("Změna mapového podkladu","Změna mapového podkladu proběhla úspěšně!\nNově nastavený mapový podklad má maximální\nzoom {:d}, tj. {:d}. pozice posuvníku ve hře zprava!".format(zoom, zprava))
            root.attributes('-topmost', False)
        else:
            root.lift()
            root.attributes('-topmost', True)
            messagebox.showinfo("Změna mapového podkladu","Změna mapového podkladu proběhla úspěšně!\nNově nastavený mapový podklad má maximální\nzoom {:d}, tj. {:d}. pozice posuvníku ve hře zleva!".format(zoom, zleva))
            root.attributes('-topmost', False)
        try:
            configFile = configparser.RawConfigParser(delimiters=("="))
            configFile.optionxform = str
            configFile.read(os.path.abspath("MapyCZforTS_settings.ini"))
            configFile.set('Mapsets', 'current', self.mapovyPodkladVar.get())

            # Writing our configuration file to 'example.ini'
            with open(os.path.abspath("MapyCZforTS_settings.ini"), 'w') as configFileHandler:
                configFile.write(configFileHandler)
        except:
            M.log("Nepovedlo se uložit změny!")
            root.lift()
            root.attributes('-topmost', True)
            messagebox.showinfo(title="Chyba!", message="Nepovedlo se uložit změny!\nOvěřte, zda má aplikace přístup ke konfiguračnímu souboru a zkuste to znovu.")
            root.attributes('-topmost', False)

    def appWindowCreator(self):
        M.log("Sestavuji GUI rozhraní aplikace!")
        root.option_add('*tearOff', FALSE)
        root.title("MapyCZforTS v.0.4.1.4")
        root.minsize(350,350)

        root.columnconfigure(0, weight=1)
        root.columnconfigure(1, weight=1)
        root.columnconfigure(2, weight=1)
        root.rowconfigure(0, weight=1)
        root.rowconfigure(1, weight=1)
        root.rowconfigure(2, weight=1)
        root.rowconfigure(3, weight=1)
        root.rowconfigure(4, weight=1)

        # self.mainFrame = ttk.Frame(root, borderwidth=0, relief="solid")
        # self.mainFrame.grid(column=0, row=0, columnspan=10, sticky="nwes")
        # self.mainFrame.columnconfigure(0, weight=1)
        # self.mainFrame.columnconfigure(1, weight=1)
        # self.mainFrame.columnconfigure(2, weight=1)
        # self.mainFrame.columnconfigure(3, weight=1)
        # self.mainFrame.columnconfigure(4, weight=1)

        self.button_text = StringVar()
        self.button_text.set("ZAPNI MAPY.CZ")
        self.buttonTriggerOnOff = Button(root, textvariable=self.button_text, command=self.turnOnTurnOffTriggered)
        self.buttonTriggerOnOff.grid(column=1, row=2, sticky="nwes")

        self.mainMenu = Menu(root)

        self.mainMenuSoubor = Menu(self.mainMenu)
        self.mainMenuSoubor.add_command(label="Ukonči aplikaci", command=self.closeApplication)

        self.mainMenuDebugChBoxValue = BooleanVar()
        self.mainMenuDebugChBoxValue.set(M.debug)
        self.mainMenuNastaveni = Menu(self.mainMenu)
        self.mainMenuNastaveni.add_checkbutton(label="Zapisovat do logu detailní informace", command=self.debugTriggered, variable=self.mainMenuDebugChBoxValue)
        self.mainMenuNastaveni.add_command(label="Nastav pracovní adresář", command=self.chooseWorkingDirectory)

        self.mainMenuMapovyPodklad = Menu(self.mainMenu)
        self.mapovyPodkladVar = StringVar()
        self.mapovyPodkladVar.set(M.currentMapset)
        for mapset in M.mapsets:
            self.mainMenuMapovyPodklad.add_radiobutton(label=M.mapsets[mapset]["name"], command=self.mapSetsClicked, value=mapset, variable=self.mapovyPodkladVar)

        self.mainMenu.add_cascade(menu=self.mainMenuSoubor, label="Soubor")
        self.mainMenu.add_cascade(menu=self.mainMenuNastaveni, label="Nastavení")
        self.mainMenu.add_cascade(menu=self.mainMenuMapovyPodklad, label="Mapové podklady")
        self.mainMenu.add_command(label="Vymaž cache", command=self.deleteCache)

        root["menu"] = self.mainMenu

        root.deiconify()
        root.lift()
        root.attributes('-topmost', True)
        root.attributes('-topmost', False)
        root.mainloop()

class HTTP_handler():
    def __init__(self):
        M.log("Zapínám HTTP překladač!")
        self.stop = False
        adresa = ('0.0.0.0', 80)
        self.server = HTTPServer(adresa, WebServerClass)
        self.vlaknoHTTP = Thread(target=self.serve)
        self.vlaknoHTTP.start()

    def serve(self):
        self.serverRunning = True
        M.log("HTTP překladač běží!")
        while not self.stop:
            self.server.handle_request()
        M.log('HTTP server ukončen!')
        self.serverRunning = False

    def forceStop(self):
        self.stop = True
        M.log('Vysílám požadavek k ukončení HTTP serveru!')
        urllib.request.urlopen("http://127.0.0.1/?vypni=True")
        M.log('Čekám na ukončení HTTP serveru!')
        while self.serverRunning:
            pass

class WebServerClass(BaseHTTPRequestHandler):
    def log_message(self, format, message, returnCode, *args):
        pass

    def do_POST(self):
        if "vypni" in urllib.parse.parse_qs(self.path[self.path.find("?")+1:]):
            self.send_response(200)
            self.end_headers()
        else:
            if M.replaceGoogleImg:
                M.zpracujPozadavek(self)
            else:
                M.vratOriginalniPozadavek(self)

    def do_GET(self):
        if "vypni" in urllib.parse.parse_qs(self.path[self.path.find("?")+1:]):
            self.send_response(200)
            self.end_headers()
        else:
            if M.replaceGoogleImg:
                M.zpracujPozadavek(self)
            else:
                M.vratOriginalniPozadavek(self)

M = Main()
W = Window()

if is_admin():
    try:
        if M.main():
            W.appWindowCreator()
            M.appExit()
    except KeyboardInterrupt:
        M.log("User keyboard interrupt!")
        M.appExit()
else:
    # Ask for re-run the program with admin rights
    root.lift()
    root.attributes('-topmost', True)
    if messagebox.askyesno("Co dál?",'Aplikace nebyla spuštěna jako správce.\n'
                            'To je ale vzhledem k editaci souboru hosts v systémové složce Windows nezbytně nutné.'
                            '\nRestartovat aplikaci jako správce?'):
        root.attributes('-topmost', False)
        ctypes.windll.shell32.ShellExecuteW(None, "runas", sys.executable, __file__, None, 1)