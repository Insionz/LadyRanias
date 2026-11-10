import { useState } from 'react'
import { Box, Image, SimpleGrid, Stack, Text, UnstyledButton } from '@mantine/core'
import { imageUrl, useApi } from '../api/api'
import type { CatImage, Litter } from '../types/types'
import { groupByWeek } from '../utils/imageWeeks'
import Lightbox from './Lightbox'

/** Fellesbilder av hele kullet, gruppert per uke («Uke 3», «Uke 5»). Vises ikke hvis kullet ikke har noen. */
export default function LitterPhotos({ litter }: { litter: Litter }) {
  const { data: images } = useApi<CatImage[]>(`/api/public/litters/${litter.id}/images`)
  const [openIndex, setOpenIndex] = useState<number | null>(null)

  if (!images || images.length === 0) return null

  const groups = litter.birthDate ? groupByWeek(images, litter.birthDate) : [{ weeks: 0, label: '', images }]
  // Alle bildene på rad, med uka som bildetekst, så man kan bla gjennom hele kullet i fullskjerm.
  const flat = groups.flatMap(group =>
    group.images.map(image => ({ image, src: imageUrl('litters', litter.id, image.imageId, 'large'), caption: group.label || undefined })),
  )

  return (
    <Stack gap="xs">
      <Text size="sm" fw={600}>
        Hele kullet
      </Text>
      {groups.map(group => (
        <Box key={group.label}>
          {group.label && (
            <Text size="sm" c="dimmed" mb={4}>
              {group.label}
            </Text>
          )}
          <SimpleGrid cols={{ base: 3, sm: 6 }} spacing="xs">
            {group.images.map(image => (
              <UnstyledButton
                key={image.imageId}
                onClick={() => setOpenIndex(flat.findIndex(f => f.image.imageId === image.imageId))}
                aria-label={`Vis bildet fra ${group.label || 'kullet'}`}
              >
                <Image src={imageUrl('litters', litter.id, image.imageId, 'thumb')} radius="sm" h={90} alt="" loading="lazy" />
              </UnstyledButton>
            ))}
          </SimpleGrid>
        </Box>
      ))}

      <Lightbox images={flat} index={openIndex} onChange={setOpenIndex} onClose={() => setOpenIndex(null)} alt={litter.name} />
    </Stack>
  )
}
