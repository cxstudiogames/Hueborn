# CREDITS - assets folosite in ColorRPG

Toate assets-urile externe sunt **CC0 1.0 (Creative Commons Zero / domeniu public)**:
http://creativecommons.org/publicdomain/zero/1.0/
Le poti folosi gratuit in proiecte personale, educationale si comerciale. Creditarea nu e obligatorie, dar o facem cu placere.

Autor: **Kenney** (www.kenney.nl). Fiecare pachet a fost descarcat de pe pagina lui oficiala pe 29.09.2026. Fisierul `License.txt` din fiecare arhiva confirma licenta CC0.

| Pachet (versiune) | Sursa | Licenta | Ce folosim | Unde e in proiect |
|---|---|---|---|---|
| Mini Characters (1.0) | https://kenney.nl/assets/mini-characters | CC0 | 12 eroi animati (idle, walk, sprint, attack, die...) | `Assets/ColorRPG/Resources/Models/Characters` |
| Mini Dungeon (2.0) | https://kenney.nl/assets/mini-dungeon | CC0 | boss orc, sabie, cufar, moneda, potiune, butoi | `.../Models/Dungeon` |
| Mini Arena (1.1) | https://kenney.nl/assets/mini-arena | CC0 | statuie, coloane, stindarde, raft de arme | `.../Models/Arena` |
| Graveyard Kit (5.0) | https://kenney.nl/assets/graveyard-kit | CC0 | schelet, zombie, fantoma, vampir (animati), obeliscuri, cosuri cu foc | `.../Models/Graveyard` |
| Cube Pets (2.0) | https://kenney.nl/assets/cube-pets | CC0 | 24 de animale animate (pets + inamici: mistret, albina, crab, leu, pinguin, urs polar) | `.../Models/Pets` |
| Holiday Kit (2.0) | https://kenney.nl/assets/holiday-kit | CC0 | brazi cu zapada, oameni de zapada, cadouri, felinare | `.../Models/Holiday` |
| Fantasy Town Kit (2.0) | https://kenney.nl/assets/fantasy-town-kit | CC0 | tarabe, fantana, moara de vant, carut | `.../Models/Town` |
| Nature Kit (2.1) | https://kenney.nl/assets/nature-kit | CC0 | copaci, flori, ciuperci, pietre, cactusi, palmieri, cort | `.../Models/Nature` |
| UI Pack (2.0) | https://kenney.nl/assets/ui-pack | CC0 | butoane, panouri (fontul *Kenney Future* nu mai e folosit din sesiunea 3b) | `.../Resources/UI` |
| Game Icons | https://kenney.nl/assets/game-icons | CC0 | iconite (inventar, auto, sunet) | `.../Resources/UI/Icons` |
| RPG Audio | https://kenney.nl/assets/rpg-audio | CC0 | sunete de sabie, echipare, monede | `.../Resources/Audio` |
| Impact Sounds (1.0) | https://kenney.nl/assets/impact-sounds | CC0 | lovituri, crit, minereu, pasi (iarba/nisip/zapada) | `.../Resources/Audio` |
| Interface Sounds (1.0) | https://kenney.nl/assets/interface-sounds | CC0 | click, deschidere/inchidere, eroare, portal | `.../Resources/Audio` |
| Casino Audio (1.1) | https://kenney.nl/assets/casino-audio | CC0 | sunetul de colectare a monedelor | `.../Resources/Audio/Coin` |
| Music Jingles | https://kenney.nl/assets/music-jingles | CC0 | jingle-uri (level up, forja, boss, rebirth, pet) | `.../Resources/Audio/Jingle*` |

## Natura (copaci, roci, iarba, flori, pietre de poteca)
| Pachet | Autor | Licenta | Unde |
|---|---|---|---|
| Stylized Nature MegaKit (Standard) | Quaternius (quaternius.com) | CC0 1.0 (domeniu public) | `Assets/ColorRPG/Resources/Models/Megakit` |

Texturile de sol (`Resources/Textures/Ground0..3`, `Cobble`) sunt generate de cod pentru acest joc.

## Arme 3D
| Pachet | Autor | Licenta | Unde |
|---|---|---|---|
| KayKit : Fantasy Weapons Bits 1.0 (Free) | Kay Lousberg (kaylousberg.com) | CC0 1.0 | `Assets/ColorRPG/Resources/Models/Weapons/KK_*` |
| Modular Weapons Pack | Quaternius (quaternius.com) | CC0 1.0 | `Assets/ColorRPG/Resources/Models/Weapons/Q_*` |

## Font
| Font | Autor | Licenta | Unde |
|---|---|---|---|
| TeX Gyre Pagella Bold (`PagellaBold.otf`) | GUST e-foundry (B. Jackowski, J. M. Nowacki) | GUST Font License (gratuit, poate fi inclus in jocuri; textul licentei e in `Resources/UI/GUST-FONT-LICENSE.txt`) | `.../Resources/UI` |

Nu este CC0, dar licenta permite folosirea si distribuirea gratuita in jocuri.

## Generat de cod (fara licenta externa)
- **Muzica de fundal**: generata procedural in `Scripts/Core/Sfx.cs` (`MusicGen`), cate o melodie chiptune pentru fiecare harta.
- **Iconitele de iteme** (sabie, coif, armura, inel, moneda, minereu): desenate procedural in `Scripts/UI/UIKit.cs` (`ItemIcons`).
- **Particule** (scantei, fum, stele, inele): texturi procedurale in `Scripts/Core/Art.cs`.

## Arhive originale
Arhivele descarcate sunt in folderul tau `Downloads` (`kenney_*.zip`). Am copiat in proiect doar fisierele folosite, ca proiectul sa ramana mic.
