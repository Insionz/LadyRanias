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
| 3a | Første komponent **Cats**: controller → handler → database (i minnet), enhets- og komponenttester | ✅ |
| 3b | **Cats**, resten: farge, aktiv/pensjonert, regler for fødselsdato, valgfri beskrivelse | Pågår |
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

## Steg 3b: Cats – resten

**Mål:** Avlsdyrene har det moren trenger å vise, og reglene for gyldige data ligger i handleren, dekket av tester.

**Felt**
- [ ] Farge (tekst foreløpig, for eksempel «svart tabby» eller EMS-kode som «n 22»)
- [ ] Status: aktiv i avl eller pensjonert (enum, ikke `bool`, så flere verdier kan komme senere)
- [ ] `Description` er valgfri: `string?` i DTO-er og entitet, uten `[Required]`

**Regler (i handleren, med enhetstester skrevet først)**
- [ ] Fødselsdato kan ikke være i fremtiden. Bruk `TimeProvider` og test med `FakeTimeProvider`.
- [ ] Fødselsdato kan ikke være urimelig gammel, for eksempel over 30 år
- [ ] Kjønn kan ikke være `Unknown` ved opprettelse
- [ ] `[StringLength]` på `Name`, `Color` og `Description`

**Offentlig visning**
- [ ] `GET api/public/cats` viser bare katter i aktiv avl. Admin-lista viser alle.
- [ ] Sorteringen flyttes fra databasen til handleren. Avklar rekkefølgen: navn, kjønn eller nyeste først?

**Endring**
- [ ] Bytt «endre navn» med en full oppdatering: `PUT api/admin/cats/{id}` med `UpdateCatDto`, som tar alle feltene

**Tester**
- [ ] Enhetstest for hver regel over
- [ ] Komponenttest: en pensjonert katt finnes i admin-lista, men ikke i public-lista
- [ ] Komponenttest: fødselsdato i fremtiden gir 400

**Ferdig når**
- [ ] `dotnet test` er grønn lokalt og i CI
- [ ] Merget via PR

**Spørsmål til deg selv:** Når handleren avviser en ugyldig fødselsdato, hvordan forteller den det til
controlleren, slik at svaret blir 400 med en forståelig melding? Exception, en resultattype, eller noe annet?
Velg selv, og vær forberedt på å forklare valget.

---

Neste steg beskrives her når vi kommer dit.
