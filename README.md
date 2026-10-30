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
| 4 | Lagring: Table Storage via Azurite lokalt, nøkkeldesign, ETag | Pågår |
| 5 | Bilder: opplasting til Blob Storage, størrelser og miniatyrer | |
| 6 | Frontend: React + TypeScript strict + Vite, servert av appen, offentlige sider | |
| 7 | Komponent **Kull og kattunger**: planlagt, født, ledig, reservert og solgt | |
| 8 | Admin: innlogging for oppdretter, `api/admin` beskyttet, adminsider | |
| 9 | Kontaktskjema: validering, spambeskyttelse, e-postvarsel via bakgrunnsjobb | |
| 10 | Container-image (chiseled), bygget og publisert fra `main` | |
| 11 | Deploy til **test** i Azure Container Apps, automatisk fra `main` | |
| 12 | Deploy til **prod** etter godkjenning, eget domene fra one.com, HTTPS | |
| 13 | Observerbarhet: logger, helseprober, varsler | |

## Utsatt til senere (Cats)

- [ ] Farge (tekst, for eksempel «svart tabby» eller EMS-kode)
- [ ] Status: aktiv i avl eller pensjonert (enum, ikke `bool`)
- [ ] `GET api/public/cats` viser bare aktive katter, admin-lista viser alle
- [ ] `Description` valgfri (`string?`), og `[StringLength]` på tekstfeltene
- [ ] «Endre navn» byttes ut med full oppdatering: `PUT api/admin/cats/{id}` med `UpdateCatDto`

---

## Steg 4: Lagring i Azure Table Storage

**Mål:** Kattene overlever en omstart av appen. `InMemoryCatDatabase` får en søster, `TableCatDatabase`,
som lagrer i Table Storage. Handleren, controllerne og testene merker ingenting, fordi de bare kjenner `ICatDatabase`.

**Hva er Table Storage?** En enkel og billig nøkkel/verdi-database i Azure. Hver rad identifiseres av
**PartitionKey + RowKey**, og det er bare disse to som er raske å slå opp på. Lokalt kjører vi **Azurite**,
en emulator som oppfører seg som Table Storage på din egen maskin.

### Del 1: Kjør Azurite lokalt
- [ ] Installer og start Azurite (se under)
- [ ] Installer **Azure Storage Explorer**, så du kan se tabellene og radene dine
- [ ] Legg mappen Azurite lagrer data i (for eksempel `.azurite/`) i `.gitignore`

### Del 2: `TableCatDatabase`
- [ ] Pakken `Azure.Data.Tables` i appen
- [ ] Tilkoblingsstreng i `appsettings.Development.json`: `"UseDevelopmentStorage=true"` betyr Azurite
- [ ] `TableServiceClient` registreres som singleton i DI
- [ ] `TableCatDatabase : ICatDatabase`, `internal sealed`, i `Impl/`
- [ ] Tabellen opprettes hvis den ikke finnes (`CreateIfNotExistsAsync`)
- [ ] `UseCatsComponent()` registrerer `TableCatDatabase` i stedet for `InMemoryCatDatabase`

**Nøkkeldesign (bestem selv, og skriv ned hvorfor):**
- **PartitionKey**: Det blir få katter. Én fast verdi, for eksempel `"cat"`, legger alle i samme partisjon. Det er
  raskt og enkelt for små datamengder. Hva ville du valgt hvis det var millioner av rader?
- **RowKey**: Id-en som tekst.

**Typer Table Storage ikke støtter.** Lagringen kan bare `string`, `int`, `long`, `double`, `bool`, `Guid`,
`DateTime`, `DateTimeOffset` og `byte[]`. Altså **ikke** `DateOnly` og **ikke** enum. Du må oversette mellom
`CatEntity` og raden som lagres. Det er en god grunn til at entitet og DTO er skilt: nå får du et tredje format,
og bare `TableCatDatabase` trenger å vite om det.

**Sortering.** Table Storage returnerer rader sortert på PartitionKey og RowKey, ikke på `CreatedAt`.
Sorteringen må derfor flyttes fra databasen til **handleren**. Testen `Get_all_cats_returns_all_newest_first`
skal fortsatt være grønn etterpå.

### Del 3: ETag og samtidige endringer
- [ ] Hver rad har en **ETag**, en versjon som endres ved hver lagring
- [ ] Ved oppdatering sendes ETag-en med. Har noen andre endret raden i mellomtiden, avviser Table Storage lagringen.
- [ ] Bestem hva handleren skal gjøre da, og hva controlleren svarer (409 Conflict?)

### Del 4: Testene
- [ ] Enhetstestene bruker fortsatt `InMemoryCatDatabase`. Ingen endring.
- [ ] Komponenttestene bytter ut `ICatDatabase` med `InMemoryCatDatabase` via `ConfigureTestServices`,
      så de ikke trenger Azurite
- [ ] Nye tester for `TableCatDatabase` som kjører mot ekte Azurite: lagre, hente, slette, ETag-konflikt
- [ ] CI starter Azurite som en *service container* i GitHub Actions, så disse testene også kjører der

**Ferdig når**
- [ ] Opprett en katt, stopp appen, start den igjen, og katten er der fortsatt
- [ ] Alle tester er grønne lokalt og i CI
- [ ] Merget via PR

**Det kan være lurt å slå opp:** «Azurite», «Azure.Data.Tables», `TableClient`, `ITableEntity`, `TableEntity`,
«PartitionKey RowKey design», `ETag`, `UpdateEntityAsync`, `TableUpdateMode`, «GitHub Actions service containers».

---

Neste steg beskrives her når vi kommer dit.
