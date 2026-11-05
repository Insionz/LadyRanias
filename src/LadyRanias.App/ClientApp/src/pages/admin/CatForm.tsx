import { useState, type FormEvent } from 'react'
import { Alert, Button, Group, Select, Stack, TextInput, Textarea } from '@mantine/core'
import { send } from '../../api/api'
import type { Cat, Sex } from '../../types/types'

type Props = {
  cat?: Cat // mangler den, lager vi en ny katt
  onSaved: () => void
  onCancel: () => void
}

const today = new Date().toISOString().slice(0, 10)

export default function CatForm({ cat, onSaved, onCancel }: Props) {
  // Startverdiene leses bare første gang. Skjemaet får en ny «key» for hver katt, så det starter på nytt.
  const [name, setName] = useState(cat?.name ?? '')
  const [sex, setSex] = useState<Sex | null>(cat?.sex === 'Unknown' ? null : (cat?.sex ?? null))
  const [birthDate, setBirthDate] = useState(cat?.birthDate ?? '')
  const [description, setDescription] = useState(cat?.description ?? '')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError(null)

    const body = { name, sex, birthDate, description }
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
        <TextInput label="Navn" required value={name} onChange={e => setName(e.currentTarget.value)} />
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
          onChange={e => setBirthDate(e.currentTarget.value)}
        />
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
          <Button type="submit" loading={saving} disabled={!sex}>
            Lagre
          </Button>
        </Group>
      </Stack>
    </form>
  )
}
