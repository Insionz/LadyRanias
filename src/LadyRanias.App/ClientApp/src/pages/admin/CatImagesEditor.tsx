import { useState } from 'react'
import { Alert, FileInput, Image, Select, SimpleGrid, Stack, Text, TextInput } from '@mantine/core'
import { imageUrl, send, uploadFile, useApi, type ImageOwner } from '../../api/api'
import ConfirmButton from '../../components/ConfirmButton'
import type { CatImage } from '../../types/types'
import { formatDate } from '../../utils/format'
import { ageInWeeks, dateAtWeek, weekLabel } from '../../utils/imageWeeks'

const maxBytes = 10 * 1024 * 1024 // samme grense som i backend
const today = new Date().toISOString().slice(0, 10)

type Props = {
  /** Katt (også kattunge) eller hele kullet. */
  owner?: ImageOwner
  ownerId: string
  /** Fødselsdatoen til kattungen eller kullet. Da velges uke i stedet for dato. */
  birthDate?: string | null
}

/** Bildene til en katt, en kattunge eller et kull. Kattunger er katter med samme id, så de bruker «cats». */
export default function CatImagesEditor({ owner = 'cats', ownerId, birthDate }: Props) {
  const images = useApi<CatImage[]>(`/api/public/${owner}/${ownerId}/images`)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const adminUrl = `/api/admin/${owner}/${ownerId}/images`

  async function upload(files: File[]) {
    setError(null)
    const tooBig = files.find(f => f.size > maxBytes)
    if (tooBig) {
      setError(`${tooBig.name} er større enn 10 MB.`)
      return
    }

    setUploading(true)
    try {
      // Ett og ett bilde, så en feil på ett ikke stopper de andre fra å bli forsøkt.
      for (const file of files) {
        await uploadFile(adminUrl, file)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Opplastingen feilet')
    } finally {
      setUploading(false)
      images.reload()
    }
  }

  async function remove(imageId: string) {
    setError(null)
    try {
      await send('DELETE', `${adminUrl}/${imageId}`)
      images.reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kunne ikke slette')
    }
  }

  async function setTakenAt(imageId: string, takenAt: string) {
    setError(null)
    try {
      await send('PUT', `${adminUrl}/${imageId}`, { takenAt })
      images.reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kunne ikke lagre datoen')
    }
  }

  // Uke 0–16 dekker en kattunge fra fødsel til den flytter. Er bildet eldre, legges uka til i lista.
  function weekOptions(current: number) {
    const weeks = Array.from({ length: 17 }, (_, i) => i)
    if (current > 16) weeks.push(current)
    return weeks.map(w => ({ value: String(w), label: weekLabel(w) }))
  }

  return (
    <Stack>
      {error && <Alert color="red">{error}</Alert>}

      <FileInput
        label="Last opp bilder"
        description={
          birthDate
            ? 'Uka regnes ut fra datoen bildet ble tatt. Står den feil, kan du rette den under bildet.'
            : 'JPEG, PNG eller WebP, maks 10 MB. Posisjon og annen metadata fjernes automatisk.'
        }
        placeholder={uploading ? 'Laster opp …' : 'Velg bilder'}
        accept="image/jpeg,image/png,image/webp"
        multiple
        value={[]}
        disabled={uploading}
        onChange={files => void upload(files)}
      />

      {images.data && images.data.length === 0 && <Text c="dimmed">Ingen bilder ennå.</Text>}

      <SimpleGrid cols={{ base: 2, sm: 3 }} spacing="sm">
        {images.data?.map((image, index) => {
          const weeks = birthDate ? ageInWeeks(birthDate, image.takenAt) : null
          return (
            <Stack key={image.imageId} gap={4}>
              <Image src={imageUrl(owner, ownerId, image.imageId, 'thumb')} radius="sm" h={120} alt="" />
              {weeks !== null && birthDate ? (
                <Select
                  size="xs"
                  aria-label="Uke"
                  allowDeselect={false}
                  value={String(weeks)}
                  data={weekOptions(weeks)}
                  onChange={value => value !== null && void setTakenAt(image.imageId, dateAtWeek(birthDate, Number(value)))}
                />
              ) : (
                <TextInput
                  size="xs"
                  type="date"
                  aria-label="Tatt"
                  max={today}
                  value={image.takenAt}
                  onChange={e => e.currentTarget.value && void setTakenAt(image.imageId, e.currentTarget.value)}
                />
              )}
              <Stack gap={2} align="flex-start">
                <Text size="xs" c="dimmed">
                  {index === 0 ? 'Vises først · ' : ''}
                  {formatDate(image.takenAt)}
                </Text>
                <ConfirmButton label="Slett" question="Slette bildet?" onConfirm={() => remove(image.imageId)} />
              </Stack>
            </Stack>
          )
        })}
      </SimpleGrid>
    </Stack>
  )
}
