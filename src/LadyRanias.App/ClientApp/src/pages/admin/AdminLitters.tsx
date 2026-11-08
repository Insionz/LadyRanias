import { useState } from 'react'
import { Anchor, Button, Group, Modal, SegmentedControl, Select, Stack, Text, TextInput } from '@mantine/core'
import { useApi } from '../../api/api'
import { ErrorMessage, Loading } from '../../components/Status'
import type { Breed, Cat, Litter } from '../../types/types'
import { breedLabel } from '../../utils/format'
import { isCurrent, litterBreed, litterMatches } from '../../utils/litters'
import LitterAdminCard from './LitterAdminCard'
import LitterForm from './LitterForm'

/** Aktive = planlagte og aktuelle kull, med samme regel som den offentlige kullsiden. */
type StatusFilter = 'active' | 'planned' | 'previous' | 'all'

const breedOptions: Breed[] = ['MaineCoon', 'SacredBirman', 'Unknown']

function matchesStatus(litter: Litter, status: StatusFilter) {
  switch (status) {
    case 'active':
      return isCurrent(litter)
    case 'planned':
      return litter.status === 'Planned'
    case 'previous':
      return !isCurrent(litter)
    case 'all':
      return true
  }
}

/** Planlagte først, så nyeste født først. Det er de nye kullene mamma jobber med. */
function byNewest(a: Litter, b: Litter) {
  if (a.status !== b.status) return a.status === 'Planned' ? -1 : 1
  const dateA = a.birthDate ?? a.expectedDate ?? ''
  const dateB = b.birthDate ?? b.expectedDate ?? ''
  return dateB.localeCompare(dateA)
}

export default function AdminLitters() {
  // Det offentlige endepunktet gir alle kull med kattunger, så vi trenger ikke et eget admin-endepunkt for lesing.
  const litters = useApi<Litter[]>('/api/public/litters')
  const cats = useApi<Cat[]>('/api/admin/cats')
  const [creating, setCreating] = useState(false)

  // Filtrene. Alt skjer i nettleseren, som på «Katter».
  const [search, setSearch] = useState('')
  const [breed, setBreed] = useState<Breed | null>(null)
  const [status, setStatus] = useState<StatusFilter>('active')

  if (litters.loading || cats.loading) return <Loading />
  if (litters.error) return <ErrorMessage message={litters.error} />
  if (cats.error) return <ErrorMessage message={cats.error} />

  const allCats = cats.data ?? []
  const catsById = new Map(allCats.map(cat => [cat.id, cat]))

  const all = litters.data ?? []
  // Søket gjelder alle kull uansett status: skriver hun et navn, vil hun finne kullet, også om det er gammelt.
  const searching = search.trim() !== ''
  const shown = all
    .filter(l => litterMatches(l, search))
    .filter(l => !breed || litterBreed(l) === breed)
    .filter(l => searching || matchesStatus(l, status))
    .sort(byNewest)
  const filtered = searching || breed !== null || status !== 'active'

  function clearFilters() {
    setSearch('')
    setBreed(null)
    setStatus('active')
  }

  return (
    <>
      <Group justify="space-between" mb="md">
        <Text c="dimmed">
          {shown.length === all.length ? `${all.length} kull` : `Viser ${shown.length} av ${all.length} kull`}
          {filtered && (
            <>
              {' · '}
              <Anchor component="button" type="button" onClick={clearFilters}>
                Nullstill filter
              </Anchor>
            </>
          )}
        </Text>
        <Button onClick={() => setCreating(true)}>Nytt kull</Button>
      </Group>

      <Group mb="md" align="flex-end" wrap="wrap">
        <TextInput
          label="Søk"
          placeholder="Kull, mor, far eller kattunge"
          value={search}
          onChange={e => setSearch(e.currentTarget.value)}
          w={260}
        />
        <Select
          label="Rase"
          placeholder="Alle"
          clearable
          w={160}
          value={breed}
          onChange={value => setBreed(value as Breed | null)}
          data={breedOptions.map(b => ({ value: b, label: breedLabel(b) }))}
        />
        <SegmentedControl
          value={status}
          onChange={value => setStatus(value as StatusFilter)}
          disabled={searching}
          data={[
            { value: 'active', label: 'Aktive' },
            { value: 'planned', label: 'Planlagte' },
            { value: 'previous', label: 'Tidligere' },
            { value: 'all', label: 'Alle' },
          ]}
        />
      </Group>
      {searching && (
        <Text size="sm" c="dimmed" mb="md">
          Søket gjelder alle kull, også tidligere.
        </Text>
      )}

      {shown.length === 0 && (
        <Text c="dimmed">{status === 'active' && !searching && !breed ? 'Ingen aktive kull. Velg «Tidligere» eller «Alle» for å se de gamle.' : 'Ingen kull passer filteret.'}</Text>
      )}

      <Stack>
        {shown.map(litter => (
          <LitterAdminCard
            key={litter.id}
            litter={litter}
            cats={catsById}
            // Å koble en katt til et kull endrer katten også, så begge listene hentes på nytt.
            onChanged={() => {
              litters.reload()
              cats.reload()
            }}
          />
        ))}
      </Stack>

      <Modal opened={creating} onClose={() => setCreating(false)} title="Nytt kull">
        {creating && (
          <LitterForm
            cats={allCats}
            onCancel={() => setCreating(false)}
            onSaved={() => {
              setCreating(false)
              litters.reload()
            }}
          />
        )}
      </Modal>
    </>
  )
}
