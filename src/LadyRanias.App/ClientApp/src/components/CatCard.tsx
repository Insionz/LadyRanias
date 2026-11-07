import { Badge, Card, Group, Image, Text } from '@mantine/core'
import { Link } from 'react-router'
import { catImageUrl, useApi } from '../api/api'
import type { Cat, CatImage } from '../types/types'
import { ageLabel, breedLine, lifeYears, sexLabel } from '../utils/format'
import { highestTitle } from '../utils/titles'
import { Rosette } from './TitleRosettes'

export default function CatCard({ cat }: { cat: Cat }) {
  const { data: images } = useApi<CatImage[]>(`/api/public/cats/${cat.id}/images`)

  // Nyeste bilde først fra API-et. Har katten ingen bilder, viser vi plassholderen.
  const first = images?.[0]
  const best = highestTitle(cat.titles)
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
        {/* Bare den høyeste tittelen på kortet. Alle titlene står på kattesiden. */}
        <Group gap={6} wrap="nowrap" style={{ minWidth: 0 }}>
          {best && <Rosette title={best} size={26} />}
          <Text fw={600} size="lg" truncate>
            {cat.name}
          </Text>
        </Group>
        <Group gap={4} wrap="nowrap">
          {/* En kattunge vi beholder: står blant kattene våre før den går i avl. */}
          {cat.role === 'Kitten' && (
            <Badge variant="outline" color="grape">
              Ung, født her
            </Badge>
          )}
          {cat.role === 'Retired' && !cat.deceased && (
            <Badge variant="outline" color="gray">
              Pensjonert
            </Badge>
          )}
          <Badge variant="light">{sexLabel(cat.sex)}</Badge>
        </Group>
      </Group>

      <Text size="sm" c="dimmed" mb="xs">
        {[cat.deceased ? lifeYears(cat.birthDate, cat.deathDate) : ageLabel(cat.birthDate), breedLine(cat.breed, cat.color)]
          .filter(Boolean)
          .join(' · ')}
      </Text>

      <Text size="sm" lineClamp={3}>
        {cat.description}
      </Text>
    </Card>
  )
}
