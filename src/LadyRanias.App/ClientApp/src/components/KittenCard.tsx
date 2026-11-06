import { Anchor, Badge, Card, Group, Image, Text } from '@mantine/core'
import { Link } from 'react-router'
import { catImageUrl, useApi } from '../api/api'
import type { CatImage, Kitten } from '../types/types'
import { breedLine, kittenStatusBadge, sexLabel } from '../utils/format'
import { contactLink } from '../utils/contactLink'

export default function KittenCard({ kitten }: { kitten: Kitten }) {
  const badge = kittenStatusBadge(kitten.status)
  const unavailable = kitten.status === 'Sold' || kitten.status === 'Kept'

  // En kattunge er en katt, så bildene ligger på samme sted som for de voksne.
  const { data: images } = useApi<CatImage[]>(`/api/public/cats/${kitten.id}/images`)
  const first = images?.[0]
  const src = first ? catImageUrl(kitten.id, first.imageId, 'thumb') : undefined

  return (
    // Solgte kattunger og de vi beholder tones litt ned, så de ledige skiller seg ut.
    <Card withBorder radius="md" padding="md" className="fade-up" style={{ opacity: unavailable ? 0.65 : 1 }}>
      <Card.Section mb="sm">
        <Image src={src} fallbackSrc="/cat-placeholder.svg" h={160} alt={kitten.name} loading="lazy" />
      </Card.Section>
      <Group justify="space-between" wrap="nowrap" mb={4}>
        {/* En kattunge er en katt, så den har sin egen side med alle bildene. */}
        <Anchor component={Link} to={`/katter/${kitten.id}`} fw={600} c="inherit" truncate>
          {kitten.name}
        </Anchor>
        <Badge color={badge.color}>{badge.label}</Badge>
      </Group>
      <Text size="sm" c="dimmed" mb={kitten.description ? 'xs' : 0}>
        {[sexLabel(kitten.sex), breedLine(kitten.breed, kitten.color, kitten.emsCode)].filter(Boolean).join(' · ')}
      </Text>
      {kitten.description && <Text size="sm">{kitten.description}</Text>}
      {kitten.status === 'Available' && (
        <Anchor component={Link} to={contactLink(`Kattungen ${kitten.name}`)} size="sm" mt="xs" display="block">
          Spør om {kitten.name}
        </Anchor>
      )}
    </Card>
  )
}
