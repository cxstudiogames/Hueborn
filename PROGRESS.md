# PROGRESS - ColorRPG

Jurnal de lucru. Scriu aici ce am facut, ce decizii am luat singur si ce urmeaza.

## Sesiunea 1 - 29.09.2026

### Stare initiala gasita
- Proiectul: `Downloads/My project (3)` (Unity 6000.6.3f1, URP, Input System).
- In `Assets/` existau **doua** versiuni ale jocului: `ColorRPG.cs` (vechi) si `ColorRPG1.cs` (mai nou, cu pets). Ambele declarau aceleasi clase in acelasi namespace, deci **proiectul nu compila**.
  - **Decizie:** am pastrat `ColorRPG1.cs` ca baza (e mai complet) si am golit ambele fisiere (au ramas doar un comentariu). Le poti sterge din Unity oricand.
- Mesajul tau s-a oprit la punctul „4.”, deci de acolo am continuat cu idei proprii (lista de mai jos).

### Ce am facut
1. **Assets CC0 (Kenney)**: 15 pachete descarcate de pe kenney.nl, verificate (License.txt = CC0) si trecute in `CREDITS.md`. In proiect am copiat doar ce folosim (~30 MB) in `Assets/ColorRPG/Resources/`.
2. **Codul, impartit in 17 fisiere** in `Assets/ColorRPG/Scripts/`:
   - `Core/` Inp (input), Art (materiale), ModelLib (incarcare modele), SimpleAnimator (animatii FBX fara Animator Controller), Sfx (sunete + muzica procedurala), Fx (particule, tremurat camera, hit-stop)
   - `Data/` GameData (harti, inamici, pets), Items (echipament si raritati), SaveData (salvare JSON cu migrare automata din salvarea veche)
   - `Gameplay/` Player + PlayerProgress, Enemy, Pet, Loot
   - `World/` WorldBuilder (harti, decor, cladiri)
   - `UI/` UIKit, HUD, InventoryUI
   - `Game.cs` porneste tot automat (nu trebuie pus nimic in scena)
3. **Modele reale in locul primitivelor** (primitivele raman ca rezerva daca un model lipseste):
   - erou animat (12 variante, schimbi eroul de pe cercul albastru din zona de start)
   - inamici noi pe fiecare harta: Slime, Wild Boar, Angry Bee | Sand Crab, Skeleton, Mummy Zombie | Ice Penguin, Frost Ghost, Ice Vampire
   - boss-uri: Orc Warlord, Sand Lion King, Frost Titan (cu coroana si atac „slam” in zona, avertizat de un cerc rosu pe sol)
   - pets animate (24 de animale, pe raritati), copaci, flori, cactusi, palmieri, brazi cu zapada, oameni de zapada, fantana, moara
4. **UI pe Canvas** (am scos OnGUI), cu sprite-urile si fontul Kenney: bare HP/XP cu „ghost”, iconita de nivel, monede, minereu, pets, butoane (Inventar / Auto / Sunet), prompt de interactiune, bara de boss, etichete deasupra cladirilor, numere de damage care sar, notificari de loot.
5. **Inventar si echipament**: 4 sloturi (Arma, Coif, Armura, Inel), 6 raritati (Common, Uncommon, Rare, Epic, Legendary, Mythic) cu culori. Iteme care pica din monstri (cufar cu raza colorata), auto-echipare daca e mai bun, comparatie cu itemul echipat, vanzare, blocare, „Vinde tot ce e Common”, „Echipeaza cele mai bune”.
6. **Forja** imbunatateste acum arma echipata (+1 ... +20), cu costuri pe tier/raritate.
7. **Senzatia de joc („juice”)**: flash alb la lovitura, knockback, squash & stretch, scantei, „poof” colorat la moarte, hit-stop la crit, tremurat de camera, dara luminoasa a sabiei, bloom si culori mai vii (post-processing URP), ceata, lumina ambientala pe 3 tonuri.
8. **Sunete si muzica**: sunete Kenney pentru lovituri, crit, monede, minereu, pasi (iarba/nisip/zapada), UI, forja, level up, boss. Muzica de fundal generata din cod (cate o melodie pe harta).
9. **Idei proprii adaugate**: auto-atac (R), sprint (Shift), zoom (rotita), pets care ataca singuri, cadou la fiecare 5 minute de joc, magazin (vinde iteme), rebirth care pastreaza itemele (+25% DMG si +10% monede, cerinta de nivel creste), inamici care se plimba si se intorc acasa, low-HP vignette, suport gamepad.

### Decizii luate singur
- **Kenney in loc de Quaternius**: quaternius.com nu era accesibil. Kenney are pachete CC0 animate care se potrivesc stilului colorat.
- **Modelele se incarca din `Resources/`** si se normalizeaza automat (inaltime si pozitie pe sol), ca jocul sa mearga din orice scena, fara prefab-uri facute manual.
- **Animatiile ruleaza prin Playables**, direct din clipurile FBX, fara Animator Controller.
- **Salvarea e acum JSON** (`crpg_save_v2`). Salvarea veche se converteste automat: sabia ta +N devine o arma cu raritatea potrivita.
- **Rebirth pastreaza itemele**, ca in jocurile Roblox de loot. Altfel ar fi fost prea frustrant.
- **Muzica e generata din cod**: nu am gasit bucle muzicale CC0 potrivite in pachetele accesibile, iar asa nu exista nicio problema de licenta.

### Verificat in Unity (30.09.2026)
- Codul **compileaza fara erori** cu DLL-urile exacte din Unity 6000.6.3f1 (verificat cu compilatorul C# Roslyn).
- Am deschis proiectul in Unity si am apasat Play: **0 erori, 0 warning-uri in Console**. Jocul porneste: HUD-ul pe Canvas, harta Meadow Fields cu modele Kenney, eroul animat, slime-uri, altarul de oua si butoanele merg.
- Dupa primul test, sabia arata prea mare (ca o placa gri langa erou). Am micsorat-o si am inclinat-o in sus (`Player.BuildSword`). Ramane de verificat vizual.
- In timpul testului, Unity s-a inchis (nu am gasit eroare in log). La urmatoarea deschidere verifica daca apare vreo eroare in Console.
- Daca eroii sau inamicii apar cu spatele: in `Scripts/Core/ModelLib.cs` schimba `YawFix = 0f` in `YawFix = 180f`.


## Sesiunea 2 - 30.09.2026: TRADING cu booth-uri + grafica

### Trading (cerut de tine)
- **TRADE PLAZA** - o zona noua (piata), cu portal auriu in partea de jos a zonei de start de pe fiecare harta.
- **Booth-ul tau** (taraba aurie): il revendici cu E, pui pana la 6 iteme la vanzare si alegi pretul (-10%, +10% sau pretul corect). Cumparatorii NPC le cumpara in timp: cu cat e mai ieftin, cu atat se vinde mai repede. **Vanzarile merg si cat esti offline (pana la 24h)**, iar banii ii colectezi de la booth.
- **Oferte de la cumparatori**: din cand in cand un NPC iti face o oferta pentru un item de pe taraba (ACCEPTA / REFUZA, 25 de secunde).
- **6 booth-uri NPC** cu negustori animati:
  - Blade Bazaar (arme), Helm & Hood (coifuri), Armor Alley (armuri), Ring Realm (inele) - magazine cu 4 iteme fiecare, stoc nou la 8 minute (Epic+ costa si minereu)
  - Trade-Up Tent - dai 3 iteme cu aceeasi raritate si primesti 1 item cu raritate mai mare
  - Ore Exchange - cumperi / vinzi minereu
- 12 cumparatori NPC se plimba prin piata si vin la booth-ul tau cand vinzi ceva.
- Salvarea include booth-ul, castigurile si stocul magazinelor (compatibila cu salvarea veche).

### Grafica
- Texturi procedurale pentru sol: iarba, nisip cu dune, zapada care sclipeste, pavaj din piatra in piata.
- Particule ambientale: polen si licurici (pajiste), praf purtat de vant (desert), ninsoare (munte), confetti (piata).
- Umbre de nori care trec peste harta.
- Copacii se leagana in vant.
- Minimap rotund in stanga-jos, cu sageata pentru directie.
- Inamicii apar cu un „pop” si un nor de fum; boss-urile apar cu un stalp de lumina.
- Stalp de lumina la level up si cand pica un item Legendary sau Mythic.
- Muzica noua pentru piata.

### Testat in Unity (30.09)
- 0 erori in Console. Portalul auriu duce in Trade Plaza. Magazinul Ring Realm merge (am cumparat un inel, s-a echipat automat). Booth-ul tau se deschide cu E.
- Booth-urile sunt construite din cod, in stil Roblox: tejghea, copertina in dungi, panou cu numele si o lumina calda. Modelul Kenney de taraba arata ca o masa simpla, asa ca nu il mai folosim.

### Fisiere noi
`Data/TradeData.cs`, `Gameplay/Trading.cs`, `World/PlazaBuilder.cs`, `UI/TradeUI.cs`, `UI/Minimap.cs`, `Core/Ambient.cs`

### Urmatoarele idei (backlog)
- [ ] Minimap in coltul ecranului
- [ ] Quest-uri zilnice („omoara 50 de slime”) cu recompense
- [ ] A 4-a harta (Lava / Vulcan) si un boss final
- [ ] Abilitati active (dash, lovitura circulara) cu cooldown
- [ ] Inventar de pets (echipezi 3 din N) + contopire pets (3 la fel = unul mai rar)
- [x] Recompense offline („24h”): booth-ul vinde cat esti plecat
- [ ] Ofertele de trade sa includa si schimb item-pe-item
- [ ] Clasament cu cei mai bogati negustori din piata
- [x] Setari (volum, calitate) - sesiunea 3
- [ ] Shader-e si materiale ca asset-uri (pentru build-uri in afara editorului)


## Sesiunea 3 - HUD nou, clase, skill points, setari, jocul in engleza

### Ce s-a facut
- **Jocul este in engleza**: toate textele din UI, mesajele, iteme, magazinele si portalurile.
- **Trade Plaza**: butonul rosu X (LEAVE) sus-centru te duce inapoi in Meadow Fields. Portalul din piata duce tot in prima harta.
- **Teleport Corner** in Meadow Fields (sud-est de zona de start): platforma de piatra cu lanterne si portalurile spre Desert, Frozen si Trade Plaza, puse unul langa altul. Celelalte harti au un singur portal, "Back to Meadow Fields". Moara de vant s-a mutat in partea de vest.
- **Minimap**: mai simplu (doar puncte colorate) si 50% transparent.
- **Portretul caracterului** (stanga-sus) este randat live. Click pe el deschide fereastra CHARACTER: schimbi numele (2-16 caractere), eroul (< >) si clasa de awakening.
- **Awakening** (de la nivelul 20): Warrior (W), Assassin (A), Guardian (G), Berserker (B). Fiecare clasa are bonusuri proprii. Schimbarea clasei costa 2500 de monede.
- **Nume deasupra jucatorului**, ex. `(A) 32 Ionutcx`: tag-ul in culoarea clasei, nivelul intr-un gri-albastru discret, numele alb cu contur negru.
- **Skill points**: 3 puncte la fiecare level up. Le pui in HEALTH (+12 HP), DAMAGE (+3%) sau DEFENSE din butonul STATS (stanga). Un badge rosu arata punctele libere. RESET POINTS le da inapoi. Rebirth reseteaza punctele.
- **Barele HP si XP** sunt jos, pe centru.
- **Setari** (rotita din coltul dreapta-sus): volum muzica/ambient, volum efecte, FPS (30/60/120/MAX), densitatea particulelor, preset de calitate (LOW/MEDIUM/HIGH), umbre, post-processing, umbre de nori, numere de damage. Se salveaza in PlayerPrefs.
- **Sabia**: pozitia mainii se calculeaza din poza T a modelului (capatul bratului drept). Sabia e proportionala cu eroul (0.38 x inaltimea), cu lama inainte, si urmeaza bratul in animatii. Loviturile folosesc doar animatia cu mana dreapta, ca sa se potriveasca cu mana care tine sabia.

### Decizii
- Butonul de mute a fost scos din HUD, pentru ca volumul e acum in Settings. Tasta M inca da mute.
- Linia de ajutor cu controalele a fost scoasa de pe ecran. Controalele sunt in Settings > CONTROLS.

### Fisiere noi
`Core/Settings.cs`, `UI/Portrait.cs`, `UI/Windows.cs` (CharacterUI, StatsUI, SettingsUI)

### De verificat la urmatorul test in Unity
- Pozitia sabiei in mana (depinde de poza T din FBX).
- Incadrarea portretului (uvRect in HUD).

## Sesiunea 3b - font clasic si camera third person
- **Font**: textele folosesc acum *TeX Gyre Pagella Bold*, un font clasic cu serife (in stilul Palatino). Textele sunt marite cu 15% (`UIKit.FontScale`), pentru ca fontul are literele mai mici decat Kenney Future.
  - Decizie: nu am putut descarca Cinzel sau Lora (reteaua era blocata in sesiune). Pagella are o licenta gratuita (GUST) si nu e CC0; e trecut in CREDITS.md.
- **Camera third person**: camera sta in spatele eroului si se roteste cu CLICK DREAPTA tinut apasat + mouse (sau Z / C, sau stick-ul drept de pe gamepad). Rotita mouse-ului face zoom.
  - WASD misca eroul relativ la camera.
  - Cand mergi inainte, camera revine singura in spatele eroului.
  - Camera nu intra in copaci sau pereti (sphere cast).
  - Sensibilitatea camerei se regleaza in Settings > GRAPHICS.
- Minimap-ul se roteste odata cu camera (sus = directia in care privesti). Litera N arata nordul.
- Ceata si fundalul au aceeasi culoare, ca orizontul sa arate natural. Far clip = 158, ca sa nu se vada hartile vecine.

## Sesiunea 4 - lumea in Hierarchy + portret static
- **Portretul din coltul stanga-sus** e acum o poza statica cu eroul (se reface doar cand schimbi eroul). In fereastra CHARACTER portretul ramane animat. Camera portretului randeaza doar cat e deschisa fereastra (mai putin consum).
- **Toata lumea e in scena (Hierarchy)**, organizata in foldere:
  - `=== GAME ===` - componenta Game (porneste jocul).
  - `=== ENVIRONMENT ===` - camera, soarele, volumul de post-processing.
  - `=== WORLD ===` - cele 3 harti si Trade Plaza. Fiecare harta are folderele Terrain, Start Area, Teleport Corner/Portals, Boss Arena si Decor. Piata are Terrain, Booths, Decor, Shoppers (NPC) si ReturnPortal.
  - `=== MY OBJECTS (kept on rebuild) ===` - pune aici obiectele tale; reconstruirea nu le sterge niciodata.
  - La Play apare si `=== RUNTIME (Play only) ===`, cu Player, Enemies, Loot, Pets si Systems (HUD, sunet, efecte). Acestea exista doar cat ruleaza jocul.
- Poti muta, sterge sau adauga obiecte din `=== WORLD ===`. Jocul foloseste lumea din scena daca exista (altfel o construieste din cod).
  - Portalurile, forja, booth-urile etc. au componenta **Interactable**: alegi din Inspector ce fac (Kind / Target).
  - Textele plutitoare au componenta **WorldLabel**: text, culoare, marime.
- Materialele si texturile generate sunt salvate in `Assets/ColorRPG/Generated`, deci le poti edita.
- **Update-uri**: meniul `ColorRPG > Rebuild World In Scene` reconstruieste lumea. Cand jocul primeste un update al hartilor, cresc `GameData.WorldVersion`, iar editorul reconstruieste singur lumea (optiunea `ColorRPG > Auto-update World On Game Updates`).
  - Inainte de reconstruire, scena veche e copiata in `Assets/ColorRPG/Backups`.
- Tehnic: fiecare componenta salvata in scena are acum fisierul ei (numele fisierului = numele clasei): Interactable, WorldLabel, WorldRoot, Sway, Spinner, FireFx, PortalFx, NpcTrader, BoothDisplay, Shopper.
  - Unity nu gasea scripturile claselor aflate in fisiere cu alt nume ("references runtime script... Fixing!"). A fost nevoie de un restart al editorului ca sa se goleasca cache-ul.
- Testat in Unity: 0 erori, 0 warning-uri. Lumea v4 e in scena, interactiunile ([E]) merg, portretul e static in HUD si animat in fereastra.
- Backup-urile v1-v3 din `Assets/ColorRPG/Backups` sunt din timpul acestei reparatii (au componentele stricate) si pot fi sterse.

## Sesiunea 5 - chenar cu statistici, selectie multipla, curatenie
- **Statisticile nu mai sunt scrise sub iteme.** Tii mouse-ul ~1 secunda pe un item si apare un chenar (ItemTooltip) cu:
  - numele si raritatea;
  - toate statisticile;
  - calitatea (roll), nivelul de forja, harta unde a picat, valoarea de vanzare;
  - comparatia cu itemul echipat si daca e blocat.
  Chenarul merge in inventar (grila si itemele echipate) si in Trade Plaza (booth, magazine, trade-up).
- **Vanzare cu selectie multipla** in inventar:
  - click pe iteme = le bifezi (bifa verde);
  - SELECT ALL / DESELECT ALL;
  - SELECT JUNK (Common + Uncommon);
  - SELL n (+valoare) vinde tot ce e bifat, fara iteme blocate sau echipate;
  - daca printre ele e un item Epic sau mai bun, trebuie confirmat cu al doilea click;
  - EQUIP (cand e bifat un singur item), LOCK/UNLOCK pe toate cele bifate;
  - click pe un item echipat -> UNEQUIP.
- **Optimizare:**
  - la construirea lumii, obiectele care nu se misca devin *Static* (static batching = mai putine draw calls);
  - materialele generate au GPU instancing;
  - materialele generate nefolosite se sterg automat.
- **Curatenie:** meniul `ColorRPG > Clean Up Unused Assets` muta in Recycle Bin (se pot recupera):
  - modelele, iconitele si sunetele care nu sunt folosite nicaieri in cod (tot ce e in Resources intra in build);
  - fontul vechi Kenney Future;
  - template-ul Unity (TutorialInfo, Readme);
  - fisierele goale vechi (ColorRPG.cs, ColorRPG1.cs) si backup-urile stricate.

## Sesiunea 6 - teleport, pass-uri, pets pe harti, seturi, servere in piata, harta 4 (Lava Volcano), joc de ~24h
- **Portretul** din stanga-sus arata doar fata avatarului (fara rama galbena). La click se deschide in continuare fereastra CHARACTER, neschimbata.
- **TELEPORT** (buton dreapta): alegi orice harta deblocata sau Trade Plaza. Merge doar cu **Teleport Pass**. Fara pass, folosesti portalurile din Teleport Corner.
- **GENERAL SHOP** (buton stanga, SHOP): coins, shards si pass-uri (Teleport Pass, Game Pass).
  - Decizie (a ta): deocamdata e **mod de TEST**, butoanele dau gratis. Plata reala o legam mai tarziu.
  - "reset passes (test)" scoate pass-urile ca sa poti testa din nou.
- **Shards** (moneda noua, pill albastru sus-dreapta): +1..4 de la fiecare boss (dupa harta), +3 la fiecare 5 niveluri, +1 din cadouri. Forja de la +10 in sus cere si shards.
- **Pets:** fiecare harta are oul ei, cu 6 pets (cate unul pe raritate). Pets de pe hartile grele sunt mult mai puternici (x1 / x2.2 / x5 / x11).
  - Echipati: **2** (sau **4 cu Game Pass**). Colectie de maxim 30; restul stau in storage.
  - Fereastra PETS: echipezi, dezechipezi, EQUIP BEST, RELEASE (cu confirmare).
  - Oul costa 2.5K / 40K / 600K / 6M coins (dupa harta) si se scumpeste cu 35% la fiecare ou deschis pe harta respectiva.
  - Pets din salvarea veche au devenit pets de Meadow (primii 2 echipati).
- **Seturi de iteme:** 3 seturi pe fiecare harta (12 in total). Fiecare set are 4 piese (arma, coif, armura, inel), iar bonusurile pentru 2 si 4 piese cresc cu harta:
  - Leafguard / Bramble / Sunpetal (Meadow)
  - Dune Stalker / Scarab / Mirage (Desert)
  - Frostbite / Glacier Ward / Aurora (Frozen)
  - Inferno / Obsidian / Phoenix (Lava)
  - Tipuri de bonus: damage; HP + defense; crit + coins.
  - Sansa de piesa de set: 20% din drop-urile normale, 60% din drop-urile de boss. Bonusurile apar in chenarul de hover si in STATS > ITEM SETS.
- **Itemele pica doar de pe harti.** NPC-urile nu mai vand iteme. Trade-Up si Ore Exchange au fost scoase (decizia ta).
- **Trade Plaza pe servere:**
  - fiecare server are pana la 18 jucatori si **10 booth-uri** asezate in cerc in jurul fantanii;
  - ceilalti jucatori (simulati) ocupa booth-uri si vand iteme din drop-uri, pe care le poti cumpara;
  - booth-urile lor se schimba la 15 minute;
  - revendici (E) un booth liber; daca toate sunt ocupate, apesi **CHANGE SERVER** (sus);
  - daca schimbi serverul, pierzi booth-ul, dar itemele raman la tine (confirmare cu al doilea click);
  - itemele tale se vand doar cat ai un booth.
- **Harta 4 - Lava Volcano** (nivel 60): 2 vulcani, rauri de lava, cristale de obsidian, stanci si focuri. Monstri: Magma Slime, Ember Fox, Ash Walker; boss Molten Mammoth. Are melodie si particule proprii.
- **Ritm pentru ~24h de joc:**
  - XP pe nivel = 20 * L^2.3 + 30.
  - Hartile se deblocheaza la nivelurile 1 / 15 / 35 / 60.
  - Estimare (~1100 kills pe ora): Meadow ~2h, Desert ~5.5h, Frozen ~8h, Lava ~8.5h pana la nivelul 80.
  - Forja se scumpeste mult mai repede (plus^2), iar pets sunt scumpi.
  - Rebirth cere nivelul 50 (+10 la fiecare rebirth).
- Tehnic:
  - `WorldVersion = 6`, deci lumea din scena se reconstruieste singura.
  - Componenta noua `BoothSlot` (cine ocupa booth-ul pe serverul curent).
  - Fisiere noi: `UI/GameWindows.cs` (Teleport / Pets / Shop).
  - `BoothDisplay.cs` nu mai e folosit si se sterge cu Clean Up.

## Sesiunea 7 - Polish grafic (mai realist 3D), arme/armuri diferite, panou ADMIN
- **Nimic descarcat** - totul e facut din cod (shadere + modele procedurale), deci proiectul ramane mic si fara licente externe.
- **Iarba 3D reala:** shader nou `ColorRPG/Grass` (fire curbate, se misca in vant, umbre, ceata) + componenta `GrassField` (mii de smocuri desenate cu GPU instancing, doar in jurul camerei). Iarba pe toate hartile (verde / uscata in desert / cu brum pe Frozen) si in Trade Plaza. Se poate opri din Settings > **3D GRASS**.
- **Cer nou:** shader `ColorRPG/Sky` - gradient, soare, nori animati; culori diferite pe fiecare harta (desert cald, frozen rece, lava rosiatic, piata). Salvat ca `Generated/Materials/Sky.mat`.
- **Lumina si post-procesare:** soare mai puternic, umbre pe distanta mai mare (40/65/95 m dupa calitate), ACES tonemapping, contrast/saturatie/white balance, bloom fin, MSAA 4x pe High.
- **Arme diferite:** 6 tipuri - Sword, Axe, Hammer, Spear, Saber, Greatsword - fiecare cu model 3D propriu si statistici proprii (damage / viteza / raza de atac; ex. Hammer +30% dmg, -20% viteza, +0.2 m raza; Spear +1.4 m raza). Tipul apare in chenarul de hover si are iconita proprie in inventar.
- **Coifuri si armuri vizibile pe erou:** 4 modele de coif (Helm / Cap / Hood / Crown) si 4 de armura (Plate / Mail / Vest / Tunic). Metalul depinde de harta (fier, bronz, argint-albastru, obsidian), piesele de set au culoarea setului, iar cele Epic+ sau +10 stralucesc. Piesele de set au un romb colorat in inventar.
- **Panou ADMIN (teste):** buton rosu ADMIN sub rotita + tasta **F1**. Apare doar in Editor si in Development Build (nu si in build-ul final). Contine:
  - coins / shards / ore: adaugi sau setezi o suma; reset la 0;
  - nivel: set / +1 / +10 / fill XP / unlock all maps / +1 rebirth;
  - iteme: alegi harta, raritatea, slotul, setul si forja; GIVE ITEM, GIVE FULL SET, GIVE PET, CLEAR INVENTORY; AUTO-EQUIP (itemul primit se echipeaza direct);
  - trucuri: god mode, viteza x3, heal, kill nearby, reset puncte, pass-uri ON/OFF, gift ready, RESET SAVE (dublu click).
- Tehnic: `WorldVersion = 7`; fisiere noi `Shaders/Grass.shader`, `Shaders/Sky.shader`, `World/GrassField.cs`, `Gameplay/GearVisuals.cs`, `UI/AdminUI.cs`.
- Fisierul `uidebug.txt` din radacina proiectului (test de input) se poate sterge.

## Sesiunea 8 - Tot jocul mai realist 3D, UI 3D, sunete noi per harta / boss, fix pets
- **Shader nou pentru toate obiectele si solul: `ColorRPG/Surface`**:
  - lumina PBR URP (soare, umbre, lumini aditionale, SSAO, ceata);
  - relief si variatie de culoare procedurale, in spatiul lumii (fara texturi externe);
  - recunoaste tipul suprafetei dupa culoare: frunzis, lemn, piatra/metal, zapada (cu sclipiri);
  - umbrire de contact langa sol si lumina de margine;
  - lava (baltile/craterele foarte stralucitoare) curge si are crusta care se misca.
- Toate modelele Kenney (copaci, case, stanci, monstri, pets, erou) trec automat pe noul shader (`Art.Realistic` in `ModelLib.Spawn`). Primitivele si solul il folosesc si ele.
- SSAO mai puternic in `Assets/Settings/PC_Renderer.asset` (Intensity 0.85, Radius 0.55).
- `WorldVersion = 8`, deci lumea din scena se reconstruieste singura cu noile materiale.
- **UI 3D:** butoanele (`btn_`, `sq_`, `round_`) si panourile (`flat_`) regenerate: gradient, luciu, bevel si "buza" 3D.
- **Pets:** am scos discul luminos (alb) de sub pets rari - nu mai apare la niciun animal.
- **Sunete noi (sintetizate din cod, fara licente; site-urile de sunete erau blocate din sesiune):**
  - muzica noua pe fiecare harta + piata (`Resources/Audio/Music/Map0..3`, `Plaza`):
    - Meadow: chitara + flaut;
    - Desert: oud + tobe arabe;
    - Frozen: celesta + cor + clopote;
    - Lava: tobe taiko + corzi + alamuri;
    - Plaza: marimba vesela;
  - **tema de boss** per harta (`Boss0..3`): porneste cand esti la mai putin de 24 m de un boss si revine dupa lupta;
  - **ambient in bucla** per harta (`Resources/Audio/Ambient`):
    - Meadow: vant, frunze, pasari, parau;
    - Desert: rafale, nisip, soim;
    - Frozen: vant care urla, gheata;
    - Lava: bubuit, lava care bolboroseste, foc, eruptii;
    - Plaza: multime, fantana, clopotei;
  - **boss:** raget propriu la inceputul luptei (Orc Warlord, Sand Lion King, Frost Titan, Molten Mammoth), plus BossSpawn, BossSlam, BossDie;
  - **FX:**
    - swing diferit pe tip de arma (Light / Heavy / Thrust);
    - lovitura taietura / izbitura + textura hartii (slime, nisip, gheata, lava);
    - moarte diferita pe harta;
    - CritHit;
    - pasi pe piatra (Lava + Plaza).

## Sesiunea 9 - Questuri, potiuni de boost, tokens, materiale de forja, HUD nou, copaci realisti
- **Bug pets:** animalele nu mai topaie / nu se mai rotesc haotic. Viteza e netezita, trec intre mers si stat pe loc cu histerezis, iar pasul e legat de distanta parcursa.
- **Copaci si natura realiste (procedurale, `World/ProcNature.cs` + shader `ColorRPG/Foliage`):**
  - stejari, mesteceni, copaci de toamna, brazi, brazi cu zapada, palmieri, copaci uscati (vulcan, desert), tufe, cactusi cu nervuri;
  - stanci deformate cu fete plate; pe Frozen au caciula de zapada;
  - frunzisul are lumina care trece prin frunze, se misca in vant si are umbre;
  - pe Meadow cad frunze din copaci.
  - `WorldVersion = 9`; editorul salveaza plasele in `Generated/Meshes`.
- **Questuri:**
  - la spawn-ul fiecarei harti e un NPC cu "!" auriu: Elder Rowan, Nomad Zahir, Sage Frida, Warden Ignis;
  - are 3 questuri: 2 cu monstri (15 / 12 kills) + 1 cu boss-ul (5 kills);
  - alegi ce iei (maxim 3 active); se termina automat si sunt repetabile;
  - tracker sus-dreapta cu TRACK ON/OFF pe fiecare quest si HIDE / QUESTS;
  - sageata aurie langa erou arata spre cea mai apropiata tinta, iar un romb pluteste deasupra ei.
- **Potiuni de boost:** XP x2, Damage x1.5, Coins x1.5, fiecare 5 minute (se aduna pana la 60 de minute).
  - Le primesti din questuri sau le cumperi in piata.
  - Le folosesti din bara de langa HP sau cu tastele 1 / 2 / 3.
  - Timpul ramas apare sub avatar.
- **Forja:**
  - cere si **Forge Stones** (de la +2): pica de la monstri (14%) si boss (garantat), plus din questuri;
  - de la +5, o forja esuata scade nivelul cu 1;
  - **Guardian Cloak** (rar: boss 10%, questul de boss, piata) te protejeaza si se consuma automat.
- **Tokens:**
  - moneda noua; singura folosita in Trade Plaza;
  - in piata se vand si se cumpara iteme, **pets** si **materiale** (pietre in pachete de 10, cloak, potiuni);
  - SHOP > TOKENS: 100 / 550 / 1200 ($0.99 / $4.99 / $9.99) - deocamdata butoane de TEST, fara plati reale;
  - castigurile vechi din booth (in coins) au fost date direct, iar preturile au fost recalculate in tokens.
- **HUD nou:**
  - sus-stanga ramane doar avatarul (click = CHARACTER);
  - banii si materialele sunt in dreapta-jos, cu iconita in coltul din dreapta si numarul in stanga ei: Coins, Tokens, Shards, Ore, Stones (+cloaks), Pets;
  - notificarile au fost mutate langa ele.
- **ADMIN:** tokens, pietre, cloaks, potiuni, CLEAR BUFFS.

## Sesiunea 10 - UI stil "simulator", POWER, forja noua, inventar nou, hub organizat
- **Tema UI**: ferestre gri-inchis cu dungi, titlu in eticheta deasupra, X rosu, font Poppins Bold (OFL), butoane colorate in carouri. Click in afara ferestrei = inchide.
- **HUD**: avatar static fara chenar (stanga-sus), jos-dreapta doar Coins / Shards / Tokens (fara chenare). Dreapta: TRADING (servere: Join, Join Random Hub, Private Server - "cand e online"), TELEPORT, INVENTORY, AUTO SWING.
- **POWER**: `Player.Power` (damage, HP, defense, crit, nivel; fara potiuni). Deasupra jucatorului: titlu dupa putere (NEWBIE ... GODLIKE), `[clasa] nume Lv`, flacara + power. Power recomandat per harta in TELEPORT si la portaluri (`GameData.RecPower`).
- **Hub (harta 1)**: forja + rebirth in stanga, 4 oua (blocate pana la nivelul hartii) + 2 clasamente (TOP POWER / TOP TIME PLAYED, `World/Leaderboard.cs`) in dreapta, NPC-ul de questuri la inceputul pavelelor pe fiecare harta. Shop-ul mic sters de pe toate hartile. Desert si Lava au doar forja lor. WorldVersion 11.
- **Forja (ForgeUI)**: alegi orice item din grila; +N -> +N+1, sansa (100, 80, 70, 62, 55, 48 ... 15%), cost, PROTECT cu Guardian Cloak (1, sau 2 de la +10), FORGE si MAX FORGE. Esec fara protectie = -1 nivel. Meadow 0->5, Desert 5->10, Lava 10->15 (max 15).
- **Inventar nou**: avatar 3D cu echipamentul pe el (copie a modelului din joc in studio-ul Portrait), 4 sloturi, POWER, statistici, materiale; grila 8x5, EQUIP BEST / EQUIP / FAVORITE (stea, nu se vinde) / SELL / SELECT JUNK.
- **Awakening progresiv**: max 3. I (Lv 20) alegi clasa - definitiv; II (Lv 45, 150K + 25 shards) si III (Lv 70, 3M + 100 shards) o fac mai puternica (x1.8 / x2.8 bonusuri). Warrior>Warlord>Titan, Assassin>Shadowblade>Phantom, Guardian>Paladin>Bastion, Berserker>Ravager>Bloodlord.
- **Lupta**: un click = o lovitura (spam = atac mai rapid, minim 45% din cooldown), fara combo. SPACE = saritura animata (crouch, jump/fall, squash & stretch).
- **Loot**: monedele se strang singure; minereul si itemele cu [E] - automat doar cu MAGNET PASS.
- **Shop nou**: carduri mari colorate pentru pass-uri (MAGNET, AUTO SWING, TELEPORT, +2 PET SLOTS) cu OWNED; tokens, coins, shards (TEST).
- **Trading**: materialele se vand "split" - alegi cantitatea si pretul in tokens.
- **Oua**: animatie pe tot ecranul (oul se clatina, crapa, blitz, coaja zboara, apare pet-ul cu raritatea).

## Sesiunea 11 - chat, prieteni, server privat, hub simetric
- **Chat** (stanga-sus, langa avatar): tab-uri GLOBAL / SERVER, ENTER sau "/" = scrii, ENTER = trimiti, buton "-" = minimizeaza. Ceilalti jucatori sunt simulati pana cand jocul e online (`UI/ChatUI.cs`). Boost-urile active stau acum pe un rand sub chat.
- **Prieteni**: tab-ul FRIENDS din TRADING - adaugi pana la 100 de prieteni, ii stergi, ii inviti.
- **PRIVATE SERVER pass** (SHOP): serverul tau din Trade Plaza (`TradeSystem.PrivateId`), inviti pana la 17 prieteni (18 jucatori pe server).
- Chenarul cu serverul / jucatorii de sus a fost scos (detaliile sunt in fereastra TRADING).
- **Atac**: un click = exact o lovitura (fara coada de click-uri); cu AUTO SWING pornit, auto-ul asteapta 1s dupa ultimul tau click. Butonul AUTO e verde cand e pornit.
- Forja: chenarul PROTECT mutat mai sus, textul nu mai atinge butoanele.
- **Hub simetric**: dreapta = platforma TELEPORT (portalurile toate pe piatra), stanga = platforma LEADERBOARDS, ambele cu fata spre spawn. Forja + Rebirth pe x=-17, cele 4 oua pe x=+17, fantana in centru, felinare simetrice, cadoul in spate, semn in oglinda cu NPC-ul. Cortul / focul / pietrele de langa spawn si CHANGE HERO au fost scoase (eroul se schimba din fereastra avatarului). Pe hartile de farm: forja in stanga, portalul inapoi in dreapta. WorldVersion 13.

## Sesiunea 12 - titluri, chat sigur, hub curat, forja noua
- **TITLURI** (`Data/TitleData.cs`, `UI/TitlesUI.cs`): 64 de titluri in 13 categorii (POWER, TOP, LEVEL, CLASS, REBIRTH, FORGE, PETS, QUESTS, HUNTER, BOSS, TIME, TRADER, WEALTH). Se deblocheaza singure (verificare la 2s, toast "NEW TITLE UNLOCKED"), raman deblocate. In INVENTORY click pe titlu -> fereastra TITLES (blocate cu lacat + cum le deblochezi, EQUIP, AUTO = titlul dupa putere). Titlul ales apare deasupra jucatorului.
- **Chat**: se strange singur dupa 6s fara folosire (click pe GLOBAL/SERVER il deschide). `ChatFilter`: 2s intre mesaje, max 4 in 15s (altfel pauza 10s), fara mesaje repetate, fara caractere spam, fara link-uri, injuraturi/insulte in ~12 limbi blocate (prinde si l33t, litere despartite, litere repetate).
- **Bug 65000 vertexuri** (chenarul gol al itemelor): textele aveau 2 contururi + umbra (25x vertexuri). Acum 1 contur + umbra peste tot; statisticile itemelor apar dupa 0.6s.
- Pick up: doar "[E] Pick up X". Numele hartii nu mai sta langa minimapa - la intrarea pe orice harta apare 5s "WELCOME TO ...".
- Shop: titluri pe un rand, descrieri in caseta lor (`UIKit.Fit` = textul se micsoreaza ca sa incapa).
- **Hub**: cerc de spawn mai mic (r 21) cu bordura, inel si rozeta; 8 felinare pe margine; forja noua (platforma, cuptor cu lava si cos, brazere, rafturi, jgheab de lava, lumina) in stanga, oul Meadow in dreapta. Fiecare ou e acum pe harta lui. REBIRTH are altarul lui langa arena boss-ului (platforma, 6 coloane, drum separat cu alee de felinare). Moara de vant mutata departe de clasamente. WorldVersion 14.

## Sesiunea 13 - alei spre ou / forja, fara iarba pe pavele, NPC pe iarba
- `Paver()` = pavela + gaura in iarba sub ea (toate drumurile: principal, alei, platforme) - iarba nu mai creste peste pavele.
- `BuildAlley()` = alee cu pavele si felinare pereche; `BuildSpotPad()` = platforma rotunda cu bordura.
- Pe FIECARE harta: OUL hartii pe platforma lui in dreapta-fata a cercului de spawn, cu alee roz; FORJA (hub, Desert, Lava) in stanga-fata, cu alee portocalie, intoarsa spre spawn. Altarul REBIRTH foloseste aceeasi alee.
- Drumul principal porneste de la marginea cercului de spawn; NPC-ul cu questuri sta pe iarba, langa inceputul drumului, cu zona libera in jur (Block 4m). WorldVersion 15.

## Sesiunea 14 - mici retusuri
- NPC-ul cu questuri: cerc de pamant + fara iarba (r 3.3) si fara obiecte (r 6) in jur, pe fiecare harta.
- Hartile 2-4: portalul spre hub are platforma lui la dreapta cercului de spawn si alee de acces cu felinare. WorldVersion 16.
- "WELCOME TO ..." apare la 1 secunda dupa intrarea pe harta. Chat-ul porneste strans si se deschide doar cand vrei.

## Sesiunea 15 - mai putine felinare, fantani, GUIDE, texte simple
- Felinare: scoase de pe cercul de spawn si de pe platformele TELEPORT / LEADERBOARDS; aleile au doar o pereche la capat. Scoase cuburile/"cristalele" fara rost din Frozen si Lava.
- Fantana din Trade Plaza: coliziune solida (nu mai urci pe ea), jucatorii simulati o ocolesc, apa curge (`World/FountainFx.cs`: jet + stropi). Fantana din hub are si ea coliziune si apa.
- Mesajul la intrarea pe harta: doar numele hartii.
- **GUIDE** (butonul "?" langa SETTINGS, `UI/GuideUI.cs`): ghidul jocului pe sectiuni (Basics, Power & Titles, Loot & Magnet, Quests, Potions, Forge, Pets & Eggs, Awakening, Rebirth, Trading, Passes, Chat). Textele lungi din Teleport, Shop, Forge, Inventory, Quests, Trading, Pets au fost scoase (explicatiile sunt in GUIDE). WorldVersion 21.

## Sesiunea 16 - skill-uri pentru eroi + iconite noi
- **Skill-uri:** fiecare din cei 12 eroi are o clasa si 3 skill-uri proprii (Data/SkillData.cs), pe tastele **Z / X / C** sau prin click pe bara de skill-uri de langa HP (stanga). Bara arata cooldown-ul radial si secundele ramase; la hover apare descrierea skill-ului.
  - Knight, Ranger, Pyromancer, Frost Mage, Storm Caller, Assassin, Berserker, Priestess, Warlock, Monk, Paladin, Sorceress.
  - 21 de skill-uri cu animatii si efecte proprii (Gameplay/PlayerSkills.cs + Gameplay/SkillFx.cs): Whirlwind (rotire), Dash, Ground/Leap Slam (saritura + unda de soc), Fireball/Ice Shards (proiectile), Meteor (cade din cer), Nova/Flame Ring/Thunder Clap (unde), Arrow Rain/Blizzard/Poison (zone), Chain Lightning/Life Drain (fulgere), Shield Wall/Battle Cry/Shadow Fury (aure), Heal/Holy Nova.
  - Inamicii pot fi incetiniti (Slow) si ametiti (Stun; boss-ii mai putin). Skill-urile nu merg in Trade Plaza.
  - Battle Cry si Shadow Fury nu modifica POWER-ul.
- CHARACTER: clasa eroului si cele 3 skill-uri ale lui (cu descriere la hover). GUIDE: sectiunea noua HEROES & SKILLS.
- **Iconite:** toate iconitele jocului sunt acum desene color (Noto Color Emoji, licenta Apache 2.0) in Resources/UI/IconArt, cu shaderul Resources/Shaders/UIIconArt.shader (nu le recoloreaza tint-ul). Chenarele au ramas la fel.

## Sesiunea 17 - HUD skill-uri, potiuni, iconite din modelele 3D, echipament variat
- Skill-urile (Z/X/C) stau deasupra barei de HP, fara chenar, mai mari; in cooldown iconita e stinsa si se reumple circular.
- Potiunile au iconite proprii (36 XP, 37 DMG, 38 COINS) si apar jos-dreapta DOAR cat timp sunt active (cu timpul ramas). Se beau cu 1/2/3 sau din INVENTORY > POTIONS (panou cu USE).
- TOKENS: iconita noua (moneda turcoaz cu stea, 19.png).
- Itemele NU se mai echipeaza singure (nici "best"): le echipezi tu (EQUIP / EQUIP BEST).
- Echipament: `Core/MeshGen.cs` (cutii rotunjite, conuri, inele) + `Gameplay/GearVisuals.cs` rescris: fiecare tip (Helm/Cap/Hood/Crown, Plate/Mail/Tunic/Vest, 6 arme, inele) are 4 stiluri (coarne, aripi, panas, palarie de mag, gluga, coroana regala, solzi, mantie, tepi, curele in X etc.) + culoare proprie din id-ul itemului. Piesele unui set arata la fel.
- `UI/GearIcons.cs`: iconitele itemelor sunt randate din modelele 3D (studio ascuns, layer 31), cu contur si umbra ca restul iconitelor -> iconita = ce porti.

## Sesiunea 18 - curatenie UI, MARKETS, MATERIALS, verificare generala
- Sunetul fantanii (ambientul din Trade Plaza) e mult mai incet (x0.12).
- INVENTORY: tab nou MATERIALS (Ore, Forge Stone, Guardian Cloak, cele 3 potiuni, Shards, Tokens) ca obiecte in grila; potiunile se folosesc cu USE. Scos randul de materiale din stanga si romburile verzi (set) de pe iteme.
- SETTINGS: CONTROLS mutat in GUIDE (sectiune noua CONTROLS); 3D grass are rand propriu. Minimap-ul a fost scos din joc (UI/Minimap.cs e gol).
- Z / C nu mai rotesc camera (sunt skill-uri).
- Trade Plaza: pavele de la piata pana la portalul BACK TO ADVENTURE (WorldVersion 22).
- Fereastra TRADING refacuta: "MARKETS" cu tab-uri (ALL MARKETS / OPEN SPOTS / MY REGION / FRIENDS), grila 3x3 de piete cu nume proprii (Amber Bazaar, Cobalt Market...), regiune, bara de ocupare, ENTER, QUICK JOIN, PRIVATE MARKET.
- Reparatii din verificare: listarile de pets/materiale din booth nu se mai sterg la incarcare; save-ul curata itemele invalide si id-urile duplicate; potiuni/buff-uri limitate; pretul oualor nu mai poate depasi limita; skill-urile se opresc la moarte/teleport; butoanele nu raman selectate (ENTER nu le mai apasa din nou); butonul "reset passes (test)" apare doar in editor.

## Sesiunea 19 - rebirth max 5, SHOP nou, pets animati, loading screens, curatenie
- Rebirth: maxim 5, progresiv (Lv 50 / 75 / 100 / 130 / 160). Save-urile cu mai mult de 5 sunt aduse la 5. Titluri: PHOENIX = rebirth 3, ETERNAL = rebirth 5.
- MARKETS: scos tab-ul OPEN SPOTS (raman ALL MARKETS / MY REGION / FRIENDS).
- PETS: in dreapta pet-ul selectat apare ANIMAT (se roteste, face animatii); in grila fiecare pet are poza lui (`UI/PetStudio.cs`).
- SHOP: preturi mici (pass-uri $0.99-$2.49, ALL PASSES $4.99 "SAVE 44%"), tokens $0.49/$1.99/$3.99, coins & shards $0.49/$0.99/$1.99, INVENTORY SPACE +20/+40/+100 sloturi ($0.49/$0.99/$1.99, o singura data). Inventarul are pagini cand ai mai mult de 40 de iteme. (Platile sunt inca de TEST.)
- Loading screens (`UI/LoadingScreen.cs`): la pornire (COLOR RPG), cand pleci spre o harta (culorile hartii) si spre Trade Plaza (auriu, numele pietei) - portal care se roteste, iconite care plutesc, bara de progres, TIP.
- Curatenie: ColorRPG > Clean Up Unused Assets sterge si backup-urile vechi (pastreaza ultimele 2) si Minimap.cs; la Rebuild World se pastreaza doar ultimele 3 backup-uri. Rulat: 24 fisiere (~175 MB) mutate in Recycle Bin.

## Sesiunea 20 - prieteni, chat privat, ADMIN pe pagini
- Buton **FRIENDS** (stanga): cauti jucatori dupa nume, ADD, MESSAGE (scrie in chat, tab-ul FRIENDS), INVITE pe piata privata, X = sterge.
- Chat: tab nou FRIENDS (mesaje private), bula cu mesajul tau deasupra capului, notificarile mutate in stanga (nu mai stau sub AUTO).
- Avatarul din INVENTORY se actualizeaza imediat cand schimbi echipamentul (`Kill()` in loc de `Destroy()` intarziat).
- **ADMIN** (F1) pe pagini: PLAYER, ITEMS (orice item, orice stil), PETS (orice pet), MODERATION (kick, warn, mute, ban, trade ban, chat restrict, booth ban, cadouri), GIVEAWAYS (premiu, castigatori, public, intarziere, anunturi), LOG.

## Sesiunea 21 - reparatii, telefon (Google Play), sistemul jocului
**Reparatii**
- PETS: grila curata (poza mare, nume, banda de raritate, EQ); damage-ul si detaliile apar doar la hover; animatia din dreapta mai mica; fara "egg" la nume.
- INVENTORY: avatarul mai mic, pe podium. Click = selectezi UN item (EQUIP / FAVORITE / SELL...). FAVORITE se anuleaza apasand din nou (UNFAVORITE). Multi-select doar in modul SELL: CANCEL / SELECT ALL / SELECT JUNK / SELL N.
- GUIDE: BASICS rescris (farm, top, ranguri, pets, forja, trade...), sectiune noua PROGRESSION, CONTROLS separat pentru PC si telefon.
- Loading screens cu 0.5s mai lungi (4.1s / 2.2s / 2.2s).
- Coroana regala nu mai pluteste (banda pe crestet, arcuri joase).
- Chat: se poate derula (rotita / deget) in GLOBAL, SERVER si FRIENDS; mesajul tau apare imediat (inainte se taiau ultimele randuri); FRIENDS arata conversatia cu prietenul ales (bara "Chatting with X" + CHANGE) pana schimbi prietenul sau harta; bula deasupra capului doar pe SERVER.
- CHARACTER: textele din cardurile de AWAKENING nu se mai suprapun. Insigna clasei: litera clasei pe treapta (ex. Assassin A -> Shadowblade S -> Phantom P), tot mai "legendara" (I [A], II «S», III auriu).
- TITLURI: fiecare are raritate (COMMON..MYTHIC); cele grele au font clasic (Pagella) sau caligrafic (TeX Gyre Chorus, `Resources/UI/TitleFont.otf`, licenta GUST) iar MYTHIC straluceste (`UI/TitleStyle.cs`).
- SETTINGS: buton EXIT GAME (apasat de 2 ori). Umbrele de nori au fost scoase.
- **Booth**: 18 locuri (iteme, pets, materiale), pretul il scrii tu (fara FAIR / -10% / +10% si fara oferte/negocieri). Booth-ul vinde DOAR cat stai in piata pe serverul lui; cand pleci (alta harta / alt server / iesi din joc) devine UNCLAIMED, listarile raman la tine si urca din nou pe urmatorul booth. Interior nou: copertina in dungi, OPEN/CLOSED, bara de viteza a vanzarii, pagini de iteme; textul care incurca tab-ul MATERIALS a fost scos.
- **Lupta**: lovitura de aproape (raza 3.6 + arma); skill-urile tintesc max 13m; fiecare monstru are raza lui de agro (7-11m, dupa viteza), te urmareste doar cat esti aproape si se intoarce in zona lui; boss-ul lupta si poate fi lovit DOAR in arena lui ("ENTER THE ARENA!"); pets stau mai aproape (si nu ataca boss-ul din afara arenei).
- **PC**: skill-urile pe tastele 1 / 2 / 3; potiunile de boost doar din INVENTORY > MATERIALS (Q = potiunea de viata ramane).

**Telefon (Google Play)** - `Core/Device.cs`, `UI/MobileControls.cs`, `UI/SignInScreen.cs`, `Online/OnlineAccount.cs`
- Jocul recunoaste singur telefonul; pe PC nu se schimba nimic. Test pe PC: ADMIN > PLAYER > TOOLS > SIMULATE MOBILE.
- Joystick stanga-jos (impins la margine = alergi), FIGHT (tii apasat = ataci) si JUMP dreapta-jos, semi-transparente; tragi cu degetul pe ecran = camera, 2 degete = zoom; apesi pe mesajul galben = interactionezi ("TAP: ...").
- Tastele nu mai apar pe telefon; skill-urile mai mari si mai sus; monedele urca langa coloana din dreapta; potiunile active in stanga barei de HP.
- Inainte de loading screen: SIGN IN WITH GOOGLE PLAY (salvare online in cloud, se alege salvarea cu mai mult timp de joc) sau PLAY OFFLINE.
- Activare Google Play: importa pluginul Google Play Games (v11+), Android setup, adauga in Scripting Define Symbols `COLORRPG_GPGS`. Meniu nou: **ColorRPG > Prepare Android (Google Play)** (landscape, IL2CPP ARM64, API 24+, .aab).

**Sistemul jocului (progres + economie)**
- Limite: nivel maxim **100**, POWER maxim **10M** (`GameData.MaxLevel`, `GameData.PowerCap` - se cresc la update-uri cu nivele / harti noi).
- Timp (joc activ, ~18 monstri/minut): Desert Lv 15 ~1-2h, Frozen Lv 35 ~5h, Lava Lv 60 ~13h, Lv 100 ~40h; apoi 5 Rebirth-uri (Lv 50 / 65 / 80 / 90 / 100) => ~100h pentru tot jocul (+ forja +15 si pets Mythic).
- Trade Plaza de la nivelul **10** (~45 min).
- Pets: primul ou e GRATIS (primul pet in primele minute), al doilea costa ~10 minute de farm (1.200 coins). Pets rebalansati (erau de 27x damage).
- XP pe harti: 20 / 110 / 260 / 420.
- Monedele: COINS (monstri, questuri, vanzari - forja, oua, awakening), SHARDS (rare: boss, questuri, la 5 nivele - forja +10..15, awakening), TOKENS (bani de trading - castigi vanzand pe booth).
- Drop-uri: itemele 5% (mai rar), Forge Stones 8% de la monstri, cloaks doar de la boss (6%) => forja costa efort; **questurile** dau mult: 4 questuri pe harta (cate unul pe fiecare zona + boss) cu potiuni, shards, Forge Stones si Guardian Cloaks.
- SHOP: pachetele de coins cresc cu tine (30 min / 2h / 8h de farm pe cea mai buna harta a ta).
- **Monstrii pe zone**: fiecare tip are locul lui pe fiecare harta (stanga / dreapta / in fata arenei boss-ului) cu eticheta "X ZONE".
- ADMIN > PLAYER > TOOLS: SIMULATE MOBILE, GO (orice harta / plaza), MAX LEVEL, COMPLETE QUESTS, RESET EGG PRICES, ALL PASSES, UNLOCK ALL TITLES, CLOUD SAVE NOW.

## Sesiunea 22 - schimbarea clasei, reset STATS zilnic, cadouri ADMIN
- CHARACTER > **CHANGE CLASS**: alegi alta clasa dupa awakening (treapta ramane), dar costa mult: coins (3x pretul awakening-ului curent, min 100K) + 150 shards x treapta, si pretul creste la fiecare schimbare. Confirmare cu 2 click-uri; si alegerea clasei cere acum 2 click-uri. Sfat in GUIDE (AWAKENING) si pe loading screen.
- STATS: **FREE RESET** o data la 24 de ore (butonul arata cat mai ai de asteptat).
- ADMIN > GIVEAWAYS: premii noi (Forge Stones, Guardian Cloaks, potiuni) + **GIFT A PLAYER**: cauti orice jucator sau alegi din cei online (ONLINE ONLY), il selectezi si ii trimiti premiul ales.
- Telefon: chat-ul (tab-uri, butoane) si questurile sunt mai mari.
- Pets stau putin mai departe de erou.

## Sesiunea 23 - verificare, curatenie, reclama Google Play
- Scos codul ramas de la umbrele de nori (`CloudShadows` si setarea ei). Proiectul nu mai avea alte fisiere nefolosite (Clean Up rulat anterior, 2 backup-uri).
- **Reclama**: `UI/PromoDirector.cs` + `Editor/ColorRPGPromo.cs`. Meniu **ColorRPG > Record Promo Video (Play mode)** sau ADMIN > TOOLS > RECORD PROMO: filmeaza singur ~33s 1920x1080 30fps (intro, lupta, skill-uri, boss, loot, pets, titluri, Trade Plaza, Lava, outro) cu titluri animate, HUD curat (fara ADMIN/mesaje), MP4 encodat direct de Unity + 3 capturi PNG. Totul in folderul `Promo` (langa Assets). Daca exista `Promo/record.flag`, Unity intra singur in Play si filmeaza.
- Versiunea finala cu muzica (temele jocului Map0 / Boss2 / Plaza): `Promo/ColorRPG_GooglePlay_Promo_1920x1080.mp4`.

## Sesiunea 24 - ONLINE pas 1: Unity Gaming Services (cont, salvare, clasamente)
- Pachete noi (Packages/manifest.json): Authentication 3.7.3, Cloud Save 3.4.1, Leaderboards 2.3.4, Deployment 1.7.2. Proiectul e legat de Unity Cloud (org cxstudiodev).
- `Scripts/Online/UGS/Ugs.cs` (assembly separat `ColorRPG.Online.UGS`, se compileaza si fara pachete): intrare in cont (anonim, pe dispozitiv), nume in clasamente, Cloud Save, Leaderboards.
- `Scripts/Online/NetSync.cs`: la pornire intra in cont, ia salvarea de pe server daca are mai mult timp de joc (si reincarca scena), salveaza pe server la 1 minut + la iesire/fundal, trimite POWER si TIME PLAYED (nu in GOD mode), citeste top 10 + locul tau la 1 minut.
- Panourile de clasament din hub arata clasamentul REAL cand esti online (eticheta ONLINE); offline raman cele simulate. Titlurile TOP folosesc locul real.
- ADMIN > TOOLS arata starea serverului. Testat: "signed in as Ionutcx#13744" - contul si salvarea merg.
- Clasamentele se definesc in `Assets/ColorRPG/Services/power.lb` si `playtime.lb`; se trimit in cloud cu **ColorRPG > Online > Deploy Server Config** (sau automat daca exista `Promo/deploy.flag`).
- Test automat: **ColorRPG > Run Smoke Test** (sau `Promo/test.flag`) -> `Promo/test_result.txt`.
- Urmeaza: chat real (Vivox), prieteni (Friends), trading verificat pe server (Cloud Code), Google Play Billing, legarea contului de Google Play.

## Sesiunea 25 - notificari noi, ADMIN pe orice dispozitiv, build PC
- Notificarile (rank nou, iteme, vanzari, quest...) sunt acum carduri in centrul de sus (sub bara de boss): contur colorat, iconita in cerc, TITLU mare + detalii mai mici, apar cu un "pop" si dispar dupa 4s (max 3). Cand apare numele hartii, coboara sub el.
- **ADMIN** (`Scripts/Online/AdminAccess.cs`): butonul apare doar pentru admin - in editor, pe contul tau online (PlayerId in AdminIds) sau pe orice dispozitiv deblocat cu codul de admin (SETTINGS > apasa de 5 ori pe "COLOR RPG vX" jos > codul). Merge si online si offline; LOCK ADMIN il scoate. Codul nu e scris in joc (doar amprenta SHA-256). ADMIN > TOOLS arata PlayerId-ul (pentru a-l adauga in AdminIds).
- **ColorRPG > Build PC Version**: face `Builds/PC/ColorRPG.exe` (Windows) - te joci fara Unity, pe acelasi server online.
- Deploy clasamente: fereastra Deployment se deschide singura inainte de deploy.

## Session 26 – pregătire Google Play
- Keystore de upload: `Keystore/colorrpg-upload.keystore` (alias colorrpg; parola în keystore_info.txt – NU se publică, se face backup).
- Meniu nou: ColorRPG → Build Android (.aab for Google Play) – semnează automat, crește versionCode.
- Settings: butoane PRIVACY POLICY și DELETE MY DATA (șterge contul UGS + cloud save + progres local); Player ID afișat lângă versiune.
- `Ugs.DeleteAccount` (Cloud Save DeleteAllAsync + Authentication DeleteAccountAsync).
- Docs/: privacy-policy.html (publicată ca artefact), privacy-site/index.html (pentru GitHub Pages), GOOGLE_PLAY_LAUNCH.md (Data safety, rating, store listing, closed testing).
- Blocante rămase: Android Build Support neinstalat; magazinul cu $ fără Play Billing; Report/Block în chat.
- Nume final: **Hueborn: Loot Legends** (sub iconiță: „Hueborn”, pachet Android `com.cxstudio.hueborn`). Codul intern rămâne `ColorRPG` (namespace, meniuri). Build-urile pun productName doar pe durata build-ului (salvarea din editor nu se pierde). De refăcut: video promo (are încă „COLOR RPG” în titluri).

## Session 27 – plăți, siguranță chat, lume blocată, builduri
- Google Play Billing: Unity IAP 5.4.3, `Online/IAP/Iap.cs` (asmdef separat, CRPG_IAP) + `Online/Shop.cs` (18 produse, id-uri în Docs/GOOGLE_PLAY_LAUNCH.md). Magazinul pornește după UGS (NetSync) – altfel Leaderboards nu se inițializa.
- Chat: buton roșu „!” → BLOCK / UNBLOCK / REPORT (ChatSafety.cs, raport pe email, lista de blocați în PlayerPrefs).
- Lumea din scenă e BLOCATĂ (ColorRPGSceneBuilder.WorldLocked = true): modificările manuale ale utilizatorului rămân; Rebuild cere confirmare dublă; backup-urile de scenă merg în `_Backups/Scenes` (în afara Assets).
- Organize: ColorRPG > Organize Hierarchy (obiecte de același fel în foldere „[Oak] x64”) și Organize Project Folders (Promo/Video, Screenshots, Renders, Store, Brand, Logs).
- Performanță telefon: prima pornire pe MEDIUM, render scale 0.85, fără MSAA, max 2 cascade de umbre, ecranul nu se stinge.
- Android: modul instalat, minSdk 26, meniu Build Android TEST (.apk). Build PC: Builds/PC/Hueborn.exe.

## Session 28 – verificare generală (versiunea finală 1.0.0)
- Cadoul din harta 1 nu se mai rotește; ADMIN „GIFT READY” arată acum mesaj.
- Admin ITEMS: iconițele nu-și mai schimbă culoarea la fiecare click (mostre fixe), itemul primit arată exact ca iconița (id nou, același stil/culoare).
- Forja: sub +5 o forjă eșuată nu mai scade nivelul și nu consumă Guardian Cloaks; mesajul „forge it in …” arată harta corectă.
- Unequip cu inventarul plin e blocat; loot cu inventar plin nu mai arată toast fals; Quality din tooltip pe intervalul corect.
- Booth-urile simulate: același aspect la revenire; nu mai poți cumpăra o ofertă dispărută.
- Iconițe: cache corect pe nivel de forjă la Epic+; arma de rezervă nu-și mai schimbă culoarea.
- Salvare: recompensele „la fiecare 5 niveluri” nu se mai sar (și revin după Rebirth); CHANGE CLASS nu mai pierde treptele de awakening; jucătorii noi primesc „Hero####” în loc de „Ionutcx”.
- Telefon: buton HEAL (poțiunea de viață), timerele de poțiuni nu mai acoperă JUMP, JUMP nu mai rămâne „memorat”.
- „Damage numbers OFF” ascunde și CRIT; prietenii simulați max Lv 100.
- Plăți: pass-urile / spațiul din inventar cumpărate se restaurează automat de la Google (reinstalare / telefon nou).
- DELETE MY DATA blochează salvarea până la repornire și uită salvarea cloud locală.
- Android: URP setat pentru toate platformele (scăpat de ecranul roz).
- Scena salvată ca finală + backup `_Backups/Scenes/SampleScene_FINAL_v1.0.0_2026-10-02.unity`.
- Telefon: joystick plutitor (apare unde pui degetul, stânga-jos); coloana din dreapta mai sus/mai mică, monedele deasupra FIGHT; ecranul „Sign in with Google Play” nu mai apare cât timp pluginul Google Play Games nu e instalat (contul online Unity se face automat).
- FPS: opțiunile 30 / 60 / 120 (MAX scos); pe telefoane cu ecran 90/120 Hz se cere și refresh-ul mare; Settings arată Hz-ul ecranului.
- Builduri finale pe Desktop: „Hueborn Builds/PC/Hueborn.exe” și „Hueborn Builds/Android/Hueborn_TEST.apk”.

## Sesiunea 29 (2 oct 2026) - MULTIPLAYER + polish UI
- **Multiplayer real** (gratuit: Unity Multiplayer Services + Relay + Netcode for GameObjects):
  - `Online/MP/MpLink.cs` (asmdef propriu): camere ("sessions"), gazda = primul jucator, mesaje prin Relay.
  - `Online/Mp.cs`: Trade Plaza = o camera pe server (max 18), fiecare harta = camere de max 4 (se face alta cand e plina).
  - Jucatorii reali apar ca `RemotePlayer` (erou + arma + nume/nivel); online dispar cumparatorii simulati, booth-urile simulate si chat-ul simulat.
  - Monstrii: gazda ii misca si decide kill-ul (ultima lovitura); loot-ul e comun (il ia primul).
  - Booth-uri reale: cumperi de la alti jucatori pe tokens (vanzatorul confirma).
  - Chat SERVER/GLOBAL ajunge la jucatorii din camera; FRIENDS = mesaj privat daca e in camera.
  - Test: smoke test -> "MP ... active: True loc: m0 host: True".
- Calitate: BETTER PERFORMANCE / BETTER QUALITY + `DeviceTier` + `AutoPerf` (rezolutie adaptiva pe telefon).
- CHARACTER: statistici mari in dreapta (kills, bosses, quests, deaths, timp, rebirths).
- AWAKENING mutat la **Awakening Shrine** (ultima harta, stanga cercului de spawn) + buton GO TO SHRINE + card in TELEPORT + ghid.
- TELEPORT: poze statice ale hartilor (`Resources/UI/Maps`, meniu ColorRPG > Capture Map Pictures).
- Misiuni: una singura odata, se repeta pana o schimbi. Pets: storage 16 (grila 4x4).
- ADMIN: MODERATION -> cadou cu orice premiu (coins, shards, tokens, pet, item, pietre, cloaks, potiuni, XP, ore); GIVEAWAYS fara "GIFT A PLAYER".
- UI: butoane/panouri plate (fara carouri/dungi), panouri ~72% opace; tab-urile de chat si butonul QUEST mai mari. Originalele: `_Backups/UI_original`.
- Apa fantanii fara fantana (curgea "in sus") dispare automat.
- Politica de confidentialitate actualizata (sectiunea multiplayer) + Data safety: App interactions.
- Update (seara): Awakening doar de la Lv 60/75/90 la altarul din Lava Volcano (scos din TELEPORT si din CHARACTER); Settings fara FPS limit / Hz (doar FPS live; 30 pe telefoane slabe, 60 in rest) + optiunea "Play with others on maps"; chat GLOBAL real (Vivox, canal "hueborn-global") cu anti-spam la trimis si la primit; fara mesajul "you are the host"; camerele raman pana pleaca ultimul (host migration); questuri simplificate (iconite + numere).
- **Update "natura" (world v24)**: toate hartile + Trade Plaza refacute cu **Stylized Nature MegaKit** (Quaternius, CC0) din `Resources/Models/Megakit`:
  - acelasi stil pe toate hartile, nuantat pe tema (`World/NatureKit.cs`): pajiste verde, desert uscat, munte inzapezit (pini, roci gri), vulcan ars (copaci morti).
  - sol cu textura pictata (`Resources/Textures/Ground0..3`, `Cobble`), poteci din pietre, margini = stanci + copaci (ziduri invizibile).
  - mai putine obiecte (fara iarba 3D grea / copacii vechi); iarba mica se ascunde cu "Grass" off (`DetailGroup`).
  - Editor: `ColorRPGNaturePack.cs` (materiale MK_*, Promo/rebuildworld.flag = backup + rebuild). Backup scene vechi: `_Backups/Scenes/*_BEFORE_NATURE_*.unity`.
  - Fix: texturile "*NormalTree*" erau importate ca normal map (frunze = patrate albe) -> doar `*_Normal.png` e normal map.
  - Diagnostic: `Promo/matdump.flag` -> `Promo/Logs/mat_dump.txt`.
- **Polish lume (world v27)**: discurile verzi de la spawn / ou / NPC-ul cu questuri -> piatra pavata + bordura de pietre (`StoneDisc`); arena boss-ului = cerc de stanci inalte (menhire) nuantate pe harta, podea cu textura solului; scoase definitiv: coroanele verzi (fire-basket), bannerele (arena + Trade Plaza), obeliscurile din desert; cactusi noi construiti din bucati (`BuildCactus`: 2 nuante, coaste, brate, floare); stancile de pe margine nu mai ies peste linia hartii (spatele drept, fata neregulata), la fel in Trade Plaza. Poze de verificare: `Promo/Renders/view_arena*`, `view_egg*`.
- **v28**: fantana din hub mutata langa cadou, pe pavaj rotund (ca in Trade Plaza, mai mic); solul pictat + pavajul folosesc material URP Lit mat (`Art.MatteGround`) - culoarea nu se mai "spala" cand te intorci / te misti (luciul si petele procedurale ale shader-ului Surface). Test: `Promo/Renders/view_dir0..3`.
- **v31**: portaluri noi (`World/PortalArt.cs`: inel de piatra cu rune, cristal plutitor, vartej rotativ `Resources/Textures/Swirl.png`); Forja cu acoperis de tigla, platforma pavata, scantei, minereu; altarele REBIRTH si AWAKENING refacute (`World/ShrineArt.cs`: platforma, cerc de rune, menhire cu cristale, inel orbitant); quest tracker mai mare; notificarile ~70% opace; pet echipat = contur verde + punct (fara "EQ"); pe telefon fara heal (buton + Q scoase); Guardian Cloak: 1 la FIECARE incercare protejata (reusita sau nu); "Online: N" langa butoanele de chat (Vivox, canalul global).
- **v32**: REWARDS (`Gameplay/Rewards.cs`, `UI/RewardsUI.cs`, buton cu cadou deasupra lui STATS + punct rosu): 10 cadouri online/zi (5 min ... 2 ore, reset la miezul noptii, ultimul = GIFT BOX) + 7 zile (zi ratata = continui, dupa ziua 7 reincepe); tarabe noi in Trade Plaza (podea de scanduri, copertina rotunjita in dungi cu volan, felinare, lazi/saci/fructe, panou cu rama); notificarile fara chenar.
- **Iteme unice**: arme 3D reale (KayKit Fantasy Weapons Bits + Quaternius Modular Weapons, CC0) - `Gameplay/GearModels.cs`: 6 tipuri x 4 harti = 24 de modele, orientate automat, nuantate pe harta + raritate (panglica, pietre, stralucire, scantei Legendary/Mythic); coifuri/armuri/inele cu tema hartii + raritate (`Gameplay/GearDecor.cs`). Iconitele se randeaza din aceleasi modele (acelasi aspect pe avatar). Plansa de verificare: ColorRPG > Gear Sheet -> `Promo/Renders/gear_sheet.png`.
- **Optimizare etapa 1-2 (world v34)**: doar harta curenta e activa (`Game.CullMaps`), fara lumini punctiforme in lume (emisie + bloom), obiectele mici nu mai fac umbra, detaliile (iarba/flori/pietricele) pe layer 28 cu distanta de desenare, Opaque/Depth Texture oprite, HDR oprit pe telefoane slabe (bloom threshold 0.82), umbre: telefon = hard + 1 cascada + 38/48 m, PC = soft + 2 cascade; PC = VSync fara limita; sfera/capsula low poly in `Art.Prim` (768/832 -> ~260/150 triunghiuri). Raport: ColorRPG > Perf Report -> `Promo/Logs/perf_report.txt`.

## Optimizare etapa 3 - GC (memorie gunoi) - 2026-10-03
- HUD: textele se seteaza doar cand se schimba valorile; tracker/nameplate/prompt la 0.2s; Bar.Set(pct, null) = doar umplere.
- Player: Equipped() fara lambda (Find(x=>) aloca la fiecare apel); SetBonus calculeaza o data piesele echipate.
- Mp: chei de locatie pastrate, chei profil/booth comparate de 4x/s, liste refolosite, fara SendState/SendSnapshot cand esti singur in camera.
- Rezultat GC Report: 10.6 KB/cadru -> 1.2 KB/cadru (restul e mai ales Burst JIT din Editor, nu exista in build).

## Update mare - 2026-10-03 (inainte de etapa 4)
- Iteme: catalog fix de 96 (ItemCatalog.cs): pe fiecare harta 6 arme / coifuri / armuri / inele (2 Common, 2 Rare, Legendary, Mythic). Seturile scoase complet. Itemele vechi se convertesc la incarcare (aceeasi harta / categorie / raritate, forja pastrata). Fara cristale in plus pe coifuri / armuri / inele.
- ITEM INDEX: pupitru cu carte in Meadow (dreapta drumului) + fereastra cu toate itemele (gasite / negasite, D.seenCat).
- Admin: grila cu catalogul (harta x 6), RESET LEADERBOARDS (live: ora salvata public in Cloud Save + anunt in chatul global; scorurile mai vechi nu se mai afiseaza).
- Pets: bonus maxim ~+100% (4 Mythic din Lava), Common Meadow +2%.
- Loot: cufar nou (benzi in culoarea raritatii), iconita itemului / minereului deasupra, cerc colorat pe jos, dispare dupa 10s (ultimele 3 clipeste), eticheta doar "1x Nume" / "N Ore" (~70%).
- LEVEL UP: banner nou + efect (unde, coloana, spirale). Ridicare loot: aplecare scurta, fara maini incrucisate.
- HUD: monedele sus-dreapta, coloana stanga mai jos, butoanele din dreapta mai rare si mai la stanga, FIGHT / JUMP mai la stanga, Online sub GLOBAL (ascuns cand chatul e deschis), toast quest scurt.
- Camera nu se mai roteste singura dupa erou / inamic. Inventar: 6 pe rand, derulare (bara, sageti, drag, rotita).
- Echipament optional (si arma). Sageata quest + pin deasupra tintei + cerc pe jos (noi).
- Multiplayer: ceilalti vad coiful, armura, arma (aceeasi marime), pets, clasa, titlul, puterea; join/leave doar in chatul SERVER; runInBackground pe PC; ADMIN > TOOLS arata starea MP.
- Iconite: citire RT prin Blit (fara MSAA) - repara "zgomotul" de pe telefon; portret cu shader UIPortrait + lumini in plus.
- Sunet: porneste de la 10% si creste treptat (intrare in joc / schimbare harta). Bloom / saturatie mai mici, Neutral tonemapping fara HDR.
- Margini harti: OuterLands.cs (sol larg + dealuri + munti in tema hartii); ceata 70..380, far clip 400. WorldVersion 39.
- Corectii: monedele inapoi jos-dreapta, QUEST inapoi sus, portretele ca inainte (fara shader / lumini in plus), ADMIN ITEMS fara GIFT MAP / RARITY, munti verzi in Meadow si in Trade Plaza, margini rare (stanci la ~20 m, putini copaci) + mai multe stanci / copaci in interior. WorldVersion 40.
- Coliziuni: Obstacle.cs (colider pentru erou + obstacol pentru inamici, care se misca fara fizica - Enemy.PushOut) pe copaci, stanci, cactusi, piramide, vulcani, oameni de zapada, stanci / copaci din Trade Plaza; munti / dealuri cu MeshCollider. Marginea: stanci / copaci asezati neregulat (fata-spate, stanga-dreapta, rotiti). Muntii mai departe de marginea hartii (randul 1 mai jos). WorldVersion 41.
- Coliziuni v2: stalpi invizibili inalti (min. 6 m, capatul de jos ingropat => perete drept la sol, nu se mai urca pe pietre) pe un copil "Collider" pe layer 2 (camera il ignora). Oameni de zapada mai lati, ren + sanie, pietrele mici din arena boss-ului, cactusi, caruta / butoaie / cufar din Trade Plaza. Spatiere: copacii min. ~9 m, stancile mari dupa marime, margini cu loc liber intre obiecte; mai putini copaci in Meadow / Frozen / Lava. WorldVersion 42.
- 2026-10-03: GUIDE: loot 10s + mobile tap item name, echipament optional, sectiune noua ITEM INDEX (96 iteme + 16 pets).
- 2026-10-03: ETAPA 4 (Mesh LOD): copaci/tufe/pietre mari Megakit cu LOD automat la import (3 nivele), scosi din static batching; WorldVersion 43. Triunghiuri randate: Meadow 1.15M->657k, Desert 1.23M->678k, Frozen 741k->420k, Lava 1.02M->455k, Plaza 416k->339k.
- 2026-10-03: LUMEA BLOCATA TOTAL (WorldFinal = true in ColorRPGSceneBuilder): niciun rebuild (meniu / flag / update) - userul face modificari manuale in scena.
- 2026-10-03: Scena FINALA: tool nou ColorRPG > Fix Colliders To Visuals (97 coliziuni mutate pe modelele mutate manual, 6 coliziuni fara model dezactivate). Backup: _Backups/Scenes/SampleScene_FINAL_v2_2026-10-03.unity (+ _FINAL_manual = varianta userului inainte de fix). WorldFinal ramane true.
- 2026-10-03: v1.0.0 FINAL: ITEM INDEX cu fata spre spawn; sunet: liniste pana la prima scena, 10% cat timp e loading/sign-in, apoi creste lent (PC + telefon); GUIDE: lista de capitole derulanta cu bara de scroll; versiune 1.0.0; Organize Hierarchy (1544 obiecte in 153 foldere); 231 asset-uri nefolosite + 50 backup-uri vechi -> Recycle Bin; backup final _Backups/Scenes/SampleScene_FINAL_v1.0.0_2026-10-03.unity; ignore.conf + .gitignore exclud Keystore/_Backups; AAB: Desktop/Hueborn Builds/Android/Hueborn_1.0.0_2.aab (versionCode 2); Check in Unity Version Control facut.
