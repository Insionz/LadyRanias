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
| 5 | Bilder: opplasting til Blob Storage, størrelser og miniatyrer | Pågår |
| 6 | Frontend: React + TypeScript strict + Vite, servert av appen, offentlige sider | |
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

---

## Steg 5: Bilder i Blob Storage

**Mål:** Oppdretter kan laste opp bilder av en katt. Besøkende ser dem i to størrelser, en miniatyr i lister
og et stort bilde på detaljsiden. Bildene lagres i Blob Storage, og informasjon om dem i Table Storage.

**Hva er Blob Storage?** Lagring av filer («blobs») i *containere*, omtrent som mapper. Hver blob har et navn,
for eksempel `3f2a.../9b1c...-thumb.webp`. Azurite har allerede Blob på port 10000.

### Ny komponent: `CatImages`
Bilder er et eget ansvarsområde, så de får sin egen komponent med `Api/`, `Impl/` og `CatImagesComponent.cs`,
akkurat som `Cats`. `Cats` skal ikke vite hvordan bilder lagres.

### Del 1: Lagre og hente filer
- [ ] Pakken `Azure.Storage.Blobs`, og `BlobServiceClient` registrert i DI med samme tilkoblingsstreng
- [ ] `IImageStore` (i `Impl/`) med tre operasjoner: lagre en strøm, åpne en strøm, slette
- [ ] `BlobImageStore : IImageStore`, som oppretter containeren `cat-images` hvis den ikke finnes
- [ ] Integrasjonstester mot Azurite, med unik container per test, slik du gjorde med tabellene

### Del 2: Opplasting
- [ ] `POST api/admin/cats/{catId}/images` tar imot en fil (`IFormFile`, `multipart/form-data`)
- [ ] Avvis hvis katten ikke finnes (404), hvis filen ikke er JPEG, PNG eller WebP (400), eller er over 10 MB (400)
- [ ] Informasjon om hvert bilde lagres i en egen tabell `catimages`

**Nøkkeldesign:** Her blir PartitionKey interessant for første gang.
Med **PartitionKey = catId** og **RowKey = imageId** ligger alle bildene til én katt i samme partisjon,
og «hent alle bilder for katt X» blir ett raskt oppslag. Sammenlign med `cats`-tabellen, der alt ligger i én partisjon.

### Del 3: Størrelser og personvern
- [ ] Hvert opplastet bilde lagres i to versjoner: miniatyr (for eksempel 400 px bred) og stort (for eksempel 1600 px bred)
- [ ] Originalen lagres ikke. Den er ofte mange MB fra en mobil, og trengs ikke på nettsiden.
- [ ] **Fjern metadata (EXIF)** før lagring. Bilder fra mobil inneholder ofte **GPS-posisjonen** der de ble tatt,
      altså adressen hjemme. Det skal ikke ut på nettet.
- [ ] Bildebehandlingen ligger bak et eget interface (`IImageProcessor`), så den kan testes alene

**Bibliotek:** `SixLabors.ImageSharp` er enklest å bruke. Sjekk lisensen: den er gratis for privatpersoner og små
virksomheter, men ikke for alle. `SkiaSharp` er et alternativ med MIT-lisens, men med et mer krevende API.

### Del 4: Visning og sletting
- [ ] `GET api/public/cats/{catId}/images` gir en liste over bildene til katten
- [ ] `GET api/public/cats/{catId}/images/{imageId}?size=thumb|large` gir selve bildet, med riktig `Content-Type`
      og `Cache-Control`, så nettleseren ikke laster det ned på nytt hver gang
- [ ] `DELETE api/admin/cats/{catId}/images/{imageId}` sletter både blobene og raden i tabellen
- [ ] Når en **katt** slettes, slettes bildene også

**Tester**
- [ ] Enhetstester: validering av filtype og størrelse, og at `IImageProcessor` gir riktige størrelser uten EXIF
- [ ] Integrasjonstester: `BlobImageStore` mot Azurite
- [ ] Komponenttest: last opp et lite bilde med `MultipartFormDataContent`, hent miniatyren, og sjekk `Content-Type`

**Ferdig når**
- [ ] Du kan laste opp et bilde fra `.http`-fila og åpne miniatyren i nettleseren
- [ ] Alle tester er grønne lokalt og i CI
- [ ] Merget via PR

**Det kan være lurt å slå opp:** «Azure.Storage.Blobs», `BlobContainerClient`, `UploadAsync`, `OpenReadAsync`,
`IFormFile`, `[RequestSizeLimit]`, «multipart/form-data», `File(stream, contentType)` i en controller,
«Cache-Control», «EXIF GPS», `Image.LoadAsync`, `Mutate(x => x.Resize(...))`.

---

Neste steg beskrives her når vi kommer dit.
