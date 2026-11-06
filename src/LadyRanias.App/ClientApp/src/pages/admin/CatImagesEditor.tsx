import { useState } from 'react'
import { Alert, FileInput, Image, SimpleGrid, Stack, Text } from '@mantine/core'
import { catImageUrl, send, uploadFile, useApi } from '../../api/api'
import ConfirmButton from '../../components/ConfirmButton'
import type { CatImage } from '../../types/types'

const maxBytes = 10 * 1024 * 1024 // samme grense som i backend

/** Bildene til en katt. Virker også for kattunger, for de er katter med samme id. */
export default function CatImagesEditor({ catId }: { catId: string }) {
  const images = useApi<CatImage[]>(`/api/public/cats/${catId}/images`)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)

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
        await uploadFile(`/api/admin/cats/${catId}/images`, file)
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
      await send('DELETE', `/api/admin/cats/${catId}/images/${imageId}`)
      images.reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kunne ikke slette')
    }
  }

  return (
    <Stack>
      {error && <Alert color="red">{error}</Alert>}

      <FileInput
        label="Last opp bilder"
        description="JPEG, PNG eller WebP, maks 10 MB. Posisjon og annen metadata fjernes automatisk."
        placeholder={uploading ? 'Laster opp …' : 'Velg bilder'}
        accept="image/jpeg,image/png,image/webp"
        multiple
        value={[]}
        disabled={uploading}
        onChange={files => void upload(files)}
      />

      {images.data && images.data.length === 0 && <Text c="dimmed">Ingen bilder ennå.</Text>}

      <SimpleGrid cols={{ base: 2, sm: 3 }} spacing="sm">
        {images.data?.map((image, index) => (
          <Stack key={image.imageId} gap={4}>
            <Image src={catImageUrl(catId, image.imageId, 'thumb')} radius="sm" h={120} alt="" />
            <Stack gap={2} align="flex-start">
              {index === 0 && (
                <Text size="xs" c="dimmed">
                  Vises først
                </Text>
              )}
              <ConfirmButton label="Slett" question="Slette bildet?" onConfirm={() => remove(image.imageId)} />
            </Stack>
          </Stack>
        ))}
      </SimpleGrid>
    </Stack>
  )
}
