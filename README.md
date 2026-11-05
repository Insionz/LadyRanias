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
| 10 | Container-image (Dockerfile, chiseled), bygget på hver PR og publisert til Azure Container Registry fra `main` | ✅ |
| 11 | Deploy til **test** i Azure Container Apps med Bicep, automatisk fra `main`, managed identity mot Storage og registry | ✅ |
| 12 | Deploy til **prod** med manuell knapp, eget domene `www.ladyranias.no` fra one.com, gratis HTTPS-sertifikat | Pågår |
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

**Tester**
- [ ] Komponenttestene (`*ApiTests`) skal ikke trenge Azurite. En felles `UseInMemoryStorage()` i testprosjektet bytter
      ut alle databaser, bildelageret og køen med versjonene i minnet. I dag trenger de Azurite ved et uhell, blant annet
      fordi bakgrunnsjobben for e-post starter i alle testene og oppretter køen.
- [ ] Integrasjonstestene starter Azurite selv med **Testcontainers** (Docker), så ingen trenger å huske å starte den,
      og `docker run`-steget i CI kan fjernes

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
      Hent `wwwroot` fra steg 1, og kjør `dotnet publish -c Release -o /app -r linux-x64 --self-contained false -p:SkipClientBuild=true`.
      **`-r linux-x64`** (også på `restore`) tar bare med native filer for Linux. Uten den ble imaget nesten tre ganger så stort.
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
- [ ] Opprett **Container registry** i `rg-ladyranias`: navn for eksempel `crladyranias` (bare små bokstaver og tall),
      plan **Basic**, region Norway East eller West Europe
- [ ] **Domain name label scope: Tenant Reuse.** Azure legger til en fast hash i adressen
      (`crladyranias-<hash>.azurecr.io`), så ingen andre kan ta over navnet hvis registryet slettes en dag.
      Den fulle adressen står under **Overview → Login server**.
- [ ] **GitHub skal logge inn i Azure uten passord**, med *OpenID Connect* (OIDC). GitHub lager et kortlevd token for
      hver kjøring. Azure stoler på det fordi vi har sagt hvilket repo og hvilken gren som er godkjent.
  - [ ] I **Default Directory** (ikke LadyRanias-tenanten fra steg 8): **App registrations → New registration**, `github-ladyranias`
  - [ ] **Certificates & secrets → Federated credentials → Add credential**: *GitHub Actions deploying Azure resources*,
        organization `Insionz`, repository `LadyRanias`, entity **Branch**, branch `main`
  - [ ] På registryet: **Access control (IAM) → Add role assignment → AcrPush** → `github-ladyranias`
  - [ ] Og rollen **Reader** på registryet, også til `github-ladyranias`. `az acr login --name` slår opp den fulle
        adressen (med hash) via Azure, og AcrPush alene gir ikke lov til å se registryet som ressurs.
  - [ ] I GitHub-repoet: **Settings → Secrets and variables → Actions → Variables**:
        `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `ACR_NAME` og `ACR_LOGIN_SERVER` (hele adressen).
        Dette er id-er, ikke hemmeligheter. Ingen passord lagres noe sted.

### Del 4: CI bygger og publiserer
- [ ] **På hver PR:** jobb som kjører `docker build`, men ikke publiserer. Da oppdager vi med en gang om noen har ødelagt Dockerfile.
- [ ] **På `main`, etter at testene er grønne:** logg inn med `azure/login` (OIDC), og bygg og push to tagger:
  - `<ACR_LOGIN_SERVER>/ladyranias:<git-sha>`: fast, endres aldri. Det er denne vi deployer i steg 11.
  - `<ACR_LOGIN_SERVER>/ladyranias:main`: flyttes hver gang, så det er lett å se hva som er nyest.
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

## Steg 11: Deploy til test i Azure Container Apps

**Mål:** Appen kjører på nettet, på en adresse som `https://ca-ladyranias-test.<noe>.norwayeast.azurecontainerapps.io`.
Hver merge til `main` deployer den nye versjonen automatisk. Ingen passord til Storage eller registry finnes noe sted.

### Slik henger det sammen

```
GitHub Actions ──push──► Container Registry ◄──henter image── Container App «ca-ladyranias-test»
     │                                          (AcrPull)              │  identitet: id-ladyranias-test
     └── az containerapp update --image …:<sha> ──────────────────────►│
                                                                       ├──► Storage (Table, Blob, Queue)
Besøkende ──HTTPS──► Azure-proxy (TLS) ──HTTP──► appen på port 8080     ├──► Communication Services (e-post)
                                                                       └──► Entra (innlogging)
```

**Nye begreper:**
- **Container Apps-miljø** (`cae-ladyranias`): et felles «nabolag» for container-apper, med nettverk og logging.
  Test og prod kan dele samme miljø.
- **Managed identity** (`id-ladyranias-test`): en identitet Azure gir appen. Appen logger inn i Storage og registryet *som seg
  selv*, uten passord eller nøkler. Det er samme idé som OIDC for GitHub i steg 10, bare for appen.
- **Secrets på Container App-en:** det som *må* være hemmelig (Entra client secret, ACS-tilkoblingen), lagres kryptert
  på appen og gis til den som miljøvariabler. Aldri i imaget og aldri i git.
- **Skalering til null:** test kjører med `min-replicas 0`. Er det ingen besøkende, slås appen av og koster nesten ingenting.
  Første besøk etter en pause tar noen sekunder (*cold start*). Bakgrunnsjobben for e-post kjører bare mens appen er på,
  så et varsel kan bli liggende i køen til neste besøk. Det er greit i test. Prod får `min-replicas 1` i steg 12.

**Hva koster det?** Container Apps har en gratis kvote per måned som en liten app med null i minimum holder seg godt
innenfor. Storage koster noen kroner. Log Analytics er gratis opp til 5 GB logger i måneden. Det som faktisk koster,
er registryet (Basic, ca. 50 kr/mnd) fra steg 10.

### Del 1: Kode
- [ ] **Storage med managed identity.** I dag lager hver komponent sin egen klient fra en tilkoblingsstreng. Samle det i
      én `UseStorage()` som brukes av alle komponentene:
  - finnes `ConnectionStrings:Storage` (lokalt med Azurite, i tester og i CI): bruk den, som i dag
  - ellers `Storage:AccountName`: lag klientene med adresser (`https://<konto>.table.core.windows.net` osv.) og
    `DefaultAzureCredential`, som finner appens managed identity automatisk
  - pakken `Azure.Identity`
- [ ] **Forwarded headers i Azure.** I steg 8 stolte appen bare på proxyer på samme maskin (Vite). Azure-proxyen har en
      annen adresse. Når `ForwardedHeaders:TrustAllProxies` er `true`, tøm lista over kjente proxyer og nettverk, så
      `X-Forwarded-Proto: https` og den ekte IP-adressen blir brukt. Det er trygt i Container Apps, fordi appen bare kan
      nås via Azure-proxyen. Uten dette tror appen at den kjører på `http`, og Entra avviser innloggingen.

### Del 2: Infrastruktur som kode med Bicep
Alt som skal finnes i Azure, beskrives i **`infra/main.bicep`**. Verdiene for test ligger i **`infra/test.bicepparam`**.
Bicep beskriver hvordan ting *skal være*. Kjører du den på nytt, endres bare det som er annerledes, og prod (steg 12)
blir samme fil med en annen parameterfil.

`main.bicep` lager:

| Ressurs | Navn | Felles eller per miljø |
|---|---|---|
| Log Analytics | `log-ladyranias` | Felles |
| Container Apps-miljø | `cae-ladyranias` | Felles |
| Storage-konto, uten nøkler | `stlrtest…` (med hash, må være unikt) | Per miljø |
| Managed identity | `id-ladyranias-test` | Per miljø |
| Roller for identiteten | AcrPull på registryet, Table/Blob/Queue Data Contributor på storage | Per miljø |
| Container App | `ca-ladyranias-test` | Per miljø |
| Rolle for GitHub | Contributor på Container App-en | Per miljø |

**Forberedelser**
- [ ] Installer Azure CLI: `winget install Microsoft.AzureCLI`, og åpne en ny terminal. `az bicep install` henter Bicep.
- [ ] `az login`, og sjekk med `az account show` at riktig abonnement er valgt
- [ ] Fyll inn verdiene i `infra/test.bicepparam`. Object id til `github-ladyranias` finner du med
      `az ad sp show --id <AZURE_CLIENT_ID> --query id -o tsv`
- [ ] Anbefalt: utvidelsen **Bicep** i VS Code eller Visual Studio, som gir hjelp mens du skriver og viser feil

**Deploy** (PowerShell, fra roten av repoet):

```powershell
# Hemmelighetene settes bare i denne terminalen. De lagres ikke i noen fil, og -MaskInput viser dem ikke.
$env:ENTRA_CLIENT_SECRET     = Read-Host "Entra client secret" -MaskInput
$env:EMAIL_CONNECTION_STRING = Read-Host "ACS connection string" -MaskInput
$env:EMAIL_NOTIFY_ADDRESS    = Read-Host "Adresse for e-postvarsler"

# Se hva som vil skje, uten å endre noe
az deployment group what-if -g rg-ladyranias -f infra/main.bicep -p infra/test.bicepparam

# Gjør det
az deployment group create -g rg-ladyranias -f infra/main.bicep -p infra/test.bicepparam
```

`what-if` er en av de store fordelene med IaC: du ser nøyaktig hva som blir laget, endret eller slettet, før det skjer.

Til slutt skriver deployen ut `url`, adressen til appen.

**Første gang** kan appen bruke et minutt eller to på å starte, fordi rollen som lar den hente imaget, må spre seg i Azure.
Står revisjonen som feilet under **Container App → Revisions and replicas**, kjør `az deployment group create` én gang til.

**Når CI har deployet en ny versjon** (del 4), peker appen på en commit-SHA. Kjører du Bicep på nytt, settes imaget
tilbake til `main`-taggen. Det er det samme imaget hvis ingenting er merget siden, men vær klar over det.

**Hvorfor `max-replicas 1`?** Grensen på fem kontaktmeldinger i timen og bakgrunnsjobben ligger i minnet til én instans.
Med to instanser ville hver hatt sin egen grense. For denne appen er én instans nok.

**Hvorfor `Production` også i test?** Test skal oppføre seg som prod. Mangler en innstilling, stopper appen ved oppstart
(steg 8 og 9), og det er bedre å oppdage i test enn i prod.

### Del 3: Innlogging på test-adressen
- [ ] I **LadyRanias-tenanten** → app-registreringen **LadyRanias** → **Authentication**: legg til
  - `https://<fqdn>/api/auth/signin-oidc`
  - `https://<fqdn>/api/auth/signout-callback-oidc`
- [ ] Logg inn på `https://<fqdn>/admin`. Brukeren din i tenanten er den samme som lokalt, så du har allerede rollen.

### Del 4: CI deployer automatisk
- [ ] Rollen **Contributor** på Container App-en for `github-ladyranias` er allerede laget av Bicep (`githubDeploy`).
      Den gjelder bare denne ene appen, ikke ressursgruppen eller abonnementet.
- [ ] GitHub-variabler: `RESOURCE_GROUP` = `rg-ladyranias`, `CONTAINER_APP_TEST` = `ca-ladyranias-test`
- [ ] Nytt steg i `image`-jobben, etter publisering, bare på `main`:

```yaml
    - name: Deploy til test
      if: github.event_name == 'push' && github.ref == 'refs/heads/main'
      run: |
        az config set extension.use_dynamic_install=yes_without_prompt
        az containerapp update \
          --name ${{ vars.CONTAINER_APP_TEST }} \
          --resource-group ${{ vars.RESOURCE_GROUP }} \
          --image ${{ vars.ACR_LOGIN_SERVER }}/ladyranias:${{ github.sha }}
```

Vi deployer **SHA-taggen**, ikke `main`. Da vet vi nøyaktig hvilken commit som kjører, og en ny revisjon lages hver gang.
Container Apps beholder de gamle revisjonene, så det er lett å gå tilbake.

**Ferdig når**
- [ ] Forsiden vises på `https://<fqdn>`, med hengelås i nettleseren
- [ ] Du kan logge inn på `/admin`, legge inn en katt med bilde og se den på `/katter`
- [ ] Kontaktskjemaet gir e-postvarsel (husk at appen kanskje må vekkes først, se «Skalering til null»)
- [ ] En merge til `main` gir en ny revisjon automatisk (**Container App → Revisions and replicas**)
- [ ] Ingen nøkler til Storage eller registry finnes noe sted

**Det kan være lurt å slå opp:** «Bicep fundamentals», `existing`, `bicepparam`, `readEnvironmentVariable`,
«az deployment group what-if», «Azure Container Apps managed identity», `DefaultAzureCredential`,
«Storage Table Data Contributor», «secretRef», «scale to zero», «Container Apps revisions»,
`ForwardedHeadersOptions.KnownIPNetworks`.

**Utsatt:** deploy av Bicep fra CI (i dag kjøres den for hånd), eget domene og helseprober (steg 12 og 13).

---

## Steg 12: Prod med eget domene

**Mål:** Den ekte nettsiden kjører på **https://www.ladyranias.no**. Prod har egne data, atskilt fra test.
En ny versjon går til prod bare når du trykker på en knapp, og da er det **nøyaktig det samme imaget** som er testet i test.

```
merge til main ──► CI: test, bygg, publiser ──► test (automatisk)
                                                     │
                                       du sjekker test, og trykker «Run workflow»
                                                     ▼
                                          prod (samme image, samme SHA)
                                                     │
                       www.ladyranias.no ──DNS hos one.com──► Container App «ca-ladyranias-prod»
                       ladyranias.no ──────────────────────► (301 til www)
```

**Hvorfor samme image?** Det som er testet, er et bestemt image med en bestemt SHA. Bygger man på nytt for prod,
er det i teorien et annet image. Vi bygger én gang og flytter det samme imaget videre. Det heter *build once, deploy many*.

**Hvorfor en knapp, og ikke godkjenning i et GitHub-environment?** *Environments* med *required reviewers* er den vanlige
måten i bedrifter, men i private repoer krever det GitHub Pro eller Team. En manuell workflow (`workflow_dispatch`) gir
det samme: ingenting går til prod uten at du har bestemt det.

### Del 0: Før du rører domenet
- [ ] **Hva ligger på ladyranias.no i dag?** Har moren din en nettside der (for eksempel laget i one.com sin nettsidebygger),
      forsvinner den når DNS peker til Azure. Ta vare på tekster og bilder som skal over til den nye siden.
- [ ] **E-post på domenet** (for eksempel `post@ladyranias.no` hos one.com) påvirkes **ikke**, så lenge du bare endrer
      oppføringene for nettsiden. **Ikke rør `MX`-oppføringene**, og ikke slett eksisterende `TXT`-oppføringer.
- [ ] Logg inn i kontrollpanelet hos one.com, og finn **DNS-innstillinger** for domenet. Ta et skjermbilde av slik det er nå.

### Del 1: Prod-miljøet med Bicep
- [ ] `infra/prod.bicepparam`: samme som test, men `environmentName = 'prod'`, `minReplicas = 1` og
      `canonicalHost = 'www.ladyranias.no'`
- [ ] `main.bicep` har fått to nye parametere:
  - `canonicalHost`: domenet uten www sendes til denne adressen (301). Koden ligger i `Program.cs`.
  - `customDomains`: domenene og sertifikatene deres. **Uten dette ville neste deploy av Bicep fjernet domenene fra appen**,
    fordi Bicep beskriver hele appen, og domener som ikke står der, regnes som «skal ikke være der».
- [ ] Deploy, med **mammas** e-postadresse som `EMAIL_NOTIFY_ADDRESS`:

```powershell
$env:ENTRA_CLIENT_SECRET     = Read-Host "Entra client secret" -MaskInput
$env:EMAIL_CONNECTION_STRING = Read-Host "ACS connection string" -MaskInput
$env:EMAIL_NOTIFY_ADDRESS    = Read-Host "Adresse for e-postvarsler (mamma)"

az deployment group what-if -g rg-ladyranias -f infra/main.bicep -p infra/prod.bicepparam
az deployment group create  -g rg-ladyranias -f infra/main.bicep -p infra/prod.bicepparam
```

`what-if` skal vise ny storage-konto, identitet, roller og `ca-ladyranias-prod`, og **ingen endring** på det som er felles
(`log-ladyranias`, `cae-ladyranias`) eller på test. Prod starter med tomme tabeller. Test-dataene blir i test.

### Del 2: Knappen som deployer til prod
- [ ] GitHub-variabel `CONTAINER_APP_PROD` = `ca-ladyranias-prod`
- [ ] Ny fil `.github/workflows/deploy-prod.yml`:

```yaml
name: Deploy til prod

on:
  workflow_dispatch:
    inputs:
      sha:
        description: 'Commit-SHA som skal i prod. Tom = siste commit på main (den som nettopp gikk til test).'
        required: false

permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - name: Velg versjon
      id: version
      run: echo "sha=${{ inputs.sha || github.sha }}" >> "$GITHUB_OUTPUT"

    - name: Logg inn i Azure (OIDC)
      uses: azure/login@v2
      with:
        client-id: ${{ vars.AZURE_CLIENT_ID }}
        tenant-id: ${{ vars.AZURE_TENANT_ID }}
        subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}

    # Stopp hvis imaget ikke finnes, i stedet for å lage en prod-versjon som ikke kan starte
    - name: Sjekk at imaget finnes
      run: |
        az acr repository show-tags --name ${{ vars.ACR_NAME }} --repository ladyranias \
          --query "[?@=='${{ steps.version.outputs.sha }}']" -o tsv | grep -q . \
          || { echo "Fant ikke ladyranias:${{ steps.version.outputs.sha }} i registryet"; exit 1; }

    - name: Deploy til prod
      run: |
        az config set extension.use_dynamic_install=yes_without_prompt
        az containerapp update \
          --name ${{ vars.CONTAINER_APP_PROD }} \
          --resource-group ${{ vars.RESOURCE_GROUP }} \
          --image ${{ vars.ACR_LOGIN_SERVER }}/ladyranias:${{ steps.version.outputs.sha }}
```

Workflowen kjører fra `main`, så GitHub-innloggingen bruker samme *federated credential* som før. Rollen som lar GitHub
oppdatere prod-appen, lager Bicep (`githubDeploy`). Under **Actions → Deploy til prod → Run workflow** finner du knappen.

### Del 3: Domenet
Container Apps gir **gratis HTTPS-sertifikat** som fornyes automatisk (*managed certificate*). For å få det må Azure
kunne se at du eier domenet, og domenet må peke til appen.

- [ ] Hent adressen til miljøet og verifiseringskoden:

```powershell
az containerapp env show -n cae-ladyranias -g rg-ladyranias `
  --query "{ip:properties.staticIp, kode:properties.customDomainConfiguration.customDomainVerificationId}" -o table
az containerapp show -n ca-ladyranias-prod -g rg-ladyranias --query properties.configuration.ingress.fqdn -o tsv
```

- [ ] **DNS hos one.com.** Legg til (eller endre) disse, og la alt annet være:

| Type | Navn (vert) | Verdi | Hvorfor |
|---|---|---|---|
| `CNAME` | `www` | `ca-ladyranias-prod.<…>.norwayeast.azurecontainerapps.io` | `www` peker til appen |
| `TXT` | `asuid.www` | verifiseringskoden | beviser at du eier `www.ladyranias.no` |
| `A` | `@` (domenet uten noe foran) | IP-adressen til miljøet | domenet uten www peker til miljøet |
| `TXT` | `asuid` | verifiseringskoden | beviser at du eier `ladyranias.no` |

Finnes det allerede en `A`-oppføring for `@` eller en `CNAME`/`A` for `www` (til one.com sin nettside), skal den **endres**,
ikke legges til ved siden av. DNS-endringer kan bruke fra noen minutter til noen timer på å slå gjennom.
`nslookup www.ladyranias.no` viser når den peker riktig.

- [ ] **Koble domenene til appen og lag sertifikatene:**

```powershell
az containerapp hostname add  -n ca-ladyranias-prod -g rg-ladyranias --hostname www.ladyranias.no
az containerapp hostname bind -n ca-ladyranias-prod -g rg-ladyranias --hostname www.ladyranias.no `
  --environment cae-ladyranias --validation-method CNAME

az containerapp hostname add  -n ca-ladyranias-prod -g rg-ladyranias --hostname ladyranias.no
az containerapp hostname bind -n ca-ladyranias-prod -g rg-ladyranias --hostname ladyranias.no `
  --environment cae-ladyranias --validation-method HTTP
```

`www` bekreftes via `CNAME`-oppføringen. Domenet uten www kan ikke ha `CNAME`, så der bekrefter Azure ved å hente en fil
over HTTP. Sertifikatet kan bruke noen minutter.

- [ ] **Skriv domenene inn i `prod.bicepparam`**, så de ikke forsvinner ved neste deploy:

```powershell
az containerapp hostname list -n ca-ladyranias-prod -g rg-ladyranias --query "[].{name:name, certificateId:certificateId}" -o json
```

Kopier `name` og `certificateId` inn i `customDomains`, og kjør `what-if` for prod. Den skal ikke vise noen endring på domenene.

- [ ] **Entra:** legg til `https://www.ladyranias.no/api/auth/signin-oidc` og `https://www.ladyranias.no/api/auth/signout-callback-oidc`
      som redirect-URI-er. Domenet uten www trenger ikke, fordi det alltid sendes til www først.

### Del 4: Mamma får tilgang
- [ ] Hun går til `https://www.ladyranias.no/admin`, logger inn med e-posten sin og engangskoden, og ser «Ingen tilgang»
- [ ] Gi henne rollen **Admin**: LadyRanias-tenanten → **Enterprise applications → LadyRanias → Users and groups**.
      Velg brukeren som ble opprettet da hun logget inn (med lang id foran `@ladyranias.onmicrosoft.com`), ikke en med samme navn.
- [ ] Hun logger inn på nytt, og kan legge inn katter, bilder og kull

### Del 5 (valgfri): E-post fra domenet
Varslene kommer i dag fra `DoNotReply@….azurecomm.net`. De kan komme fra `ikke-svar@ladyranias.no` i stedet.
- [ ] **Email Communication Services → Provision domains → Add domain → Custom domain** `ladyranias.no`
- [ ] Azure viser `TXT`- og `CNAME`-oppføringer (verifisering, SPF og DKIM) som legges inn hos one.com.
      **Har domenet allerede en SPF-oppføring** (`v=spf1 …`, typisk fordi e-posten ligger hos one.com), skal Azure sin del
      **flettes inn i den samme oppføringen**. Et domene kan bare ha én SPF-oppføring.
- [ ] Koble domenet til Communication Services, og bytt `emailSenderAddress` i `prod.bicepparam`

**Ferdig når**
- [ ] `https://www.ladyranias.no` viser siden, med gyldig sertifikat
- [ ] `https://ladyranias.no/katter` sendes til `https://www.ladyranias.no/katter`
- [ ] Mamma kan logge inn og legge inn innhold, og får e-postvarsel fra kontaktskjemaet
- [ ] **Run workflow** på «Deploy til prod» flytter samme SHA som i test over til prod
- [ ] `what-if` for prod viser ingen endringer på domenene

**Det kan være lurt å slå opp:** «Azure Container Apps custom domain managed certificate», «asuid TXT record»,
«CNAME vs A record», «apex domain», «workflow_dispatch», «build once deploy many», «SPF record merge».

---

Neste steg beskrives her når vi kommer dit.
