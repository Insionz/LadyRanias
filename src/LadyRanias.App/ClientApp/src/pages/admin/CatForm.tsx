import { useState, type FormEvent } from 'react'
import { Alert, Button, Checkbox, Group, Select, SimpleGrid, Stack, TextInput, Textarea } from '@mantine/core'
import { send } from '../../api/api'
import type { Breed, Cat, CatRole, Sex } from '../../types/types'
import { breedLabel, emsBreedCode, roleLabel } from '../../utils/format'

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
  const [titles, setTitles] = useState(cat?.titles ?? '')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const roles = roleOptions(cat)
  const isKitten = role === 'Kitten'

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError(null)

    const body = { name, sex, birthDate, description, breed, role, emsCode, color, neutered, registrationNumber, titles }
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
            onChange={value => value && setRole(value as CatRole)}
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
          <TextInput label="Titler" placeholder="For eksempel «IC»" value={titles} onChange={e => setTitles(e.currentTarget.value)} />
        </SimpleGrid>
        <Checkbox label="Kastrert" checked={neutered} onChange={e => setNeutered(e.currentTarget.checked)} />
        <Textarea
          label="Beskrivelse"
          required
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
