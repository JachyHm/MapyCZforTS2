# MapyCZforTS
![Náhled aplikace](/MapyCZforTSexample.jpg)

Pythoní aplikace k nahrazení místy zastaralých map od Google mapami od Seznamu.

Aplikace je zcela volně šiřitelná, kdokoli může jakkoli využívat, či upravovat její části.
Neručím za žádné znefunkčnění aplikace, nebo snad ještě něčeho dalšího neodbornými úpravami.

Aplikace je vyvinuta jen a pouze pro OS Windows a RailWorks. Kompatibilita je zaručena pouze s Windows 7, jakkoli může fungovat i na jiných verzích Windows.

## Instalace:

Aplikaci stačí stáhnout a umístit kamkoli do PC, ale je nutno počítat s faktem, že si v této složce bude vytvářet pomocné soubory.
Proto doporučuji aplikaci umisťovat do zvláštní složky.

## Použití:

1. Aplikaci spustíme dvojitým poklepáním na soubor *MapyCZforTS.exe* (pokud z nějakého důvodu potřebujeme konzoli), jinak na *MapyCZforTS_without_command_line.exe* (nabíhá pouze GUI).
1. Vyčkáme načtení ovladacího okna.
1. Vybereme požadovaný mapový podklad.
1. Nyní je možné zapnout hru, v případě že probíhala změna mapového podkladu, je nutné ověřit, jestli je ve hře nastavený odpovídající zoom. Doporučuji používat vždy maximální možný.
1. Až budeme potřebovat mapový podklad Seznamu, klikneme na tlačítko *Zapni Mapy.cz*
1. Ve chvíli, kdy budeme potřebovat mapové podklady od Googlu, klikneme na tlačítko *Vypni Mapy.cz*. Od toho okamžiku budou veškeré nově načítané snímky opět z mapových podkladů Googlu.
1. Aplikaci ukončíme kliknutím na křížek, nebo Soubor -> Ukonči aplikaci.

## Obecná doporučení
* *Vzhledem k faktu, že RailWorks mapové snímky cachuje do složky "AppData\Local\Microsoft\Windows\Temporary Internet Files\" na WXP/W7 a "AppData\Microsoft\Windows\INetCache\IE" na W8/W10, doporučuji zapnout překladání na Seznamové mapy PŘED načtením editoru, nebo spouštět RailWorks s parametrem -DontUseBlueprintCache - obecně doporučuji pro vývojáře. Parametr má vliv na cacheování snímků - neprobíhá.
Aplikace sice každý start provede vyčištění cache, ovšem pokud již byl editor spuštěný, tyto snímky se nenačtou vůbec*
* *Vzhledem k zápisu do systémových souborů (konkrétně *Windows\System32\Drivers\Etc\hosts*) je bezpodmínečně nutné spouštět aplikaci jako správce. Pokud se tak nestane, aplikace si sama o práva požádá. Pokud by došlo k zamítnutí, aplikace se ukončí.*
* *Vzhledem k výše uvedenému doporučuji přidat aplikaci do vyjímek Antivirového programu. Manipulací se souborem hosts se vyznačují některé viry, které se pokouší přesměrovat uživatele na falešné stránky. Proto aplikaci většina antivirových programů vyhodnocuje jako škodlivou. V aktuální době je pouze na whitelistu Esetu. U ostatních antivirů se čeká na vyřízení žádosti.*
