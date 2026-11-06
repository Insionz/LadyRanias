import { Badge, Card, Group, Image, Text } from '@mantine/core'
import { Link } from 'react-router'
import { catImageUrl, useApi } from '../api/api'
import type { Cat, CatImage } from '../types/types'
import { ageLabel, breedLine, lifeYears, nameWithTitles, sexLabel } from '../utils/format'

export default function CatCard({ cat }: { cat: Cat }) {
  const { data: images } = useApi<CatImage[]>(`/api/public/cats/${cat.id}/images`)

  // Nyeste bilde først fra API-et. Har katten ingen bilder, viser vi plassholderen.
  const first = images?.[0]
  const src = first ? catImageUrl(cat.id, first.imageId, 'thumb') : undefined

  return (
    <Card
      shadow="sm"
      padding="lg"
      radius="md"
      withBorder
      component={Link}
      to={`/katter/${cat.id}`}
      className="fade-up"
      aria-label={`${cat.name}, ${sexLabel(cat.sex).toLowerCase()}`}
    >
      <Card.Section>
        <Image src={src} fallbackSrc="/cat-placeholder.svg" h={220} alt={cat.name} loading="lazy" />
      </Card.Section>

      <Group justify="space-between" mt="md" mb={4} wrap="nowrap">
        <Text fw={600} size="lg" truncate>
          {nameWithTitles(cat.name, cat.titles)}
        </Text>
        <Group gap={4} wrap="nowrap">
          {cat.role === 'Retired' && !cat.deathDate && (
            <Badge variant="outline" color="gray">
              Pensjonert
            </Badge>
          )}
          <Badge variant="light">{sexLabel(cat.sex)}</Badge>
        </Group>
      </Group>

      <Text size="sm" c="dimmed" mb="xs">
        {[cat.deathDate ? lifeYears(cat.birthDate, cat.deathDate) : ageLabel(cat.birthDate), breedLine(cat.breed, cat.color)]
          .filter(Boolean)
          .join(' · ')}
      </Text>

      <Text size="sm" lineClamp={3}>
        {cat.description}
      </Text>
    </Card>
  )
}
