import { useState } from 'react'
import { Alert, Anchor, Badge, Button, Group, Modal, SegmentedControl, Select, Table, Text, TextInput } from '@mantine/core'
import { send, useApi } from '../../api/api'
import ConfirmButton from '../../components/ConfirmButton'
import { ErrorMessage, Loading } from '../../components/Status'
import type { Breed, Cat, CatRole, Sex } from '../../types/types'
import { breedLabel, formatDate, roleLabel, sexLabel } from '../../utils/format'
import CatForm from './CatForm'
import CatImagesEditor from './CatImagesEditor'

type LifeFilter = 'alive' | 'deceased' | 'all'

const breedOptions: Breed[] = ['MaineCoon', 'SacredBirman', 'Unknown']
const roleOptions: CatRole[] = ['Breeding', 'Retired', 'External']

export default function AdminCats() {
  const cats = useApi<Cat[]>('/api/admin/cats')
  // 'new' = ny katt, en Cat = rediger den, null = ingen skjema åpent
  const [editing, setEditing] = useState<Cat | 'new' | null>(null)
  const [imagesFor, setImagesFor] = useState<Cat | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Filtrene. Alt skjer i nettleseren: lista er liten nok til at det ikke trengs noe i API-et.
  const [search, setSearch] = useState('')
  const [breed, setBreed] = useState<Breed | null>(null)
  const [role, setRole] = useState<CatRole | null>(null)
  const [sex, setSex] = useState<Sex | null>(null)
  const [life, setLife] = useState<LifeFilter>('alive')

  async function remove(cat: Cat) {
    setError(null)
    try {
      await send('DELETE', `/api/admin/cats/${cat.id}`)
      cats.reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kunne ikke slette')
    }
  }

  const missingBreed = cats.data?.filter(cat => cat.breed === 'Unknown').length ?? 0

  if (cats.loading) return <Loading />
  if (cats.error) return <ErrorMessage message={cats.error} />

  const all = cats.data ?? []
  const query = search.trim().toLowerCase()
  const shown = all
    .filter(cat => !query || cat.name.toLowerCase().includes(query) || (cat.registrationNumber ?? '').toLowerCase().includes(query))
    .filter(cat => !breed || cat.breed === breed)
    .filter(cat => !role || cat.role === role)
    .filter(cat => !sex || cat.sex === sex)
    .filter(cat => life === 'all' || (life === 'deceased' ? cat.deceased : !cat.deceased))
    .sort((a, b) => a.name.localeCompare(b.name, 'nb'))
  const filtered = search !== '' || breed !== null || role !== null || sex !== null || life !== 'alive'

  function clearFilters() {
    setSearch('')
    setBreed(null)
    setRole(null)
    setSex(null)
    setLife('alive')
  }

  return (
    <>
      <Group justify="space-between" mb="md">
        <Text c="dimmed">
          {shown.length === all.length ? `${all.length} katter` : `Viser ${shown.length} av ${all.length} katter`}
          {filtered && (
            <>
              {' · '}
              <Anchor component="button" type="button" onClick={clearFilters}>
                Nullstill filter
              </Anchor>
            </>
          )}
        </Text>
        <Button onClick={() => setEditing('new')}>Ny katt</Button>
      </Group>

      {missingBreed > 0 && (
        <Alert color="yellow" mb="md">
          {missingBreed === 1 ? 'Én katt mangler rase.' : `${missingBreed} katter mangler rase.`} Trykk «Rediger» og velg rase.{' '}
          <Anchor
            component="button"
            type="button"
            onClick={() => {
              setBreed('Unknown')
              setLife('all') // også de døde mangler gjerne rase
            }}
          >
            Vis bare dem
          </Anchor>
        </Alert>
      )}

      <Group mb="md" align="flex-end" wrap="wrap">
        <TextInput
          label="Søk"
          placeholder="Navn eller registreringsnummer"
          value={search}
          onChange={e => setSearch(e.currentTarget.value)}
          w={240}
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
        <Select
          label="Rolle"
          placeholder="Alle"
          clearable
          w={150}
          value={role}
          onChange={value => setRole(value as CatRole | null)}
          data={roleOptions.map(r => ({ value: r, label: roleLabel(r) }))}
        />
        <Select
          label="Kjønn"
          placeholder="Begge"
          clearable
          w={140}
          value={sex}
          onChange={value => setSex(value as Sex | null)}
          data={[
            { value: 'Female', label: 'Hunnkatt' },
            { value: 'Male', label: 'Hannkatt' },
          ]}
        />
        <SegmentedControl
          value={life}
          onChange={value => setLife(value as LifeFilter)}
          data={[
            { value: 'alive', label: 'Lever' },
            { value: 'deceased', label: 'Døde' },
            { value: 'all', label: 'Alle' },
          ]}
        />
      </Group>

      {error && (
        <Alert color="red" mb="md">
          {error}
        </Alert>
      )}

      <Table.ScrollContainer minWidth={720}>
        <Table striped highlightOnHover verticalSpacing="sm">
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Navn</Table.Th>
              <Table.Th>Rase</Table.Th>
              <Table.Th>Rolle</Table.Th>
              <Table.Th>Kjønn</Table.Th>
              <Table.Th>Født</Table.Th>
              <Table.Th />
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {shown.map(cat => (
              <Table.Tr key={cat.id}>
                <Table.Td fw={600}>{cat.name}</Table.Td>
                <Table.Td>
                  {cat.breed === 'Unknown' ? (
                    <Badge color="yellow" variant="light">
                      {breedLabel(cat.breed)}
                    </Badge>
                  ) : (
                    breedLabel(cat.breed)
                  )}
                </Table.Td>
                <Table.Td>
                  {/* Rollen sier hva katten var. For en død katt står det «Var i avl», så det ikke ser ut som den er i avl nå. */}
                  {cat.deceased ? `Var ${roleLabel(cat.role).toLowerCase()}` : roleLabel(cat.role)}
                  {cat.deceased && (
                    <Badge ml="xs" variant="outline" color="gray">
                      Død
                    </Badge>
                  )}
                </Table.Td>
                <Table.Td>{sexLabel(cat.sex)}</Table.Td>
                <Table.Td>{formatDate(cat.birthDate)}</Table.Td>
                <Table.Td>
                  <Group gap="xs" justify="flex-end" wrap="nowrap">
                    <Button size="xs" variant="light" onClick={() => setEditing(cat)}>
                      Rediger
                    </Button>
                    <Button size="xs" variant="light" onClick={() => setImagesFor(cat)}>
                      Bilder
                    </Button>
                    <ConfirmButton
                      label="Slett"
                      question={`Slette ${cat.name}? Bildene slettes også.`}
                      onConfirm={() => remove(cat)}
                    />
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </Table.ScrollContainer>

      <Modal
        opened={editing !== null}
        onClose={() => setEditing(null)}
        title={editing === 'new' ? 'Ny katt' : `Rediger ${editing?.name ?? ''}`}
      >
        {editing !== null && (
          <CatForm
            // Ny key for hver katt gjør at skjemaet starter med riktige verdier.
            key={editing === 'new' ? 'new' : editing.id}
            cat={editing === 'new' ? undefined : editing}
            onCancel={() => setEditing(null)}
            onSaved={() => {
              setEditing(null)
              cats.reload()
            }}
          />
        )}
      </Modal>

      <Modal opened={imagesFor !== null} onClose={() => setImagesFor(null)} title={`Bilder av ${imagesFor?.name ?? ''}`} size="lg">
        {imagesFor && <CatImagesEditor key={imagesFor.id} ownerId={imagesFor.id} />}
      </Modal>
    </>
  )
}
