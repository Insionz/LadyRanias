import { useState, type FormEvent } from 'react'
import { Alert, Button, Group, Select, Stack, TextInput, Textarea } from '@mantine/core'
import { send } from '../../api/api'
import type { Cat } from '../../types/types'

type Props = {
  cats: Cat[]
  onSaved: () => void
  onCancel: () => void
}

export default function LitterForm({ cats, onSaved, onCancel }: Props) {
  const [name, setName] = useState('')
  const [motherId, setMotherId] = useState<string | null>(null)
  const [fatherId, setFatherId] = useState<string | null>(null)
  const [expectedDate, setExpectedDate] = useState('')
  const [description, setDescription] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Bare hunnkatter kan være mor, og bare hannkatter far. Backend sjekker det samme.
  const mothers = cats.filter(c => c.sex === 'Female').map(c => ({ value: c.id, label: c.name }))
  const fathers = cats.filter(c => c.sex === 'Male').map(c => ({ value: c.id, label: c.name }))

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError(null)
    try {
      await send('POST', '/api/admin/litters', {
        name,
        motherId,
        fatherId,
        expectedDate: expectedDate || null, // tomt felt = ingen dato
        description,
      })
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
        <TextInput label="Navn på kullet" placeholder="A-kullet" required value={name} onChange={e => setName(e.currentTarget.value)} />
        <Select label="Mor" required data={mothers} value={motherId} onChange={setMotherId} nothingFoundMessage="Ingen hunnkatter" />
        <Select label="Far" required data={fathers} value={fatherId} onChange={setFatherId} nothingFoundMessage="Ingen hannkatter" />
        <TextInput
          label="Forventet fødsel"
          description="Valgfri. Vises som måned og år."
          type="date"
          value={expectedDate}
          onChange={e => setExpectedDate(e.currentTarget.value)}
        />
        <Textarea label="Beskrivelse" autosize minRows={2} value={description} onChange={e => setDescription(e.currentTarget.value)} />
        <Group justify="flex-end">
          <Button variant="default" onClick={onCancel} disabled={saving}>
            Avbryt
          </Button>
          <Button type="submit" loading={saving} disabled={!motherId || !fatherId}>
            Lagre
          </Button>
        </Group>
      </Stack>
    </form>
  )
}
