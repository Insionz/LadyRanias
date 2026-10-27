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
| 2 | GitHub: `main` som hovedgren, CI som bygger og tester hver PR, beskyttet `main` | Pågår |
| 3 | Første komponent **Katter** (avlsdyr): controller → handler → database (i minnet), tester | |
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

Neste steg beskrives her når vi kommer dit.
