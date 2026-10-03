# Hueborn: Loot Legends – lansare pe Google Play (ghid pas cu pas)

Ultima actualizare: 2 octombrie 2026. Răspunsurile de mai jos sunt pentru starea actuală a jocului
(cont anonim Unity, cloud save, clasamente, chat între jucători, login Google Play opțional, fără reclame).
Dacă adaugi ceva nou (reclame, analytics, chat vocal etc.), actualizează formularele.

---

## 0. Ce TREBUIE rezolvat înainte de lansare (blocante)

1. **Android Build Support nu e instalat.** Unity Hub → Installs → Unity 6 → ⚙ → Add modules →
   bifează *Android Build Support* + *OpenJDK* + *Android SDK & NDK Tools* → Install.
2. ~~Magazinul cu prețuri în $ dă obiectele gratis.~~ **REZOLVAT:** plata merge prin Google Play Billing (Unity IAP 5, `Scripts/Online/Shop.cs`). Trebuie create produsele în Play Console → Monetize → Products cu aceste id-uri: pass_magnet, pass_auto, pass_teleport, pass_pets, pass_private, pass_all (one-time / non-consumable); bag_20, bag_40, bag_100 (one-time); tokens_1..3, coins_1..3, shards_1..3 (consumable). Fostele note: Google Play cere ca orice conținut digital vândut
   pe bani reali să treacă prin **Google Play Billing**. Variante:
   - (a) integrăm Unity IAP / Play Billing (pas următor recomandat), sau
   - (b) la prima versiune scoatem prețurile în $ și facem pachetele cumpărabile cu monede din joc.
   Un buton „$1.99” care nu încasează nimic e respins la review.
3. ~~Raportare + blocare în chat.~~ **REZOLVAT:** butonul roșu „!” din chat → BLOCK / REPORT (raportul pleacă pe email la studio). Fostele note: Jocurile cu chat între jucători trebuie să aibă în aplicație
   un mod de a **raporta** și de a **bloca** un jucător. Se adaugă odată cu chatul real (Vivox).
4. **Pagina cu politica de confidențialitate trebuie să fie publică** (vezi secțiunea 2).

---

## 1. Semnarea aplicației și fișierul .aab

**Cheia de upload (gata):** `Keystore/colorrpg-upload.keystore`, alias `colorrpg`.
Parola e în `Keystore/keystore_info.txt` (Unity o citește automat la build).

- **Fă backup** la tot folderul `Keystore/` (stick USB + Google Drive privat). Fără el nu mai poți
  publica update-uri decât cu o procedură de resetare la Google, care durează zile.
- **Nu urca folderul `Keystore/` pe GitHub** sau în altă parte publică.
- Amprente (le poți cere în Firebase / Google Play Games Services):
  - SHA-1: `2F:84:65:52:EE:82:4C:F0:6B:0D:D5:70:C6:27:D7:D4:A6:83:4B:31`
  - SHA-256: `51:3A:28:EF:0A:57:6D:8D:1D:27:2F:58:8C:C6:9D:50:74:67:6A:B3:46:0F:2D:FE:AF:C7:83:F9:55:1C:7F:BA`

**Build:** în Unity → meniul **ColorRPG → Build Android (.aab for Google Play)**.
Rezultatul: `Builds/Android/Hueborn_<versiune>_<cod>.aab`. Fiecare build crește automat
`versionCode` (Google Play nu acceptă de două ori același cod).

**Play App Signing:** la primul upload, Play Console îți propune „Let Google manage and protect
your app signing key” → **acceptă**. Google semnează aplicația finală; tu semnezi doar cu cheia
de upload de mai sus. Dacă pierzi cheia de upload, Google o poate reseta.

Setări puse automat la build: pachet `com.cxstudio.hueborn` (nu se mai poate schimba după primul upload), nume sub iconiță „Hueborn”, IL2CPP + ARM64, minim Android 8 (API 26),
target „cel mai nou instalat”. Din 31 aug 2026 Google cere **target API 36** pentru aplicații noi: dacă la upload primești eroare de target API, instalează platforma Android 16 (API 36) din Android SDK Manager.

---

## 2. Politica de confidențialitate

- Pagina: artefactul **Hueborn Privacy Policy** (linkul e în conversație și în joc, Settings → PRIVACY POLICY).
  Ca s-o poată deschide oricine: pe pagină → **Share** → acces public prin link.
- Variantă de rezervă (recomandată pe termen lung): `Docs/privacy-site/index.html` se poate urca pe
  **GitHub Pages** sau **Google Sites** (gratuit). Dacă schimbi adresa, actualizează constanta
  `SettingsUI.PrivacyUrl` din `Scripts/UI/Windows.cs` și linkul din Play Console.
- Link pentru „Delete account URL” în Play Console: aceeași pagină, secțiunea **#delete**.
- În joc: **Settings → DELETE MY DATA** (apăsat de 2 ori) șterge contul online, cloud save-ul,
  clasamentele și progresul local.

---

## 3. Formularul „Data safety” (Play Console → Policy → App content → Data safety)

**Întrebări generale**
| Întrebare | Răspuns |
|---|---|
| Does your app collect or share any of the required user data types? | **Yes** |
| Is all of the user data collected by your app encrypted in transit? | **Yes** |
| Do you provide a way for users to request that their data is deleted? | **Yes** (în joc + pagina web) |
| Account creation | „My app allows users to create an account” → **Username and other authentication** (cont anonim creat automat) + **OAuth** (Google Play Games) |
| Delete account URL | linkul paginii de confidențialitate + `#delete` |

**Tipuri de date** (pentru toate: *Collected = Yes*, *Shared = No* — Unity și Google sunt furnizori de
servicii, iar Google spune că transferul către furnizori de servicii nu se declară ca „shared”;
*Processed ephemerally = No*)

| Categorie → tip | Ce e la noi | Obligatoriu? | Scopuri |
|---|---|---|---|
| Personal info → **Name** | numele eroului (vizibil altor jucători) | Required | App functionality, Account management |
| Personal info → **User IDs** | Player ID Unity, ID Google Play Games | Required | App functionality, Account management, Fraud prevention & security |
| Messages → **Other in-app messages** | chat între jucători | Optional | App functionality, Fraud prevention & security |
| App activity → **Other actions** | progres, scoruri, timp jucat | Required | App functionality |
| App activity → **App interactions** | multiplayer: pozitia eroului, miscarile, atacurile, loot-ul luat (trimise jucatorilor din aceeasi camera prin Unity Relay) | Required | App functionality |
| App activity → **Other user-generated content** | anunțuri din booth (prețuri puse de jucător) | Optional | App functionality |
| Device or other IDs → **Device or other IDs** | Player ID (așa recomandă Unity pentru Cloud Save) | Required | App functionality |
| Financial info → **Purchase history** | **doar după ce adăugăm Google Play Billing** | Optional | App functionality |

**NU** bifa: location, contacts, email, phone, photos/videos, audio, files, calendar, health,
web browsing, crash logs/diagnostics (nu folosim Crashlytics/Analytics), advertising ID.

---

## 4. Ratingul de conținut (App content → Content ratings, chestionar IARC)

- Email de contact: cxstudiodev@gmail.com · Categorie: **Game**
| Întrebare (pe scurt) | Răspuns |
|---|---|
| Violență | **Yes** – violență fantasy / cartoon împotriva monștrilor și a creaturilor fantastice; fără sânge realist, fără violență împotriva oamenilor în mod realist |
| Sânge / gore | No |
| Frică / horror | No (cel mult boși „fioroși” în stil cartoon → poți răspunde No) |
| Conținut sexual / nuditate | No |
| Limbaj vulgar | No (în conținutul jocului) |
| Droguri / alcool / tutun | No (poțiunile sunt magice) |
| Jocuri de noroc reale / simulate | No |
| **Users can interact or exchange content** (chat, trade) | **Yes** |
| Share user's location | No |
| **Digital purchases** | **Yes** (după Billing) |
| Random items for real money (loot boxes) | **Yes** dacă ouăle cu animale/obiecte aleatorii se pot cumpăra direct sau indirect cu bani reali (monede cumpărate cu $). Altfel No |
| Unrestricted internet / browser | No |

Rezultat probabil: PEGI 7 sau 12, ESRB Everyone 10+, cu notele *Users Interact* și
*In-Game Purchases (Includes Random Items)*. Chatul nu ridică vârsta, dar adaugă eticheta „Users Interact”.

---

## 5. Restul secțiunii „App content”

| Secțiune | Răspuns |
|---|---|
| Privacy policy | URL-ul public al paginii |
| Ads | **No, my app does not contain ads** |
| App access | **All functionality is available without special access** (nu trebuie cont/parolă) |
| Target audience | **13–15, 16–17, 18+** (NU bifa sub 13 – altfel intri în Families Policy, cu reguli mult mai stricte pentru chat) |
| Appeal to children? | No |
| News app | No |
| Government app / Financial features / Health | No |
| Data safety | secțiunea 3 |
| Content rating | secțiunea 4 |

---

## 6. Store listing (Main store listing)

- **App name:** Hueborn: Loot Legends (21 caractere din 30)
- **Short description** (max 80): `Fight monsters, hatch pets, trade with players and climb the leaderboards!`
- **Full description:** descrie hărțile, clasele + awakening, animalele, booth-ul de trade, titlurile, clasamentele.
- **Grafică:** icon 512×512 PNG · feature graphic 1024×500 · minim 2 screenshot-uri telefon
  (avem 3 în `Promo/`, 1920×1080) · video: urcă `ColorRPG_GooglePlay_Promo_1920x1080.mp4` pe YouTube (Unlisted) și pune linkul.
- **Categorie:** Game → Role Playing · Tags: RPG, Action RPG, Casual.
- **Contact:** cxstudiodev@gmail.com (obligatoriu, e public).

---

## 7. Testarea închisă (obligatorie pentru conturile personale noi)

1. Testing → **Internal testing**: urcă `.aab`, adaugă-te pe tine, instalează din linkul de test, verifică.
2. Testing → **Closed testing**: creează un track, adaugă **minim 12 testeri** (lista de emailuri Gmail sau un Google Group).
3. Testerii trebuie să rămână înscriși **14 zile la rând**. Ține jocul actualizat și cere-le feedback.
4. După 14 zile: Dashboard → **Apply for production** (răspunzi la câteva întrebări despre test).
5. Production → Create release → același `.aab` (sau unul nou) → review (de obicei 1–7 zile).

---

## 8. Ce urmează tehnic (în ordinea recomandată)

1. Google Play Billing (Unity IAP) – înlocuiește butoanele cu $.
2. Chat real (Vivox) + Report / Block.
3. Legarea contului de Google Play Games (să nu pierzi progresul la schimbarea telefonului).
4. Trading verificat pe server (Cloud Code), ca să nu se poată trișa.
5. Adaugă Player ID-ul de pe telefonul tău la `AdminAccess.AdminIds`.
