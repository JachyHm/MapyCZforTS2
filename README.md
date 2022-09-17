# MapyCZforTS_CS
## CZ:
Jednoduchá utilitka s grafickým prostředím k nahrazení místy zastaralých Google Map v Train Simulatoru mapami od Seznamu/Bingu.<br>
Jedná se o kompletně přepracovanou verzi [původní Pythoní aplikace](https://github.com/JachyHm/MapyCZforTS) vytvořené za stejným účelem několik málo let zpět.<br>
Bohužel u staré aplikace nebylo zvoleno nejvhodnější technické řešení spočívající v editaci souboru hosts, které u mnoha uživatelů způsobilo nemalé technické potíže.<br>

Tato verze je proto postavena na principu vlastního proxy serveru plnícího funkci MITM mezi místním zařízením a mapovým API Google.<br>
Z toho důvodu není při používání této aplikace potřeba žádný API klíč.<br>
Všechny požadavky na Google API jsou zachyceny ještě na lokálním zařízení a jsou nahrazeny mapovými podklady Seznam/Bing.<br>

### Instalace:
[Aplikaci stačí stáhnout](http://github.com/JachyHm/MapyCZforTS2/releases/latest/download/MapyCZforTS2.exe) do libovolného umístění a spustit.

### Použití:
![Hlavní obrazovka](https://user-images.githubusercontent.com/26261651/190869380-4924f11b-581a-4509-b211-c51d77bdd57d.png)

Po zapnutí se zobrazí minimalistické grafické prostředí, kde lze vybrat mapový podklad, zapnout/vypnout proxy server, případně upravit další nastavení.<br>
Zapnutí překládání podkladů se provede stiskem tlačítka "Zapnout proxy".

![Settings](https://user-images.githubusercontent.com/26261651/190869657-14ce3bf0-fddc-4249-9622-29af8bed6c60.png)

V záložce nastavení se dají upravovat další parametry aplikace, jako například port na kterém poběží proxy server.<br>
*(port není potřeba nikam jinam zadávat, stačí zvolit takový, který není využíván žádným jiným procesem)*

V případě problémů se zde dá rovněž zapnout pokročilý výpis, případně log zobrazit.

Při pomalejším připojení doporučuji ponechat zapnuté persistentní ukládání mapových podkladů do mezipaměti, které sice bude zabírat místo na disku. Snižuje se tím ale objem přenesených dat, protože již stažený snímek nebude aplikace nikdy stahovat znovu a to ani při jeho změně na serveru. V případě potřeby je lze proto také ručně smazat.


### Obecná doporučení:
* Protože mapové snímky ukládá do mezipaměti také přímo Train Simulator, doporučuji přepínat mapové podklady, případně zapínat/vypínat aplikaci před zapnutím editoru. V opačném případě mohou zůstat některé snímky načtené v paměti a nemusí se zobrazovat správně.<br>
*Toto chování se dá obejít použitím parametru `-DontUseBlueprintCache`, který hře zabrání ukládat do mezipaměti většinu dat. Tento parametr obecně doporučuji všem stavitelům, jelikož dokáže ušetřit nemálo času jinak stráveným mazáním cache, případně restartováním celé hry.*
* Aplikace ke své funkčnosti potřebuje připojení k internetu. V případě dotazu toto povolte.
* Přestože aplikace sama zálohuje původní nastavení proxy serveru a po svém ukončení ho obnoví zpět, silně doporučuji si toto nastavení zálohovat také.
* Aplikace pro svůj běh vyžaduje .NET 6.0.
* Aplikace je testována pouze na nejnovějších systémech Windows 10 a Windows 11. Přesto může, ale nemusí fungovat i na jiných verzích.

## EN:
Simple GUI application to replace Train Simulator's paid and sometimes outdated Google satelite images with free Czech Seznam/Bing ones.<br>
This app is a reworked version of [an obsolete Python app](https://github.com/JachyHm/MapyCZforTS) created for the same purpose by me few years ago.<br>
Unfortunately for the old app, we decided to go with editing the hosts file, which only caused plenty of technical issues.<br>

This app however is completely based on a custom proxy server acting as a MITM between local computer and Google Maps API.<br>
Because of that, having a valid Google API key is no longer required while using this utility.<br>
Every request for a Google Map tile is intercepted while still on the local machine and replaced with Seznam/Bing imagery.<br>

### Instalation:
[Download the app from here](http://github.com/JachyHm/MapyCZforTS2/releases/latest/download/MapyCZforTS2.exe), place it anywhere you want and run it.

### Usage:
![Main screen](https://user-images.githubusercontent.com/26261651/190868375-5a697aeb-b5ff-4cf6-b674-4ed33a05e9e5.png)

You will be welcomed by a simple window where you can select map type, start/stop the proxy server and adjust some other settings.<br>
You can start translating the images by clicking "Enable proxy".

![Settings](https://user-images.githubusercontent.com/26261651/190868626-aea118ad-930f-4b74-b11b-32bd68e75688.png)

On the Settings page, you can change properties, such as port the server will run on.<br>
*(don't worry, you won't need the port number anywhere else, just make sure there's no other process using this port)*

In case of any issues, you can enable advanced logging, and also view the log file from here.

For slower connections, it is also recommended to leave the caching of map tiles enabled. It will save all processed tiles and will never download them again, unless you manually delete them which can be also done from setting page.


### General advice
* Due to TS ability to cache images internally ingame, it is highly recommended to always toggle map types (or start /close the application itself) before opening the editor. Otherwise, some images may remain loaded in the memory and they won't change until the next restart.<br>
*You can avoid this by supplying `-DontUseBlueprintCache` to TS as startup parameter. This is recommended for developers anyways, as it will save you a lot of time with clearing cache ingame and restarting the game.*
* Make sure you allow this app access to network. It won't work without it.
* Although the app backups any currently used proxy servers and restores them afterwards, make sure you write down these settings too. I do not take any responsibility for previously used proxy settings getting corrupted.
* Application needs .NET 6.0 installed.
* Application was only tested under latest stable Windows 10 and Windows 11, it may or may not work on other systems.
