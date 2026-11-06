import { useState, type FormEvent } from 'react'
import { Alert, Badge, Button, Card, Group, Modal, Select, Stack, Table, Text, TextInput, Title } from '@mantine/core'
import { send, useApi } from '../../api/api'
import ConfirmButton from '../../components/ConfirmButton'
import { ErrorMessage, Loading } from '../../components/Status'
import CatForm from './CatForm'
import CatImagesEditor from './CatImagesEditor'
import type { Cat, Kitten, KittenStatus, Litter, Sex } from '../../types/types'
import { formatDate, kittenStatusBadge, sexLabel } from '../../utils/format'
import { nextKittenStatuses } from '../../utils/kittenRules'

type Props = {
  litter: Litter
  cats: Map<string, Cat>
  onChanged: () => void
}

const today = new Date().toISOString().slice(0, 10)

export default function LitterAdminCard({ litter, cats, onChanged }: Props) {
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [birthDate, setBirthDate] = useState(today)
  const [kittenName, setKittenName] = useState('')
  const [kittenSex, setKittenSex] = useState<Sex | null>(null)
  const [imagesFor, setImagesFor] = useState<Kitten | null>(null)
  const [editing, setEditing] = useState<Kitten | null>(null)
  const [litterImagesOpen, setLitterImagesOpen] = useState(false)

  /** Kjører en handling mot API-et, viser feilen hvis den feiler, og laster lista på nytt hvis den lykkes. */
  async function run(action: () => Promise<unknown>) {
    setBusy(true)
    setError(null)
    try {
      await action()
      onChanged()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Noe gikk galt')
    } finally {
      setBusy(false)
    }
  }

  function markBorn(event: FormEvent) {
    event.preventDefault()
    void run(() => send('POST', `/api/admin/litters/${litter.id}/born`, { birthDate }))
  }

  function addKitten(event: FormEvent) {
    event.preventDefault()
    void run(async () => {
      await send('POST', `/api/admin/litters/${litter.id}/kittens`, { name: kittenName, sex: kittenSex })
      setKittenName('')
      setKittenSex(null)
    })
  }

  function changeStatus(kitten: Kitten, status: KittenStatus) {
    void run(() => send('PUT', `/api/admin/litters/${litter.id}/kittens/${kitten.id}/status`, { status }))
  }

  const mother = cats.get(litter.motherId)?.name ?? 'ukjent'
  const father = cats.get(litter.fatherId)?.name ?? 'ukjent'

  return (
    <Card withBorder radius="md" padding="lg">
      <Stack gap="sm">
        <Group justify="space-between" align="flex-start">
          <div>
            <Group gap="xs">
              <Title order={3} fz={32}>
                {litter.name}
              </Title>
              <Badge variant="light" color={litter.status === 'Born' ? 'green' : 'blue'}>
                {litter.status === 'Born' ? 'Født' : 'Planlagt'}
              </Badge>
            </Group>
            <Text size="sm" c="dimmed">
              Mor: {mother} · Far: {father}
              {litter.birthDate && ` · Født ${formatDate(litter.birthDate)}`}
            </Text>
          </div>
          <Group gap="xs">
            <Button size="xs" variant="light" onClick={() => setLitterImagesOpen(true)}>
              Bilder av kullet
            </Button>
            <ConfirmButton
              label="Slett kull"
              question={`Slette ${litter.name}? Kattungene og bildene slettes også.`}
              onConfirm={() => run(() => send('DELETE', `/api/admin/litters/${litter.id}`))}
            />
          </Group>
        </Group>

        {error && <Alert color="red">{error}</Alert>}

        {litter.status === 'Planned' && (
          <form onSubmit={markBorn}>
            <Group align="flex-end">
              <TextInput
                label="Fødselsdato"
                type="date"
                required
                max={today}
                value={birthDate}
                onChange={e => setBirthDate(e.currentTarget.value)}
              />
              <Button type="submit" loading={busy}>
                Marker som født
              </Button>
            </Group>
          </form>
        )}

        {litter.status === 'Born' && (
          <>
            {litter.kittens.length > 0 && (
              <Table.ScrollContainer minWidth={480}>
                <Table verticalSpacing="xs">
                  <Table.Tbody>
                    {litter.kittens.map(kitten => {
                      const badge = kittenStatusBadge(kitten.status)
                      const options = [kitten.status, ...nextKittenStatuses(kitten.status)]
                      return (
                        <Table.Tr key={kitten.id}>
                          <Table.Td fw={600}>
                            {kitten.name}
                            {kitten.deceased && (
                              <Badge ml="xs" variant="outline" color="gray">
                                Død
                              </Badge>
                            )}
                          </Table.Td>
                          <Table.Td>{sexLabel(kitten.sex)}</Table.Td>
                          <Table.Td w={180}>
                            {/* Bare lovlige overganger vises. Er den solgt, er det ingenting å velge. */}
                            <Select
                              size="xs"
                              aria-label={`Status for ${kitten.name}`}
                              value={kitten.status}
                              disabled={busy || options.length === 1}
                              allowDeselect={false}
                              data={options.map(s => ({ value: s, label: kittenStatusBadge(s).label }))}
                              onChange={value => value && value !== kitten.status && changeStatus(kitten, value as KittenStatus)}
                              leftSection={<Badge size="xs" circle color={badge.color} />}
                            />
                          </Table.Td>
                          <Table.Td>
                            <Group justify="flex-end" gap="xs">
                              <Button size="xs" variant="light" onClick={() => setEditing(kitten)}>
                                Rediger
                              </Button>
                              <Button size="xs" variant="light" onClick={() => setImagesFor(kitten)}>
                                Bilder
                              </Button>
                              <ConfirmButton
                                label="Slett"
                                question={`Slette ${kitten.name}?`}
                                onConfirm={() => run(() => send('DELETE', `/api/admin/litters/${litter.id}/kittens/${kitten.id}`))}
                              />
                            </Group>
                          </Table.Td>
                        </Table.Tr>
                      )
                    })}
                  </Table.Tbody>
                </Table>
              </Table.ScrollContainer>
            )}

            <form onSubmit={addKitten}>
              <Group align="flex-end" wrap="wrap">
                <TextInput label="Ny kattunge" placeholder="Navn" required value={kittenName} onChange={e => setKittenName(e.currentTarget.value)} />
                <Select
                  label="Kjønn"
                  required
                  w={140}
                  value={kittenSex}
                  onChange={value => setKittenSex(value as Sex | null)}
                  data={[
                    { value: 'Female', label: 'Hunnkatt' },
                    { value: 'Male', label: 'Hannkatt' },
                  ]}
                />
                <Button type="submit" loading={busy} disabled={!kittenSex}>
                  Legg til
                </Button>
              </Group>
            </form>
          </>
        )}
      </Stack>

      {/* Samme skjema som for de voksne kattene: farge, EMS-kode og, for en beholdt kattunge, ny rolle. */}
      <Modal opened={editing !== null} onClose={() => setEditing(null)} title={`Rediger ${editing?.name ?? ''}`} size="lg">
        {editing && (
          <KittenEditor
            key={editing.id}
            kittenId={editing.id}
            onCancel={() => setEditing(null)}
            onSaved={() => {
              setEditing(null)
              onChanged()
            }}
          />
        )}
      </Modal>

      {/* Fellesbilder av hele kullet. Samme editor, men bildene hører til kullet og ikke til én kattunge. */}
      <Modal opened={litterImagesOpen} onClose={() => setLitterImagesOpen(false)} title={`Bilder av ${litter.name}`} size="lg">
        {litterImagesOpen && <CatImagesEditor owner="litters" ownerId={litter.id} birthDate={litter.birthDate} />}
      </Modal>

      {/* Samme bildeeditor som for de voksne kattene. En kattunge er en katt, med samme id. */}
      <Modal opened={imagesFor !== null} onClose={() => setImagesFor(null)} title={`Bilder av ${imagesFor?.name ?? ''}`} size="lg">
        {imagesFor && <CatImagesEditor key={imagesFor.id} ownerId={imagesFor.id} birthDate={litter.birthDate} />}
      </Modal>
    </Card>
  )
}

/** Kullet kjenner bare navn, kjønn og status. Skjemaet trenger hele katten, så den hentes her. */
function KittenEditor({ kittenId, onSaved, onCancel }: { kittenId: string; onSaved: () => void; onCancel: () => void }) {
  const cat = useApi<Cat>(`/api/admin/cats/${kittenId}`)

  if (cat.loading) return <Loading />
  if (cat.error || !cat.data) return <ErrorMessage message={cat.error ?? 'Fant ikke kattungen'} />

  return <CatForm cat={cat.data} onSaved={onSaved} onCancel={onCancel} />
}
