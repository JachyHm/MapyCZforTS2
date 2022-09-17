# MapyCZforTS_CS
## CZ:
Jednoduchá utilitka s grafickým prostředím k nahrazení místy zastaralých Google Map v Train Simulatoru mapami od Seznamu/Bingu.<br>
Jedná se o kompletně přepracovanou verzi původní Pythoní aplikace vytvořené za stejným účelem několik málo let zpět.<br>
Bohužel u staré aplikace nebylo zvoleno nejvhodnější technické řešení spočívající v editaci souboru hosts, které pouze způsobilo nemalé technické potíže.<br>

Tato verze je proto postavena na principu vlastního proxy serveru plnícího funkci MITM mezi místním zařízením a mapovým API Google.<br>
Z toho důvodu není při používání této aplikace potřeba žádný API klíč.<br>
Všechny požadavky na Google API jsou zachyceny ještě na lokálním zařízení a jsou nahrazeny mapovými podklady Seznam/Bing.<br>

### Instalace:
Aplikaci stačí stáhnout do libovolného umístění a spustit.

### Použití:
![Hlavní obrazovka](https://user-images.githubusercontent.com/26261651/190869380-4924f11b-581a-4509-b211-c51d77bdd57d.png)

Po zapnutí se zobrazí minimalistické grafické prostředí, kde lze vybrat mapový podklad, zapnout/vypnout proxy server, případně upravit další nastavení.<br>
Zapnutí překládání podkladů se provede stiskem tlačítka "Zapnout proxy".

![Settings](https://user-images.githubusercontent.com/26261651/190869657-14ce3bf0-fddc-4249-9622-29af8bed6c60.png)

V záložce nastavení se dají upravovat další parametry aplikace, jako například port na kterém poběží proxy server.<br>
*(port není potřeba nikam jinam zadávat, stačí zvolit takový, který není využíván žádným jiným procesem)*

V případě problémů se zde dá rovněž zapnout pokročilý výpis, případně log zobrazit.

Pro pomalejší připojení doporučuji ponechat zapnuté persistentní ukládání mapových snímků. Snižuje se tím objem přenesených dat, protože každý snímek aplikace stahuje pouze jednou. Nicméně mohou zabírat místo na disku, takže je v případě potřeby lze také ručně smazat.


### Obecná doporučení:
* Protože mapové snímky ukládá do mezipaměti také přímo Train Simulator, doporučuji přepínat mapové podklady, případně zapínat/vypínat aplikaci před zapnutím editoru. V opačném případě mohou zůstat některé snímky načtené v paměti a nemusí se zobrazovat správně.<br>
*Toto chování se dá obejít použitím parametru `-DontUseBlueprintCache`, který hře zabrání ukládat do mezipaměti většinu dat. Tento parametr obecně doporučuji všem stavitelům, jelikož dokáže ušetřit nemálo času jinak stráveným mazáním cache, případně restartováním celé hry.*
* Aplikace ke své funkčnosti potřebuje připojení k internetu. V případě dotazu toto povolte.
* Přestože aplikace sama zálohuje původní nastavení proxy serveru a po svém ukončení ho obnoví zpět, silně doporučuji si toto nastavení zálohovat také.
* Aplikace je testována pouze na nejnovějších systémech Windows 10 a Windows 11. Přesto může, ale nemusí fungovat i na jiných verzích.

## EN:
Simple GUI application to replace Train Simulator's paid and sometimes outdated Google satelite images with free Czech Seznam/Bing ones.<br>
This app is reworked version of deprecated Python app created for the same purpose by me few years ago.<br>
Unfortunatelly for the old app, we decided to go with editing hosts file, which only caused plenty of technical issues. <br>

This app however is completely based on custom proxy server acting as MITM between local computer and Google Maps API.<br>
Because of that, you don't even need to have valid Google API key while using this utility.<br>
Every request for Google Map tile is intercepted yet on the local machine and replaced with Seznam/Bing imagery.<br>

### Instalation:
Download the app from here, place it anywhere you want and run it.

### Usage:
![Main screen](https://user-images.githubusercontent.com/26261651/190868375-5a697aeb-b5ff-4cf6-b674-4ed33a05e9e5.png)

Simple window will welcome you, where you can select map type, start/stop the proxy server and adjust some other settings.<br>
You can start translating the images by clicking "Enable proxy".

![Settings](https://user-images.githubusercontent.com/26261651/190868626-aea118ad-930f-4b74-b11b-32bd68e75688.png)

On settings page, you can change some properties, such as port the server will run on.<br>
*(don't worry, you won't need to port number anywhere else, just make sure there's no other process using this port)*

In case of any issues, you can enable advanced logging, and view the log file from here.

For slower connections, it is also recommended to leave caching of map tiles enabled. It will save all processed tiles and will never download them again. It can consume some space though, so in such case, you can delete these images manually.


### General advices
* Due to TS ability to cache images internally ingame, I highly recommend to always toggle map types (or the application at all) before opening editor. Otherwise some images may persist loaded in memory and won't change until next restart.<br>
*You can avoid this by supplying `-DontUseBlueprintCache` to TS as startup parameter. This is recommended for developers anyways, as it will save you a lot of time with clearing cache ingame and restarting the game.*
* Make sure you allow this app access to network. It won't work without.
* Although the app backups any currently used proxy servers and restores them afterwards, make sure you write down these settings too. I do not take any responsibility for corrupting previously used proxy settings.
* Application is only tested under latest stable Windows 10 and Windows 11, it may or may not work on other systems.
