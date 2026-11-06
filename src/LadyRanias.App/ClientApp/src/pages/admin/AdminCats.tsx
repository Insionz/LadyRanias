import { useState } from 'react'
import { Alert, Badge, Button, Group, Modal, Table, Text } from '@mantine/core'
import { send, useApi } from '../../api/api'
import ConfirmButton from '../../components/ConfirmButton'
import { ErrorMessage, Loading } from '../../components/Status'
import type { Cat } from '../../types/types'
import { breedLabel, formatDate, roleLabel, sexLabel } from '../../utils/format'
import CatForm from './CatForm'
import CatImagesEditor from './CatImagesEditor'

export default function AdminCats() {
  const cats = useApi<Cat[]>('/api/admin/cats')
  // 'new' = ny katt, en Cat = rediger den, null = ingen skjema åpent
  const [editing, setEditing] = useState<Cat | 'new' | null>(null)
  const [imagesFor, setImagesFor] = useState<Cat | null>(null)
  const [error, setError] = useState<string | null>(null)

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

  return (
    <>
      <Group justify="space-between" mb="md">
        <Text c="dimmed">{cats.data?.length ?? 0} katter</Text>
        <Button onClick={() => setEditing('new')}>Ny katt</Button>
      </Group>

      {missingBreed > 0 && (
        <Alert color="yellow" mb="md">
          {missingBreed === 1 ? 'Én katt mangler rase.' : `${missingBreed} katter mangler rase.`} Trykk «Rediger» og velg rase.
        </Alert>
      )}

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
            {cats.data?.map(cat => (
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
                  {roleLabel(cat.role)}
                  {cat.deathDate && (
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
