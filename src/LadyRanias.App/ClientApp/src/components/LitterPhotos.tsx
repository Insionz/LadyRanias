import { useState } from 'react'
import { Box, Image, Modal, SimpleGrid, Stack, Text, UnstyledButton } from '@mantine/core'
import { imageUrl, useApi } from '../api/api'
import type { CatImage, Litter } from '../types/types'
import { groupByWeek } from '../utils/imageWeeks'

/** Fellesbilder av hele kullet, gruppert per uke («Uke 3», «Uke 5»). Vises ikke hvis kullet ikke har noen. */
export default function LitterPhotos({ litter }: { litter: Litter }) {
  const { data: images } = useApi<CatImage[]>(`/api/public/litters/${litter.id}/images`)
  const [open, setOpen] = useState<CatImage | null>(null)

  if (!images || images.length === 0) return null

  const groups = litter.birthDate ? groupByWeek(images, litter.birthDate) : [{ weeks: 0, label: '', images }]

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
              <UnstyledButton key={image.imageId} onClick={() => setOpen(image)} aria-label={`Vis bildet fra ${group.label || 'kullet'}`}>
                <Image src={imageUrl('litters', litter.id, image.imageId, 'thumb')} radius="sm" h={90} alt="" loading="lazy" />
              </UnstyledButton>
            ))}
          </SimpleGrid>
        </Box>
      ))}

      <Modal opened={open !== null} onClose={() => setOpen(null)} size="xl" title={litter.name} centered>
        {open && <Image src={imageUrl('litters', litter.id, open.imageId, 'large')} radius="sm" alt={litter.name} fit="contain" mah="80vh" />}
      </Modal>
    </Stack>
  )
}
