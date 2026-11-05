# LadyRanias

Nettside for katteoppdrettet LadyRanias: avlsdyr, kull og kattunger, kontaktskjema og om oppdrettet.
Besøkende ser alt uten å logge inn. Oppdretter logger inn på en adminside og legger inn innholdet selv.

Bygget som læringsprosjekt etter mønsteret for en moderne .NET/React-fagapp: én .NET-prosess som
både er API og serverer React-appen, med data i Azure Storage og deploy via GitHub Actions.

## Arkitektur (målbilde)

```
Besøkende ──► domenet (DNS hos one.com) ──► Azure Container Apps
                                             └─ LadyRanias.App (.NET)
                                                ├─ React-appen (statiske filer)
                                                ├─ api/public/...  (åpent, kun lesing)
                                                ├─ api/admin/...   (krever innlogging)
                                                └─ bakgrunnsjobber (e-postvarsel)
                                                      │
                               Azure Table Storage ◄──┴──► Azure Blob Storage
                               (katter, kull, meldinger)     (bilder)
```

Domenet blir liggende hos one.com. Der kan det ikke kjøre .NET, så vi peker bare DNS mot Azure i steg 12.

## Plan

| Steg | Innhold | Status |
|---|---|---|
| 1 | Solution, App og Tests, felles byggeoppsett (`Directory.Build.props`), `.gitignore` | ✅ |
| 2 | GitHub: `main` som hovedgren, CI som bygger og tester hver PR (beskyttelse av `main` utsatt, privat repo) | ✅ |
| 3 | Komponent **Cats**: controller → handler → database (i minnet), regler for fødselsdato og kjønn, enhets- og komponenttester | ✅ |
| 4 | Lagring: Table Storage via Azurite lokalt og i CI, integrasjonstester | ✅ |
| 5 | Bilder: komponenten **CatImages** med Blob Storage, to størrelser i WebP uten metadata, sletting når katten slettes | ✅ |
| 6 | Frontend: React + TypeScript strict + Vite + Mantine, servert av appen, offentlige sider (forside, katter, katt, kontakt, 404) | ✅ |
| 7 | Komponent **Litters** (kull og kattunger): planlagt og født, kattunger ledig, reservert, solgt og beholdes, side for kull | ✅ |
| 8 | Admin: innlogging med Microsoft Entra External ID, rollen `Admin`, `api/admin` beskyttet, adminsider | ✅ |
| 9 | Kontaktskjema: validering, spambeskyttelse, kø og bakgrunnsjobb, e-postvarsel via Azure Communication Services | ✅ |
| 10 | Container-image (Dockerfile, chiseled), bygget på hver PR og publisert til Azure Container Registry fra `main` | Pågår |
| 11 | Deploy til **test** i Azure Container Apps, automatisk fra `main` | |
| 12 | Deploy til **prod** etter godkjenning, eget domene fra one.com, HTTPS | |
| 13 | Observerbarhet: logger, helseprober, varsler | |

## Utsatt til senere

**Cats**

- [ ] Farge (tekst, for eksempel «svart tabby» eller EMS-kode)
- [ ] Status: aktiv i avl eller pensjonert (enum, ikke `bool`)
- [ ] `GET api/public/cats` viser bare aktive katter, admin-lista viser alle
- [ ] `Description` valgfri (`string?`), og `[StringLength]` på tekstfeltene
- [ ] «Endre navn» byttes ut med full oppdatering: `PUT api/admin/cats/{id}` med `UpdateCatDto`

**Lagring**
- [ ] ETag: oppdateringer avvises hvis raden er endret av noen andre i mellomtiden, og API-et svarer 409 Conflict

## Valg underveis

- **Bildebehandling med SkiaSharp (MIT), ikke ImageSharp.** ImageSharp krever lisensnøkkel ved bygging fra versjon 3.2/4.0,
  og community-nøkler gis bare til åpne prosjekter.
- **Azurite startes med `--skipApiVersionCheck`** lokalt og i CI, fordi Azurite 3.37 ikke støtter API-versjonen til
  `Azure.Storage.Blobs`. Kan fjernes når en nyere Azurite er ute.
- **Komponenter snakker bare via `Api/`.** Når `CatImages` må reagere på at en katt slettes, skjer det via
  `ICatDeletedListener` i `Cats/Api`, slik at `Cats` ikke trenger å kjenne `CatImages`.
- **TypeScript låst til `~6.0`.** TypeScript 7 er ute, men `typescript-eslint` støtter den ikke ennå. Kan løftes når de gjør det.
- **Mantine som komponentbibliotek**, og **Pinyon Script** som skrivefont for overskrifter. Fonten ligger i appen via
  `@fontsource`, ikke hos Google, så besøkende ikke sendes til Google bare for å se siden (personvern).
- **All henting går via `useApi`** i `ClientApp/src/api/api.ts`: lasting, feil, 404 og avbrudd på ett sted.
- **`dotnet publish` bygger React-appen** (`npm ci` og `npm run build`) og tar med filene. CI har en egen jobb som
  kjører lint og bygg av frontend på hver PR.

---

## Steg 7: Kull og kattunger

**Mål:** Oppdretter kan legge inn et planlagt kull med mor og far, markere at det er født, og legge inn kattungene.
Hver kattunge er **ledig**, **reservert**, **solgt** eller **beholdes** (oppdretter beholder den selv). Besøkende ser planlagte kull og ledige kattunger på siden «Kull».

### Ny komponent: `Litters`
Samme oppbygning som `Cats`: `Api/`, `Impl/`, `Controllers/` og `LittersComponent.cs` med `UseLittersComponent()`.

**Avhengighet:** `Litters` trenger å vite at mor og far finnes, og hvilket kjønn de har. Derfor bruker `Litters`
`ICatHandler` fra `Cats/Api`. Det er lov: `Litters` kjenner `Cats`, men `Cats` vet ingenting om `Litters`.
Pilen går bare én vei. Gjenbruk også `SexEnum` fra `Cats/Api` for kattungene.

### Modellen

```
Litter (kull)                          Kitten (kattunge)
─────────────                          ─────────────────
Id                                     Id
Name          «A-kullet»               LitterId      → hvilket kull
MotherId      → en hunnkatt i Cats     Name
FatherId      → en hannkatt i Cats     Sex           Male | Female
Status        Planned | Born           Status        Available | Reserved | Sold | Kept
ExpectedDate  forventet fødsel         Description
BirthDate     null til kullet er født  CreatedAt
Description
CreatedAt
```

`LitterDto` tar med lista over kattunger, så frontend får hele kullet i ett kall.

### Tilstander og overganger

Et kull og en kattunge har en **status** som bare kan endres på bestemte måter. Det kalles en *tilstandsmaskin*.
Reglene samles på ett sted, så de kan testes alene.

**Kull:** `Planned → Born`. Ingen vei tilbake.

**Kattunge:**

| Fra \ Til | Available | Reserved | Sold | Kept |
|---|---|---|---|---|
| **Available** | – | ✅ | ✅ | ✅ |
| **Reserved** | ✅ (reservasjonen falt bort) | – | ✅ | ❌ |
| **Sold** | ❌ | ❌ | – | ❌ |
| **Kept** | ✅ (ombestemt seg) | ❌ | ❌ | – |

En reservert kattunge må først bli ledig igjen før oppdretter kan beholde den. Samme status til samme status (`–`) er ikke
en endring, og avvises også.

Lag en ren funksjon, for eksempel `KittenStatusRules.CanChange(KittenStatus from, KittenStatus to)`, uten database og
uten klokke. Da kan hele tabellen over testes med én `[TestMethod]` og 16 `[DataRow]` (alle kombinasjoner), slik du har gjort før.

### Del 1: Modell, regler og handler (i minnet)
- [ ] DTO-er i `Api/`: `LitterDto`, `KittenDto`, `CreateLitterDto`, `MarkBornDto`, `CreateKittenDto`, `ChangeKittenStatusDto`,
      og enumene `LitterStatus` og `KittenStatus`
- [ ] `ILitterHandler` i `Api/`, og `LitterHandler` i `Impl/` med `InMemoryLitterDatabase`
- [ ] Regler ved **opprettelse av kull**: mor og far må finnes (`ICatHandler.GetAsync`), mor må være hunnkatt og far hannkatt
- [ ] Regler ved **fødsel**: kullet må være `Planned`, og `BirthDate` kan ikke være i fremtiden (bruk `TimeProvider`)
- [ ] Regler for **kattunger**: kan bare legges til når kullet er `Born`, og statusendringer følger tabellen over

**To slags feil.** Til nå har feil input gitt `ValidationException` og 400 Bad Request. Nå kommer en ny type:
input er gyldig, men passer ikke med tilstanden, for eksempel å selge en kattunge som allerede er solgt.
Det er **409 Conflict**. Lag en egen exception, for eksempel `InvalidStateException`, så controlleren kan skille dem.

### Del 2: Endepunkter

| Metode | URL | Svar |
|---|---|---|
| GET | `api/public/litters` | alle kull, nyeste først, med kattunger |
| GET | `api/public/litters/{id}` | ett kull, eller 404 |
| POST | `api/admin/litters` | 201, eller 400 hvis mor/far mangler eller har feil kjønn |
| POST | `api/admin/litters/{id}/born` | 200, 404, 400 (dato i fremtiden) eller 409 (allerede født) |
| DELETE | `api/admin/litters/{id}` | 204 eller 404. Kattungene slettes også. |
| POST | `api/admin/litters/{id}/kittens` | 201, 404 eller 409 (kullet er ikke født) |
| PUT | `api/admin/litters/{litterId}/kittens/{kittenId}/status` | 200, 404 eller 409 (ulovlig overgang) |
| DELETE | `api/admin/litters/{litterId}/kittens/{kittenId}` | 204 eller 404 |

### Del 3: Lagring i Table Storage
- [ ] Tabellen `litters`: PartitionKey = `"litter"`, RowKey = litterId
- [ ] Tabellen `kittens`: **PartitionKey = litterId**, RowKey = kittenId, samme mønster som `catimages`.
      «Alle kattunger i kull X» blir ett raskt oppslag.
- [ ] Enumene lagres som tekst, ikke tall, så tabellen er lesbar i Storage Explorer
- [ ] Integrasjonstester mot Azurite

### Del 4: Frontend
- [ ] Ny lenke «Kull» i menyen, og siden `/kull`
- [ ] Øverst **planlagte kull**: navn, forventet tid, og mor og far som lenker til katteprofilene
- [ ] Under: **fødte kull** med kattungene som kort. Fargen på merket viser status: ledig (grønn), reservert (gul), solgt (grå), beholdes (lilla, «Blir hos oss»).
- [ ] Typer i `types.ts` og formatering av status i `format.ts`, akkurat som `sexLabel`

**Tester**
- [ ] Enhetstester: hele overgangstabellen, kjønn på mor og far, fødselsdato i fremtiden, kattunge i planlagt kull
- [ ] Komponenttest, hele løpet: lag mor og far → lag kull → marker født → legg til kattunge → reserver → selg →
      prøv å reservere igjen → 409
- [ ] Integrasjonstester: `TableLitterDatabase` mot Azurite

**Ferdig når**
- [ ] Du kan gjennomføre løpet over fra `.http`-fila og se kullet på `/kull`
- [ ] Alle tester er grønne lokalt og i CI
- [ ] Merget via PR

**Det kan være lurt å slå opp:** «state machine», `Conflict()` i ASP.NET Core,
«exception filter» eller `try/catch` i controller, `QueryAsync` med filter på PartitionKey.

**Utsatt:** bilder av kattunger, hva som skjer med et kull når mor eller far slettes, og kjøper eller pris på reservasjon
(personopplysninger, tas når vi har innloggin## Steg 8: Admin med Microsoft Entra External ID

**Mål:** Oppdretter logger inn med e-postadressen sin og en engangskode, uten passord, og legger inn katter, bilder, kull
og kattunger selv. Alt under `api/admin` krever innlogging **og** rollen `Admin`. Besøkende merker ingen forskjell.

**Hvorfor Entra og ikke Google?** Appen kjører i Azure, og Entra er det man møter i .NET-fagapper. *External ID* er
varianten for kunder og andre utenfor organisasjonen, i en egen **external tenant**. Den er gratis opp til 50 000 aktive
brukere i måneden.

### Slik virker det

```
1. Mamma trykker «Logg inn» på /admin
2. Nettleseren går til  /api/auth/login  →  appen sender henne videre til Entra (<tenant>.ciamlogin.com)
3. Hun skriver e-posten sin og får en engangskode på mail (passordet ser vi aldri, fordi det ikke finnes)
4. Entra sender henne tilbake til  /api/auth/signin-oidc  med et ID-token: hvem hun er, og hvilke roller hun har
5. Appen sjekker tokenet og lager en innloggings-cookie (HttpOnly, Secure, SameSite=Lax)
6. Fra nå sender nettleseren cookien med hver forespørsel. api/admin slipper henne inn hvis hun har rollen Admin.
```

Dette heter **OpenID Connect** (OIDC). Entra gjør *autentisering* (hvem er du?). Rollen `Admin` er *autorisasjon*
(får du lov?). Den styres også i Entra, så tilgang gis og fjernes i portalen, ikke i koden.

### Del 1: Oppsett i Entra (gjøres for hånd i portalen)

**a) External tenant**
- [ ] I Microsoft Entra admin center: lag en ny tenant av typen **External**, med navn og underdomene for eksempel `ladyranias`
      (gir `ladyranias.ciamlogin.com`). Den kobles til Azure-abonnementet ditt, men er en egen katalog.
- [ ] Bytt til den nye tenanten (tannhjulet eller «Switch directory») før resten

**b) App-registrering**
- [ ] **App registrations → New registration**: «LadyRanias», *Accounts in this organizational directory only*
- [ ] **Authentication → Add a platform → Web**, med redirect-URI-er:
  - `http://localhost:5173/api/auth/signin-oidc` (når du utvikler via Vite)
  - `http://localhost:5127/api/auth/signin-oidc` (når appen kjører alene)
  - Senere: `https://<domenet>/api/auth/signin-oidc` i steg 12
  - `http://localhost:5173/api/auth/signout-callback-oidc` (hit sender Entra brukeren etter utlogging)
- [ ] La *Front-channel logout URL* stå tom. Den krever HTTPS og trengs bare hvis Entra skal varsle appen om utlogging
      andre steder.
- [ ] **Certificates & secrets → New client secret.** Kopier *Value* med én gang, den vises bare én gang.
      Noter utløpsdatoen i kalenderen: hemmeligheter i Entra varer maks to år.
- [ ] Noter **Application (client) ID** og **Directory (tenant) ID** fra *Overview*

**c) Brukerflyt med engangskode**
- [ ] **External Identities → User flows → New user flow**: «Sign up and sign in», metode **Email one-time passcode**.
      Attributter: *Display Name*.
- [ ] Åpne brukerflyten → **Applications → Add application** → LadyRanias

**d) Rollen Admin**
- [ ] **App registrations → LadyRanias → App roles → Create app role**: navn «Admin», *Users/Groups*, value `Admin`
- [ ] La mamma (og deg) logge inn én gang via appen, slik at brukerne blir opprettet. Eller lag dem i **Users**.
- [ ] **Enterprise applications → LadyRanias → Users and groups → Add user/group**, og gi dere rollen Admin
- [ ] Hvem som helst kan registrere seg via brukerflyten, men uten rollen får de bare 403. Det er greit.

**e) Hemmeligheter lokalt**

Appen trenger fire verdier. Bare `ClientSecret` er egentlig hemmelig, men vi holder alle fire ute av git, så repoet
ikke er knyttet til én bestemt tenant:

```powershell
cd src/LadyRanias.App
dotnet user-secrets init
dotnet user-secrets set "AzureAd:Instance" "https://ladyranias.ciamlogin.com/"
dotnet user-secrets set "AzureAd:TenantId" "<directory (tenant) id>"
dotnet user-secrets set "AzureAd:ClientId" "<application (client) id>"
dotnet user-secrets set "AzureAd:ClientSecret" "<client secret value>"
```

User secrets ligger i profilen din på Windows, utenfor repoet, og kan aldri havne i git ved et uhell.
I Azure blir de *secrets* på Container App-en (steg 11).

**Hvorfor `/api/auth/signin-oidc` og ikke standard `/signin-oidc`?** Vite videresender bare `/api/...` til .NET.
Hvis Entra sender deg tilbake til `/signin-oidc` på port 5173, svarer Vite med React-appen, og innloggingen stopper.

### Del 2: Innlogging i backend
- [ ] Pakken `Microsoft.Identity.Web`
- [ ] `AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))`, med
      `CallbackPath = "/api/auth/signin-oidc"` og `SignedOutCallbackPath = "/api/auth/signout-callback-oidc"`
- [ ] En *policy* `Admin` som krever rollen `Admin`. Rollene kommer i ID-tokenet som claimet `roles`.
- [ ] For `api/...` skal svaret være **401** (ikke innlogget) og **403** (innlogget, men uten rollen), ikke en
      omdirigering til Entra. En `fetch` kan ikke følge en omdirigering til en innloggingsside.
      Unntaket er `api/auth/login`, som *skal* omdirigere.
- [ ] Endepunkter:

| Metode | URL | Hva |
|---|---|---|
| GET | `api/auth/login?returnUrl=/admin` | sender til Entra. Bare lokale `returnUrl` godtas, så ingen kan lage en lenke som sender til en annen side etterpå. |
| POST | `api/auth/logout` | logger ut både av appen (cookien) og av Entra |
| GET | `api/auth/me` | `{ name, email, isAdmin }`, eller 401 |

- [ ] Hvis `AzureAd`-verdiene mangler, skal appen fortsatt starte (CI og testene har dem ikke), men innlogging virker ikke.
      I produksjon skal den derimot feile tydelig.

### Del 3: Beskytt admin, og tilpass testene
- [ ] `[Authorize(Policy = "Admin")]` på alle admin-controllere: Cats, CatImages og Litters
- [ ] **Testene:** komponenttestene kaller `api/admin` og blir røde med en gang. Lag en `TestAuthHandler` som logger inn
      ut fra headere, for eksempel `X-Test-User: mamma@example.com` og `X-Test-Roles: Admin`, og registrer den i `ConfigureTestServices`.
- [ ] Nye tester:
  - [ ] admin uten innlogging gir **401**
  - [ ] innlogget uten rollen gir **403**
  - [ ] innlogget med rollen `Admin` slipper inn
  - [ ] `api/public` er fortsatt åpent
  - [ ] `api/auth/login?returnUrl=https://evil.example` avvises

### Del 4: Full oppdatering av katt (fra «Utsatt»)
- [ ] `PUT api/admin/cats/{id}` med `UpdateCatDto` (navn, kjønn, fødselsdato, beskrivelse), med de samme reglene som ved opprettelse
- [ ] «Endre navn» (`PUT .../change`) fjernes

### Del 5: Adminsider i frontend
- [ ] `/admin` viser «Logg inn» hvis du ikke er innlogget, «Du har ikke tilgang» hvis du mangler rollen, og ellers adminsidene.
      Lenken står **ikke** i menyen. Det er ikke sikkerhet, siden sikkerheten ligger i API-et, men besøkende trenger ikke se den.
- [ ] `useAuth()` som spør `api/auth/me`
- [ ] Hjelpefunksjoner i `api.ts` for `POST`, `PUT` og `DELETE`, som viser `detail` fra feilsvarene (ProblemDetails)
- [ ] **Katter:** liste, ny, rediger, slett (med bekreftelse), last opp og slett bilder
- [ ] **Kull:** nytt kull, marker født, ny kattunge, bytt status (bare lovlige valg vises), slett
- [ ] Datoer med `TextInput type="date"`, så vi slipper en ekstra datopakke

**Ferdig når**
- [ ] Mamma kan logge inn på `localhost:5173/admin` med engangskode og legge inn en katt med bilde, uten `.http`-fila
- [ ] `api/admin` gir 401 uten innlogging, også når du prøver fra `.http`-fila
- [ ] Ingen hemmeligheter i git (`git grep` etter ClientSecret-verdien finner ingenting)
- [ ] Alle tester er grønne lokalt og i CI
- [ ] Merget via PR

**Det kan være lurt å slå opp:** «Entra External ID ASP.NET Core sign in», `Microsoft.Identity.Web`,
`AddMicrosoftIdentityWebApp`, «app roles Entra», `RequireRole`, `OpenIdConnectEvents.OnRedirectToIdentityProvider`,
«open redirect», `Url.IsLocalUrl`, «dotnet user-secrets», `AuthenticationHandler<T>` (for testene).

**Unngå:** dokumentasjon om «Azure AD B2C». Det er forgjengeren. Den ligner, men oppsettet og adressene er annerledes.

---

e).

---

## Steg 9: Kontaktskjema

**Mål:** Besøkende kan sende en melding til oppdretter fra siden «Kontakt». Meldingen lagres, oppdretter får
e-postvarsel, og hun kan lese og svare på meldingene fra adminsiden. Skjemaet skal tåle spam og ikke miste meldinger,
selv om e-posttjenesten er nede en stund.

### Slik virker det

```
Besøkende ──POST api/public/contact──► ContactController
                                         │ 1. valider (felt, honeypot, rate limit)
                                         │ 2. lagre meldingen i tabellen «contactmessages»   (Status = New)
                                         │ 3. legg meldingens id i køen «contact-notifications»
                                         ▼
                                     202 Accepted   ◄── svarer med en gang, venter ikke på e-post

                      ┌──────── bakgrunnsjobb (BackgroundService) ────────┐
                      │ henter fra køen → leser meldingen → sender e-post │
                      │ OK:  sletter fra køen                             │
                      │ Feil: lar den ligge, prøver igjen senere          │
                      │ 5 feil på rad: flyttes til «...-poison»           │
                      └───────────────────────────────────────────────────┘
```

**Hvorfor kø, og ikke bare sende e-posten direkte i controlleren?**
- Besøkende slipper å vente på e-posttjenesten.
- Er e-posttjenesten nede, går meldingen ikke tapt. Den ligger i tabellen og i køen, og blir sendt senere.
- Det er samme mønster du vil møte i større systemer: lagre først, gjør det tunge arbeidet i bakgrunnen.

Køen er **Azure Queue Storage**, som ligger i samme Storage-konto som tabellene og bildene. Azurite har den allerede,
på port 10001.

### Ny komponent: `Contact`
Samme oppbygning som de andre: `Api/`, `Impl/`, `Controllers/` og `ContactComponent.cs`.

```
ContactMessage
──────────────
Id
Name           påkrevd, maks 100 tegn
Email          påkrevd, gyldig e-post, maks 200 tegn
Phone          valgfri, maks 30 tegn
Subject        valgfri, for eksempel «Kull våren 2027» eller navnet på en kattunge
Message        påkrevd, 10–4000 tegn
Status         New | Read | Answered
EmailSentAt    null til varselet er sendt
CreatedAt
```

### Del 1: Ta imot og lagre
- [ ] `ContactMessageDto`, `CreateContactMessageDto` (med `[Required]`, `[EmailAddress]`, `[StringLength]`)
- [ ] `IContactHandler` og `ContactHandler`, `IContactDatabase` med versjon i minnet og `TableContactDatabase`
- [ ] `POST api/public/contact` → **202 Accepted** (ikke 201: varselet er ikke sendt ennå, det skjer i bakgrunnen)
- [ ] Tabellen `contactmessages` med **én partisjon** (`"message"`), som for katter. Måned som partisjon ble vurdert,
      men da må man vite måneden for å slå opp én melding. Med noen titalls meldinger i året er det mer komplisert
      enn det er verdt (YAGNI). Sletting etter 12 måneder blir et filter på `CreatedAt`.

### Del 2: Spambeskyttelse
Uten innlogging kan hvem som helst sende, også roboter. Tre enkle lag, uten CAPTCHA:
- [ ] **Honeypot:** et skjult felt `Website` i skjemaet. Mennesker ser det ikke og lar det stå tomt, mens enkle roboter
      fyller ut alt. Er det fylt ut, svarer vi 202 som om alt gikk bra, men lagrer ingenting. Da lærer ikke roboten noe.
- [ ] **Rate limiting** med innebygd `AddRateLimiter`: for eksempel 5 meldinger per time per IP-adresse.
      Svarer **429 Too Many Requests**. IP-adressen kommer fra `X-Forwarded-For`, som vi satte opp i steg 8.
- [ ] **Lengdegrenser** på alle felt (over), så ingen kan sende en melding på 10 MB.

Holder ikke dette, kan vi legge til Cloudflare Turnstile senere. Det er en usynlig CAPTCHA uten bildeoppgaver.

### Del 3: Kø og bakgrunnsjobb
- [ ] `QueueServiceClient` (pakken `Azure.Storage.Queues`), køen `contact-notifications`
- [ ] Handleren legger meldingens **id** i køen, ikke hele meldingen. Køen skal ikke inneholde personopplysninger.
- [ ] `ContactNotificationWorker : BackgroundService`:
  - henter meldinger fra køen med `ReceiveMessagesAsync` og en *visibility timeout* (meldingen er skjult for andre mens vi jobber)
  - sender e-post via `IEmailSender`, setter `EmailSentAt`, og sletter meldingen fra køen
  - feiler det, gjør vi ingenting: meldingen blir synlig igjen etter timeouten og prøves på nytt
  - etter 5 forsøk (`DequeueCount`) flyttes den til køen `contact-notifications-poison` og logges som feil
  - venter litt mellom rundene når køen er tom, så den ikke spør Azure hundre ganger i sekundet
- [ ] Enhetstester av arbeideren med en falsk kø og en falsk `IEmailSender`, som feiler et bestemt antall ganger

### Del 4: E-post med Azure Communication Services
- [ ] `IEmailSender` i `Impl/`, med to versjoner:
  - `LogEmailSender`: skriver e-posten til loggen. Brukes lokalt, i tester og i CI.
  - `AcsEmailSender`: sender via Azure Communication Services (pakken `Azure.Communication.Email`)
- [ ] Hvilken som brukes, styres av konfigurasjonen, slik som med Entra i steg 8: finnes `Email:ConnectionString`,
      brukes ACS, og ellers loggen. I produksjon skal den mangle-sjekken feile tydelig.
- [ ] E-posten går til `Email:NotifyAddress` (mammas adresse, i user secrets og senere som secret i Azure)
- [ ] **Reply-To** settes til den besøkendes e-post, så mamma kan trykke «Svar» rett i e-postprogrammet
- [ ] Teksten i e-posten er ren tekst, ikke HTML. Det besøkende skriver, skal aldri tolkes som HTML.

**Oppsett i Azure** (gjøres for hånd, når del 1–3 virker):
- [ ] Opprett **Communication Services** og **Email Communication Services** i `rg-ladyranias`
- [ ] Legg til et **Azure-administrert domene** (`…azurecomm.net`) for å komme i gang. Eget domene fra one.com tas i steg 12.
- [ ] Koble domenet til Communication Services-ressursen
- [ ] Legg tilkoblingsstrengen og avsenderadressen i user secrets:
      `Email:ConnectionString`, `Email:SenderAddress`, `Email:NotifyAddress`

### Del 5: Frontend
- [ ] Kontaktsiden: skjema med navn, e-post, telefon (valgfri), emne og melding, og honeypot-feltet skjult med CSS
- [ ] «Takk, vi svarer så snart vi kan» etter sending, og tydelige feilmeldinger ved 400 og 429
- [ ] En kort tekst om hva opplysningene brukes til (personvern)
- [ ] Lenke «Spør om denne kattungen» fra kull-siden, som fyller ut emnet
- [ ] **Admin:** ny fane «Meldinger» med lista, status (ny, lest, besvart), og lenke `mailto:` for å svare

**Personvern**
- [ ] Meldinger slettes automatisk etter 12 måneder. Det kan gjøres i den samme bakgrunnsjobben, én gang i døgnet.
- [ ] Adminsiden kan slette en melding med en gang, hvis noen ber om det

**Ferdig når**
- [ ] En melding fra kontaktsiden havner i adminsiden, og lokalt står e-posten i loggen
- [ ] Med ACS satt opp kommer e-posten fram, og «Svar» går til den som skrev
- [ ] Honeypot, 429 og poison-køen er dekket av tester
- [ ] Alle tester er grønne lokalt og i CI
- [ ] Merget via PR

**Det kan være lurt å slå opp:** `BackgroundService`, `Azure.Storage.Queues`, `ReceiveMessagesAsync`,
«visibility timeout», «poison queue», `AddRateLimiter`, `RequireRateLimiting`, «honeypot spam»,
«Azure Communication Services Email .NET», `EmailClient.SendAsync`, «Reply-To».

---

## Steg 10: Container-image

**Mål:** Hele appen, altså .NET og den ferdigbygde React-appen, pakkes i ett lite og sikkert container-image.
Hver PR sjekker at imaget kan bygges. Hver merge til `main` bygger og publiserer imaget til Azure Container Registry,
klart til å kjøres i steg 11.

### Hva er et container-image?
En «pakke» med alt appen trenger for å kjøre: .NET-runtime, appen og React-filene. Imaget kjører likt på din maskin,
i CI og i Azure, uten at noe må installeres der. Et **registry** er et lager for images, omtrent som NuGet for pakker.

### Hvorfor «chiseled»?
Microsoft lager .NET-images i tre størrelser:

| Image | Størrelse | Inneholder |
|---|---|---|
| `aspnet:10.0` | ca. 220 MB | Ubuntu med skall, pakkebehandler og mange verktøy |
| `aspnet:10.0-noble-chiseled` | ca. 110 MB | Bare det .NET trenger. Ingen skall, ingen pakkebehandler, kjører som ikke-root. |
| `aspnet:10.0-noble-chiseled-extra` | ca. 125 MB | Som chiseled, pluss ICU (språk og datoformater) og tidssoner |

Mindre innhold betyr færre sikkerhetshull å oppdatere, og at en angriper som kommer seg inn, ikke har noe skall å jobbe i.
Vi bruker **`-extra`**, fordi appen formaterer datoer og kan trenge norske tidssoner.
SkiaSharp fungerer, fordi vi bruker `NativeAssets.Linux.NoDependencies` (valgt i steg 5).

### Del 1: Dockerfile med tre byggesteg (*multi-stage*)

```
┌─ 1. node:24 ───────────────┐   ┌─ 2. dotnet/sdk:10.0 ─────────┐   ┌─ 3. aspnet:10.0-noble-chiseled-extra ─┐
│ npm ci                      │   │ dotnet restore               │   │ bare den ferdige appen               │
│ npm run build  → wwwroot ───┼──►│ dotnet publish  → /app ──────┼──►│ kjører som bruker «app», port 8080   │
└─────────────────────────────┘   └──────────────────────────────┘   └──────────────────────────────────────┘
       kastes etterpå                    kastes etterpå                      dette er imaget
```

- [ ] `Dockerfile` i roten av repoet
- [ ] **Steg 1 (Node):** kopier først bare `package.json` og `package-lock.json`, kjør `npm ci`, og kopier så resten.
      Da kan Docker gjenbruke `npm ci` fra forrige bygg (*layer caching*) når bare koden er endret.
- [ ] **Steg 2 (.NET SDK):** samme triks: kopier `Directory.Build.props` og `.csproj` først, kjør `dotnet restore`, så resten.
      Hent `wwwroot` fra steg 1, og kjør `dotnet publish -c Release -o /app -p:SkipClientBuild=true`.
- [ ] **csproj:** publish-målet fra steg 6 kjører `npm ci` og `npm run build`. Det finnes ikke Node i SDK-imaget, og
      React er uansett bygget i steg 1. Legg til `Condition="'$(SkipClientBuild)' != 'true'"` på de to `Exec`-linjene.
- [ ] **Steg 3 (runtime):** `COPY --from=build /app .`, og `ENTRYPOINT ["dotnet", "LadyRanias.App.dll"]`.
      Chiseled-imaget kjører allerede som ikke-root og lytter på port **8080**.
- [ ] `.dockerignore`: `**/bin`, `**/obj`, `**/node_modules`, `**/wwwroot`, `.git`, `.vs`, `tests`, `.azurite`.
      Uten den sendes alt til Docker, også `node_modules`, og byggingen blir treg.

### Del 2: Kjør imaget lokalt
- [ ] Installer **Docker Desktop**, hvis du ikke har det. Du har kanskje allerede Azurite i Docker.
- [ ] `docker build -t ladyranias .` fra roten
- [ ] Kjør med Azurite på maskinen din. Inne i containeren betyr `localhost` containeren selv, så adressen til
      maskinen din er `host.docker.internal`:

```powershell
docker run --rm -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e ConnectionStrings__Storage="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://host.docker.internal:10000/devstoreaccount1;QueueEndpoint=http://host.docker.internal:10001/devstoreaccount1;TableEndpoint=http://host.docker.internal:10002/devstoreaccount1;" `
  ladyranias
```

Nøkkelen over er Azurites offentlige standardnøkkel, ikke en hemmelighet.
`Development` fordi `Production` krever Entra og e-post (steg 8 og 9), og stopper appen hvis de mangler.
Det er akkurat det vi vil i Azure, men ikke her.

- [ ] Åpne `http://localhost:8080`, og se at forsiden og kattene vises
- [ ] `docker images ladyranias` viser størrelsen. Den bør være rundt 130–150 MB.

`__` (dobbel understrek) i navnet på miljøvariabelen betyr `:` i konfigurasjonen, så `ConnectionStrings__Storage` blir
`ConnectionStrings:Storage`. Det er slik Azure gir appen innstillinger i steg 11.

### Del 3: Azure Container Registry og innlogging fra GitHub uten passord
- [ ] Opprett **Container registry** i `rg-ladyranias`: navn for eksempel `crladyranias` (må være unikt i hele Azure,
      bare små bokstaver og tall), plan **Basic**, region Norway East eller West Europe
- [ ] **GitHub skal logge inn i Azure uten passord**, med *OpenID Connect* (OIDC). GitHub lager et kortlevd token for
      hver kjøring. Azure stoler på det fordi vi har sagt hvilket repo og hvilken gren som er godkjent.
  - [ ] I **Default Directory** (ikke LadyRanias-tenanten fra steg 8): **App registrations → New registration**, `github-ladyranias`
  - [ ] **Certificates & secrets → Federated credentials → Add credential**: *GitHub Actions deploying Azure resources*,
        organization `Insionz`, repository `LadyRanias`, entity **Branch**, branch `main`
  - [ ] På registryet: **Access control (IAM) → Add role assignment → AcrPush** → `github-ladyranias`
  - [ ] I GitHub-repoet: **Settings → Secrets and variables → Actions → Variables**:
        `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `ACR_NAME`.
        Dette er id-er, ikke hemmeligheter. Ingen passord lagres noe sted.

### Del 4: CI bygger og publiserer
- [ ] **På hver PR:** jobb som kjører `docker build`, men ikke publiserer. Da oppdager vi med en gang om noen har ødelagt Dockerfile.
- [ ] **På `main`, etter at testene er grønne:** logg inn med `azure/login` (OIDC), og bygg og push to tagger:
  - `crladyranias.azurecr.io/ladyranias:<git-sha>`: fast, endres aldri. Det er denne vi deployer i steg 11.
  - `crladyranias.azurecr.io/ladyranias:main`: flyttes hver gang, så det er lett å se hva som er nyest.
- [ ] Jobben trenger `permissions: id-token: write` for å få OIDC-tokenet fra GitHub

**Ferdig når**
- [ ] `docker run` lokalt viser appen på `localhost:8080`
- [ ] PR-en viser en grønn jobb for `docker build`
- [ ] Etter merge ligger imaget i registryet under **Repositories → ladyranias**, med git-SHA som tag
- [ ] Ingen passord eller nøkler i GitHub (bare id-er under *Variables*)

**Det kan være lurt å slå opp:** «multi-stage build», «Docker layer caching», «.NET chiseled containers»,
`$APP_UID`, «host.docker.internal», «GitHub Actions OIDC Azure», `azure/login`, `az acr login`, «federated credential».

**Utsatt:** opprydding av gamle images (Basic har ingen automatisk opprydding), og sårbarhetsskanning av imaget.

---

Neste steg beskrives her når vi kommer dit.
