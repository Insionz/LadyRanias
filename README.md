# Lady Rania’s

Nettsiden til katteoppdrettet Lady Rania’s, som driver med Maine Coon og Hellig Birma: **[www.ladyranias.no](https://www.ladyranias.no)**

Besøkende ser kattene, kullene og kattungene uten å logge inn. Oppdretteren logger inn på en adminside og legger inn
alt selv: katter, bilder, kull, nyheter og teksten på forsiden.

Bygget som læringsprosjekt, for å lære en moderne .NET/React-stakk på Azure fra grunnen av, med et ekte behov og en ekte
bruker. Hele veien er beskrevet i [byggeloggen](docs/byggelogg.md).

## Hva siden kan

**For besøkende**
- Våre katter, per rase, med titler som rosetter, stamtavle hos NRR og bildegalleri
- Kull og kattunger: planlagte kull, kattunger som er ledige, reservert eller under evaluering, og tidligere kull
- Bilder av kattungene uke for uke («Uke 5»), med datoen fra kameraet
- Nyheter, minneside for katter som er døde, «Om oss» og kontaktskjema

**For oppdretteren**
- Innlogging med engangskode på e-post, uten passord
- Katter med rase, EMS-kode, farge, registreringsnummer, titler, mor og far, også lånte hannkatter fra andre oppdrett
- Kull fra planlagt til født, kattunger med status, og kattunger som blir værende, blir voksne katter
- Bilder lastes opp fra mobilen, skaleres og lagres uten metadata (ingen GPS-posisjon)
- Nyheter med bilder, valg av forsidebilde (fast, tilfeldig eller lysbilder), og velkomsttekst
- E-post når noen sender en melding i kontaktskjemaet, med «Svar» rett til avsenderen

## Teknologi

| | |
|---|---|
| **Backend** | .NET 10, ASP.NET Core, MSTest 4 |
| **Frontend** | React 19, TypeScript (strict), Vite, Mantine |
| **Data** | Azure Table Storage (katter, kull, nyheter, meldinger), Blob Storage (bilder), Queue Storage (e-postkø) |
| **Innlogging** | Microsoft Entra External ID (OpenID Connect), rollen `Admin` |
| **E-post** | Azure Communication Services |
| **Drift** | Azure Container Apps (chiseled image), Azure Container Registry, managed identity, eget domene med gratis HTTPS |
| **Infrastruktur** | Bicep, ett oppsett for test og prod (`infra/`) |
| **CI/CD** | GitHub Actions: bygg og test på hver PR, deploy til test fra `main`, prod med en knapp. Innlogging i Azure med OIDC, uten passord |
| **Overvåking** | Helseprober, Application Insights via OpenTelemetry, varsler og budsjett på e-post |

## Arkitektur

```
Besøkende ──► www.ladyranias.no ──► Azure Container Apps
                                     └─ LadyRanias.App (.NET)
                                        ├─ React-appen (statiske filer)
                                        ├─ api/public/...  (åpent, kun lesing)
                                        ├─ api/admin/...   (krever innlogging og rollen Admin)
                                        └─ bakgrunnsjobb (e-postvarsel fra kontaktskjemaet)
                                              │
              Azure Table og Queue Storage ◄──┴──► Azure Blob Storage
              (katter, kull, nyheter, meldinger)     (bilder)
```

Én .NET-prosess er både API og server for React-appen. Domenet ligger hos one.com, som bare peker DNS mot Azure.

### Komponenter

Backend er delt i komponenter under `src/LadyRanias.App/Components/`: `Cats`, `Litters`, `CatImages`, `Posts`, `Site`,
`Contact`, `Auth`, `Storage`, `Health` og `Telemetry`. Hver komponent har samme oppbygning:

```
Components/Cats/
├─ Api/           det andre komponenter får se: DTO-er, ICatHandler, ICatDeletedListener
├─ Impl/          handler, regler og databaser (Table Storage og i minnet), internal
├─ Controllers/   api/public og api/admin
└─ CatsComponent.cs   UseCatsComponent() registrerer alt
```

Komponentene snakker bare med hverandre via `Api/`. Når en katt slettes, sier `Cats` fra via `ICatDeletedListener`,
og `CatImages` og `Posts` rydder opp etter seg. `Cats` vet ikke at de finnes.

## Valg underveis

Noen av valgene som er tatt. Begrunnelsen for resten står i [byggeloggen](docs/byggelogg.md).

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

## Kjøre lokalt

Du trenger .NET 10 SDK, Node.js og Docker (for Azurite, som etterligner Azure Storage).

```powershell
# 1. Azure Storage lokalt
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite `
  azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --skipApiVersionCheck

# 2. Backend på http://localhost:5127
dotnet run --project src/LadyRanias.App

# 3. Frontend på http://localhost:5173, med API-et videresendt til backend
cd src/LadyRanias.App/ClientApp
npm ci
npm run dev
```

Uten mer oppsett virker hele den offentlige siden. Innlogging er slått av, så `api/admin` svarer 401, og e-poster
skrives til loggen i stedet for å sendes. For å logge inn lokalt trengs en app-registrering i Entra og
`dotnet user-secrets` for `AzureAd:*`. Se [steg 8 i byggeloggen](docs/byggelogg.md#steg-8-admin-med-microsoft-entra-external-id).

## Tester

```
dotnet test
```

Testene er delt i tre: `ServiceTests` (handlere og regler), `ComponentTests` (hele API-et via
`WebApplicationFactory`) og `IntegrationTests` (databasene og bildelageret mot Azurite). Azurite må kjøre.

Frontend sjekkes med `npm run lint` og `npm run build` i `ClientApp`.

## Deploy

- **Test:** hver merge til `main` bygger et image, publiserer det til Azure Container Registry og deployer til testmiljøet.
- **Prod:** **Actions → Deploy til prod → Run workflow** deployer et image som allerede er testet.
- **Infrastruktur:** `infra/main.bicep` med `test.bicepparam` og `prod.bicepparam`, kjøres for hånd med
  `az deployment group what-if` og `create`. Se steg 11 og 12 i byggeloggen.

## Lisens

Koden er lisensiert under MIT, se [LICENSE](LICENSE). Bildene og tekstene på ladyranias.no ligger ikke i repoet, og
lisensen gjelder ikke dem.
