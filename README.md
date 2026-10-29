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
| 3 | Første komponent **Katter** (avlsdyr): controller → handler → database (i minnet), tester | Pågår |
| 4 | Lagring: Table Storage via Azurite lokalt, nøkkeldesign, ETag | |
| 5 | Bilder: opplasting til Blob Storage, størrelser og miniatyrer | |
| 6 | Frontend: React + TypeScript strict + Vite, servert av appen, offentlige sider | |
| 7 | Komponent **Kull og kattunger**: planlagt, født, ledig, reservert og solgt | |
| 8 | Admin: innlogging for oppdretter, `api/admin` beskyttet, adminsider | |
| 9 | Kontaktskjema: validering, spambeskyttelse, e-postvarsel via bakgrunnsjobb | |
| 10 | Container-image (chiseled), bygget og publisert fra `main` | |
| 11 | Deploy til **test** i Azure Container Apps, automatisk fra `main` | |
| 12 | Deploy til **prod** etter godkjenning, eget domene fra one.com, HTTPS | |
| 13 | Observerbarhet: logger, helseprober, varsler | |

---

## Steg 2: Repo, PR-flyt og CI

**Mål:** Ingen kode kommer inn på `main` uten en PR med grønt bygg.

**Krav**
- [ ] Hovedgrenen heter `main`, både lokalt og på GitHub
- [ ] Workflow `.github/workflows/ci.yml` som kjører på pull request mot `main`:
      `ubuntu-latest`, sjekker ut koden, setter opp .NET 10, og kjører `restore`, `build` og `test` som separate steg
- [ ] `main` er beskyttet: ingen direkte push, PR kreves, og CI-jobben må være grønn før merge

**Ferdig når**
- [ ] Gren → endring → PR → CI kjører → merge
- [ ] En PR med `string s = null;` blir rød og kan ikke merges

**Det kan være lurt å slå opp:** «GitHub Actions workflow syntax», `on: pull_request`, `actions/checkout`,
`actions/setup-dotnet`, «GitHub rulesets», «required status checks».
Status-sjekken kan ikke gjøres påkrevd før workflowen har kjørt minst én gang.

---

## Steg 3: Første komponent – Katter (avlsdyr)

**Mål:** API for avlsdyrene, bygget som en selvstendig komponent der en forespørsel går
controller → handler → database. Data ligger foreløpig i minnet, og byttes til Table Storage i steg 4.

**Rydd først**
- [ ] Slett `WeatherForecast.cs`, `WeatherForecastController.cs` og `Test1.cs`

**Mappestruktur**
```
src/LadyRanias.App/Components/Katter/
├─ KatterComponent.cs   ← UseKatterComponent(): registrerer alt i DI, kalles fra Program.cs
├─ Api/                 ← det andre får se: interface for handleren og DTO-ene
└─ Impl/                ← controllere, handler, database-interface og in-memory-database
```

**En katt har (minst)**
- Id, navn, kjønn (hann/hunn), fødselsdato, farge, beskrivelse (valgfri)
- om den er i aktiv avl eller pensjonert

**Endepunkter**

| Metode og rute | Hvem | Hva |
|---|---|---|
| `GET api/public/katter` | alle | liste over katter i aktiv avl |
| `GET api/public/katter/{id}` | alle | én katt, 404 hvis den ikke finnes |
| `POST api/admin/katter` | admin* | opprett, 400 ved ugyldig input, 201 med den nye katten |
| `PUT api/admin/katter/{id}` | admin* | endre, 404 hvis den ikke finnes |
| `DELETE api/admin/katter/{id}` | admin* | slett, 204, eller 404 |

\* Admin-rutene får innlogging i steg 8. Nå er de åpne, men skal ligge i en **egen controller**,
så vi senere kan låse hele prefikset på ett sted.

**Regler**
- [ ] Controllerne har ingen logikk. De oversetter bare mellom HTTP og handleren.
- [ ] Handleren vet ingenting om HTTP. Den returnerer ikke `IActionResult`.
- [ ] Bare `Api/` og controllerne er `public`. Resten er `internal`.
- [ ] Databasen tåler samtidige forespørsler, siden alt er singleton.
- [ ] Navn er påkrevd, og fødselsdato kan ikke være i fremtiden.

**Tester**
- [ ] Enhetstester av handleren, uten HTTP
- [ ] Minst én komponenttest som starter hele appen og går via HTTP: opprett en katt med POST, og hent den med GET
- [ ] En test som viser at en pensjonert katt ikke kommer med i den offentlige lista

**Ferdig når**
- [ ] `dotnet test` er grønn lokalt og i CI
- [ ] Du kan opprette og hente katter fra `LadyRanias.App.http`
- [ ] Merget via PR

**Det kan være lurt å slå opp:** `[ApiController]`, `[Route]`, «dependency injection lifetimes»,
`ConcurrentDictionary`, `WebApplicationFactory` (pakken `Microsoft.AspNetCore.Mvc.Testing`),
`InternalsVisibleTo`, `TimeProvider` (for å teste «ikke i fremtiden» uten å være avhengig av dagens dato).

---

Neste steg beskrives her når vi kommer dit.
