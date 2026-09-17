# Johans Relay-test

En separat testscen med spelarnamn, Host, Join, joinkod, kopiera-knapp och Lämna. Hosten plus tre klienter kan se varandras namn och rörelser på en enkel testyta.

## Starta två spelare på samma dator

1. Öppna `Assets/Johans Mapp/RelayTest/Relay Test.unity`.
2. Öppna **Window > Play Mode > Scenarios** (eller menyn intill Play).
3. Skapa ett eget scenario, exempelvis `Johans Relay Test`.
4. Aktivera huvudeditorn och välj testscenen som **Initial Scene**. Kontrollera detta särskilt om ett tidigare scenario pekar på paketets Example-scen.
5. Lägg till en instans med **+** under **Additional Editor Instances**. Två spelare räcker för första testet; lägg till totalt tre extra instanser för fyra spelare.
6. Välj scenariot och starta det. Vänta tills båda spelfönstren kör testscenen.
7. Skriv ett namn i första fönstret och klicka **Host**. Vänta tills en kod visas.
8. Skriv ett annat namn i andra fönstret, klistra in koden och klicka **Join**.
9. Flytta med **WASD/piltangenter** i det fokuserade fönstret. Namn och position ska synas hos båda spelarna. Lämna textfältet genom att klicka på en knapp eller i spelvärlden innan du flyttar.
10. Testa **Lämna**, anslut igen med samma kod medan hosten är kvar, och testa att stoppa hosten. När hosten stoppas avslutas matchen; nästa hoststart ger en ny kod.

**Window > Play Mode > Active Scenario** visar instansernas status och låter dig fokusera spelarfönstren. Unity 6.6 stöder även Activate/Deactivate och Keep Active där.

Om scenen saknas: kör **Tools > Johans > Skapa Relay-testscen** efter att skripten kompilerats. Verktyget skapar bara scenen om den saknas och bevarar andra öppna scener. Det är också ett läsbart exempel på hur alla komponenter kopplas ihop.

## Vad har ändrats i Unity?

Projektet använder Unity **6000.6.0f1**, Multiplayer Play Mode **3.0.0**, Netcode for GameObjects **2.13.2** och Multiplayer Services **2.3.2**.

Multiplayer Center hjälper till med paketval och exempel. Netcode for GameObjects synkroniserar spelobjekt. Unity Transport skickar nätverkstrafiken. Authentication och Relay låter instanserna ansluta via en kod. Multiplayer Play Mode startar extra lokala spelarfönster för testning.

I Play Mode 3.0 konfigureras de extra Editor-instanserna genom Scenarios. Det motsvarar användningsfallet för den äldre fliken med extra Virtual Players. Ett scenario är testkonfiguration, inte ett krav i spelets nätverkskod. Det måste heller inte vara just paketets tvåspelarscenario.

Paketets Casual co-op-exempel använder Multiplayer Services Sessions och en CreateOrJoinSessionConnector. Det kan automatisera anslutning. Den här scenen följer i stället ditt gamla projekts manuella Relay-flöde: Host skapar en Relay allocation och visar dess kod; Join ansluter med den koden. Scenariot startar fönstren, och du väljer Host/Join själv.

Exemplets `CreateOrJoinSessionConnector.asset` har dessutom **Max Players = 2**. Antalet startade testfönster och sessionens spelargräns är två olika inställningar. Att starta fyra fönster höjer inte automatiskt den gränsen. Vår separata Relay-testscen reserverar tre klientplatser utöver hosten.

## Internet och Unity Services

Relay-testet behöver internet även när alla spelare kör på samma dator. Ett helt offline-test skulle i stället ansluta Unity Transport direkt till localhost; det är en annan anslutningsväg och använder inga Relay-koder.

Projektet har redan ett Cloud Project ID. Vid tjänstfel: kontrollera rätt projekt/organisation i Unitys Services-inställningar och att Authentication/Relay är tillgängliga i det projektets Unity Dashboard. Kopiera inte gamla projektets Cloud Project ID för att lösa ett fel. Fel från tjänsterna visas i menyn och med detaljer i Console.

Testmenyn använder separata anonyma Authentication-profiler per process när den själv loggar in. Det undviker att flera lokala spelinstanser delar samma testsession. Detta är en testlösning för identiteter, inte permanent spelarkontohantering.

## Testa med build eller en annan dator

Scenarios behövs inte för en vanlig build. Skapa en separat Build Profile där Relay Test-scenen ligger först i scenlistan. Bygg spelet och kör det vid sidan av editorn eller på en annan dator. Klicka Host i ena spelet och Join med samma kod i det andra. Använd samma projektversion/prefabs på båda sidor. Scenen har ingen automatisk scenväxling; alla instanser ska börja i Relay Test.

## Filer och återanvändning

- `Scripts/RelayTestMenu.cs`: UI, anonym inloggning, Relay allocation/join, DTLS, StartHost/StartClient och felhantering. Bygger på flödet i `D:/Unity/MultiPlayerCenter/Assets/_Scripts/RelayManager.cs`, utan beroendet på gamla SceneFlow.
- `Scripts/RelayTestPlayer.cs`: lokal tangentbordsstyrning och ett nätverkssynkroniserat namn.
- `Relay Test Player.prefab`: NetworkObject, NetworkTransform med Owner authority och RelayTestPlayer.
- `Relay Test Network Prefabs.asset`: separat lista för testscenens prefab.
- `Editor/RelayTestSceneBuilder.cs`: skapar scenens kamera, ljus, testyta, NetworkManager, prefab och vanlig redigerbar Canvas-UI.

Spelaren styr själv sin position här, vilket är enkelt för ett rörelsetest. Servervalidering av gameplay, lobby/team-system, host migration och byte till en annan gameplay-scen är nästa steg, inte delar av detta test. Namnet är ett visningsnamn och är inte kontots identitet.

## Fel efter upprepad Play-start

Om Multiplayer Tools visar `RuntimeUpdater.CreateInstance` / `Assertion failed`, eller Analytics ger en förstörd `AnalyticsContainer`, kontrollera **Edit > Project Settings > Editor > Enter Play Mode Settings > When entering Play Mode**. Använd **Reload Domain and Scene** under testningen. Projektet hade tidigare båda omladdningarna avstängda; de är nu aktiverade som en första felsökningsåtgärd. Orsaken är ännu inte bekräftad genom reproduktion.

Stoppa scenariot, spara eget arbete och starta om Unity samt de extra Editor-instanserna så att gamla referenser försvinner. Kontrollera inställningen efter omstart eftersom en redan öppen editor kan ha behållit det gamla värdet. Testa Host/Join, stoppa och upprepa. Relay kräver fortfarande internet.

## Dokumentation

- [Unity: skapa ett Play Mode-scenario](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@3.0/manual/play-mode-scenario/play-mode-scenario-create.html)
- [Unity: extra Editor-instanser](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@3.0/manual/instance-types/main-and-additional-editor-instances.html)
- [Unity: Relay allocation och join](https://docs.unity.com/en-us/mps-sdk/advanced-config/allocating-binding-joining)
