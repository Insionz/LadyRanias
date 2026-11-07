import { useState, type FormEvent } from 'react'
import { Alert, Button, Checkbox, Group, Select, SimpleGrid, Stack, TagsInput, Text, TextInput, Textarea } from '@mantine/core'
import { send, useApi } from '../../api/api'
import type { Breed, Cat, CatRole, Sex } from '../../types/types'
import { breedLabel, emsBreedCode, roleLabel } from '../../utils/format'
import { knownTitleCodes, parseTitle } from '../../utils/titles'

type Props = {
  cat?: Cat // mangler den, lager vi en ny katt
  onSaved: () => void
  onCancel: () => void
}

const today = new Date().toISOString().slice(0, 10)

const breeds: Breed[] = ['MaineCoon', 'SacredBirman']

/**
 * Hvilke roller som kan velges. Samme regler som i backend (CatHandler.ValidateRoleChange):
 * en kattunge lages via kullet, og bare en beholdt kattunge kan bli voksen katt her.
 */
function roleOptions(cat?: Cat): CatRole[] {
  if (cat?.role === 'Kitten') {
    return cat.saleStatus === 'Kept' ? ['Kitten', 'Breeding', 'Retired'] : ['Kitten']
  }
  return ['Breeding', 'Retired', 'External']
}

export default function CatForm({ cat, onSaved, onCancel }: Props) {
  // Startverdiene leses bare første gang. Skjemaet får en ny «key» for hver katt, så det starter på nytt.
  const [name, setName] = useState(cat?.name ?? '')
  const [sex, setSex] = useState<Sex | null>(cat?.sex === 'Unknown' ? null : (cat?.sex ?? null))
  const [birthDate, setBirthDate] = useState(cat?.birthDate ?? '')
  const [description, setDescription] = useState(cat?.description ?? '')
  const [breed, setBreed] = useState<Breed | null>(cat?.breed === 'Unknown' ? null : (cat?.breed ?? null))
  const [role, setRole] = useState<CatRole>(cat?.role ?? 'Breeding')
  const [emsCode, setEmsCode] = useState(cat?.emsCode ?? '')
  const [color, setColor] = useState(cat?.color ?? '')
  const [neutered, setNeutered] = useState(cat?.neutered ?? false)
  const [registrationNumber, setRegistrationNumber] = useState(cat?.registrationNumber ?? '')
  // Titlene som en liste, én per merkelapp. Lagres som tekst med mellomrom: «GIC JW WW'24».
  const [titles, setTitles] = useState<string[]>((cat?.titles ?? '').split(/\s+/).filter(Boolean))
  const [motherId, setMotherId] = useState<string | null>(cat?.motherId ?? null)
  const [motherName, setMotherName] = useState(cat?.motherName ?? '')
  const [fatherId, setFatherId] = useState<string | null>(cat?.fatherId ?? null)
  const [fatherName, setFatherName] = useState(cat?.fatherName ?? '')
  const [deceased, setDeceased] = useState(cat?.deceased ?? false)
  const [deathDate, setDeathDate] = useState(cat?.deathDate ?? '')
  const [roleNotice, setRoleNotice] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const roles = roleOptions(cat)
  const bornHere = cat?.litterId != null

  // Mulige foreldre: alle katter i admin-lista (også eksterne), med riktig kjønn, og ikke katten selv.
  const allCats = useApi<Cat[]>(bornHere ? null : '/api/admin/cats')
  const parentOptions = (parentSex: Sex) =>
    (allCats.data ?? [])
      .filter(c => c.sex === parentSex && c.id !== cat?.id)
      .sort((a, b) => a.name.localeCompare(b.name, 'nb'))
      .map(c => ({ value: c.id, label: c.role === 'External' ? `${c.name} (ekstern)` : c.name }))
  const isKitten = role === 'Kitten'

  // En kastrert katt kan ikke være i avl (samme regel som i backend, CatHandler.ValidateNeutered).
  // I stedet for en feilmelding endrer vi det andre feltet, for det er nesten alltid det som er ment.
  function changeNeutered(checked: boolean) {
    setNeutered(checked)
    if (checked && role === 'Breeding' && roles.includes('Retired')) {
      setRole('Retired')
      setRoleNotice('Rollen er endret til «Pensjonert», for en kastrert katt kan ikke være i avl.')
    } else {
      setRoleNotice(null)
    }
  }

  function changeRole(newRole: CatRole) {
    setRole(newRole)
    if (newRole === 'Breeding' && neutered) {
      setNeutered(false)
      setRoleNotice('«Kastrert» er fjernet, for en katt i avl kan ikke være kastrert.')
    } else {
      setRoleNotice(null)
    }
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError(null)

    // Tom dødsdato betyr at katten lever. Den sendes som null, ikke som tom tekst.
    const body = {
      name,
      sex,
      birthDate,
      description,
      breed,
      role,
      emsCode,
      color,
      neutered,
      registrationNumber,
      titles: titles.join(' '),
      // Født her: mor og far kommer fra kullet. Ellers en katt fra lista, eller bare et navn.
      motherId: bornHere ? null : motherId,
      motherName: bornHere || motherId ? null : motherName,
      fatherId: bornHere ? null : fatherId,
      fatherName: bornHere || fatherId ? null : fatherName,
      deceased,
      // Ikke død: ingen dato, selv om feltet hadde en fra før.
      deathDate: deceased && deathDate ? deathDate : null,
    }
    try {
      if (cat) {
        await send('PUT', `/api/admin/cats/${cat.id}`, body)
      } else {
        await send('POST', '/api/admin/cats', body)
      }
      onSaved()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kunne ikke lagre')
    } finally {
      setSaving(false)
    }
  }

  return (
    <form onSubmit={submit}>
      <Stack>
        {error && <Alert color="red">{error}</Alert>}
        {cat?.breed === 'Unknown' && (
          <Alert color="yellow">Katten ble lagt inn før rasen fantes. Velg rase før du lagrer.</Alert>
        )}
        <TextInput label="Navn" required value={name} onChange={e => setName(e.currentTarget.value)} />
        <SimpleGrid cols={{ base: 1, sm: 2 }}>
          <Select
            label="Rase"
            required
            value={breed}
            onChange={value => setBreed(value as Breed | null)}
            data={breeds.map(b => ({ value: b, label: breedLabel(b) }))}
          />
          <Select
            label="Rolle"
            required
            allowDeselect={false}
            disabled={roles.length === 1}
            description={isKitten && roles.length === 1 ? 'Sett kattungen til «Blir hos oss» for å gjøre den voksen.' : undefined}
            value={role}
            onChange={value => value && changeRole(value as CatRole)}
            data={roles.map(r => ({ value: r, label: roleLabel(r) }))}
          />
          <Select
            label="Kjønn"
            required
            value={sex}
            onChange={value => setSex(value as Sex | null)}
            data={[
              { value: 'Female', label: 'Hunnkatt' },
              { value: 'Male', label: 'Hannkatt' },
            ]}
          />
          <TextInput
            label="Fødselsdato"
            type="date"
            required
            max={today}
            value={birthDate}
            // En kattunge har fødselsdatoen til kullet.
            disabled={cat?.litterId != null}
            onChange={e => setBirthDate(e.currentTarget.value)}
          />
          <TextInput
            label="EMS-kode"
            placeholder={breed ? `${emsBreedCode(breed)} …` : 'Velg rase først'}
            description="Fra stamtavlen. Kan stå tom til fargen er kjent."
            value={emsCode}
            onChange={e => setEmsCode(e.currentTarget.value)}
          />
          <TextInput
            label="Farge"
            placeholder="For eksempel «svart tabby med hvitt»"
            value={color}
            onChange={e => setColor(e.currentTarget.value)}
          />
          <TextInput
            label="Registreringsnummer"
            value={registrationNumber}
            onChange={e => setRegistrationNumber(e.currentTarget.value)}
          />
          <TagsInput
            label="Titler"
            description="Velg fra lista. Vinnertitler med år skrives inn, for eksempel WW'24."
            placeholder={titles.length === 0 ? 'Velg eller skriv' : undefined}
            data={knownTitleCodes}
            value={titles}
            // Store bokstaver, og «WW 24» blir «WW'24», så titlene gjenkjennes og får rosett.
            onChange={values => setTitles(values.map(v => parseTitle(v.trim().replace(/\s+(?=['’]?\d{2,4}$)/, '')).text))}
            renderOption={({ option }) => `${option.value} – ${parseTitle(option.value).name}`}
            maxDropdownHeight={260}
            clearable
          />
        </SimpleGrid>

        {bornHere ? (
          <Text size="sm" c="dimmed">
            Mor og far kommer fra kullet katten er født i.
          </Text>
        ) : (
          <SimpleGrid cols={{ base: 1, sm: 2 }}>
            <Stack gap={4}>
              <Select
                label="Mor"
                description="En katt vi har i systemet, også en ekstern"
                placeholder="Velg katt"
                searchable
                clearable
                value={motherId}
                onChange={setMotherId}
                data={parentOptions('Female')}
              />
              {!motherId && (
                <TextInput
                  aria-label="Mors navn"
                  placeholder="… eller skriv navnet, med titler"
                  value={motherName}
                  onChange={e => setMotherName(e.currentTarget.value)}
                />
              )}
            </Stack>
            <Stack gap={4}>
              <Select
                label="Far"
                description="En katt vi har i systemet, også en ekstern"
                placeholder="Velg katt"
                searchable
                clearable
                value={fatherId}
                onChange={setFatherId}
                data={parentOptions('Male')}
              />
              {!fatherId && (
                <TextInput
                  aria-label="Fars navn"
                  placeholder="… eller skriv navnet, med titler"
                  value={fatherName}
                  onChange={e => setFatherName(e.currentTarget.value)}
                />
              )}
            </Stack>
          </SimpleGrid>
        )}

        <Group align="flex-end">
          <Checkbox
            label="Kastrert"
            description={roleNotice ?? undefined}
            checked={neutered}
            onChange={e => changeNeutered(e.currentTarget.checked)}
            mb={8}
          />
          {/* Også for nye katter: mange av de gamle kattene legges inn etter at de er døde. */}
          <Checkbox
            label="Død"
            description="Katten flyttes til minnesiden"
            checked={deceased}
            onChange={e => setDeceased(e.currentTarget.checked)}
            mb={8}
          />
          {deceased && (
            <TextInput
              label="Dødsdato"
              description="La stå tom hvis du ikke husker den"
              type="date"
              min={birthDate || undefined}
              max={today}
              value={deathDate}
              onChange={e => setDeathDate(e.currentTarget.value)}
            />
          )}
        </Group>
        <Textarea
          label="Beskrivelse"
          autosize
          minRows={3}
          value={description}
          onChange={e => setDescription(e.currentTarget.value)}
        />
        <Group justify="flex-end">
          <Button variant="default" onClick={onCancel} disabled={saving}>
            Avbryt
          </Button>
          <Button type="submit" loading={saving} disabled={!sex || !breed}>
            Lagre
          </Button>
        </Group>
      </Stack>
    </form>
  )
}
