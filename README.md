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
| 6 | Frontend: React + TypeScript strict + Vite + Mantine, servert av appen, offentlige sider (forside, katter, katt, kontakt, 404) | Klar for PR |
| 7 | Komponent **Kull og kattunger**: planlagt, født, ledig, reservert og solgt | |
| 8 | Admin: innlogging for oppdretter, `api/admin` beskyttet, adminsider | |
| 9 | Kontaktskjema: validering, spambeskyttelse, e-postvarsel via bakgrunnsjobb | |
| 10 | Container-image (chiseled), bygget og publisert fra `main` | |
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

Neste steg beskrives her når vi kommer dit.
