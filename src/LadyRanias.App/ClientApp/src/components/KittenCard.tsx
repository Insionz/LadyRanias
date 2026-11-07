import { Anchor, Badge, Card, Group, Image, Stack, Text } from '@mantine/core'
import { Link } from 'react-router'
import { catImageUrl, useApi } from '../api/api'
import type { CatImage, Kitten } from '../types/types'
import { breedLine, kittenStatusBadge, sexLabel } from '../utils/format'
import { contactLink } from '../utils/contactLink'

export default function KittenCard({ kitten }: { kitten: Kitten }) {
  const badge = kittenStatusBadge(kitten.status)
  // Solgte kattunger tones litt ned, så de ledige skiller seg ut. De som blir hos oss, er gode nyheter og vises som vanlig.
  const sold = kitten.status === 'Sold'

  // En kattunge er en katt, så bildene ligger på samme sted som for de voksne.
  const { data: images } = useApi<CatImage[]>(`/api/public/cats/${kitten.id}/images`)
  const first = images?.[0]
  const src = first ? catImageUrl(kitten.id, first.imageId, 'thumb') : undefined

  return (
    <Stack gap={6}>
      {/* Hele kortet er en lenke til kattungens egen side, med alle bildene uke for uke. Som kattekortene. */}
      <Card
        withBorder
        radius="md"
        padding="md"
        className="fade-up"
        component={Link}
        to={`/katter/${kitten.id}`}
        aria-label={`${kitten.name}, ${badge.label.toLowerCase()}`}
        style={{ opacity: sold ? 0.65 : 1 }}
      >
        <Card.Section mb="sm">
          <Image src={src} fallbackSrc="/cat-placeholder.svg" h={160} alt={kitten.name} loading="lazy" />
        </Card.Section>
        <Group justify="space-between" wrap="nowrap" mb={4}>
          <Text fw={600} truncate>
            {kitten.name}
          </Text>
          <Badge color={badge.color}>{badge.label}</Badge>
        </Group>
        <Text size="sm" c="dimmed" mb={kitten.description ? 'xs' : 0}>
          {[sexLabel(kitten.sex), breedLine(kitten.breed, kitten.color, kitten.emsCode)].filter(Boolean).join(' · ')}
        </Text>
        {kitten.description && (
          <Text size="sm" lineClamp={3}>
            {kitten.description}
          </Text>
        )}
      </Card>

      {/* Utenfor kortet: en lenke inni en lenke er ikke lov i HTML. */}
      {kitten.status === 'Available' && (
        <Anchor component={Link} to={contactLink(`Kattungen ${kitten.name}`)} size="sm">
          Spør om {kitten.name}
        </Anchor>
      )}
    </Stack>
  )
}
