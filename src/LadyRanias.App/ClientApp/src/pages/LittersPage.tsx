import { Anchor, Card, Divider, Group, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { Link } from 'react-router'
import { useApi } from '../api/api'
import KittenCard from '../components/KittenCard'
import Parents from '../components/Parents'
import { ErrorMessage, Loading } from '../components/Status'
import type { Litter } from '../types/types'
import { ageLabel, formatDate, formatMonth } from '../utils/format'
import { contactLink } from '../utils/contactLink'

function PlannedLitter({ litter }: { litter: Litter }) {
  return (
    <Card withBorder radius="md" padding="lg" className="fade-up">
      <Group justify="space-between" align="baseline" mb="xs">
        <Title order={3} fz={36}>
          {litter.name}
        </Title>
        <Text c="dimmed">{litter.expectedDate ? `Ventes ${formatMonth(litter.expectedDate)}` : 'Dato ikke satt'}</Text>
      </Group>
      <Parents litter={litter} />
      {litter.description && (
        <Text mt="sm" style={{ whiteSpace: 'pre-line' }}>
          {litter.description}
        </Text>
      )}
      <Anchor component={Link} to={contactLink(litter.name)} size="sm" mt="sm" display="inline-block">
        Meld interesse for {litter.name}
      </Anchor>
    </Card>
  )
}

function BornLitter({ litter }: { litter: Litter }) {
  const available = litter.kittens.filter(k => k.status === 'Available').length

  return (
    <Stack gap="sm">
      <Group justify="space-between" align="baseline">
        <Title order={3} fz={40}>
          {litter.name}
        </Title>
        {litter.birthDate && (
          <Text c="dimmed">
            Født {formatDate(litter.birthDate)} ({ageLabel(litter.birthDate)})
          </Text>
        )}
      </Group>
      <Parents litter={litter} />
      {litter.description && <Text style={{ whiteSpace: 'pre-line' }}>{litter.description}</Text>}

      {litter.kittens.length === 0 ? (
        <Text c="dimmed">Kattungene legges inn snart.</Text>
      ) : (
        <>
          <Text size="sm" fw={600}>
            {available === 0 ? 'Ingen ledige kattunger' : available === 1 ? '1 ledig kattunge' : `${available} ledige kattunger`}
          </Text>
          <SimpleGrid cols={{ base: 1, xs: 2, md: 4 }} spacing="md">
            {litter.kittens.map(kitten => (
              <KittenCard key={kitten.id} kitten={kitten} />
            ))}
          </SimpleGrid>
        </>
      )}
    </Stack>
  )
}

export default function LittersPage() {
  // Mor og far følger med i kullet, også en ekstern far som ikke står blant kattene våre.
  const litters = useApi<Litter[]>('/api/public/litters')

  if (litters.loading) return <Loading />
  if (litters.error) return <ErrorMessage message={litters.error} />

  const all = litters.data ?? []
  const planned = all.filter(l => l.status === 'Planned')
  const born = all.filter(l => l.status === 'Born')

  return (
    <Stack gap={40}>
      <title>Kull – LadyRanias</title>

      <Title order={2} fz={52}>
        Kull og kattunger
      </Title>

      {all.length === 0 && <Text c="dimmed">Det er ingen kull akkurat nå. Ta gjerne kontakt hvis du vil stå på venteliste.</Text>}

      {planned.length > 0 && (
        <Stack gap="md">
          <Title order={2} fz={40}>
            Planlagte kull
          </Title>
          <SimpleGrid cols={{ base: 1, md: 2 }} spacing="md">
            {planned.map(litter => (
              <PlannedLitter key={litter.id} litter={litter} />
            ))}
          </SimpleGrid>
        </Stack>
      )}

      {born.map((litter, index) => (
        <Stack key={litter.id} gap="md">
          {(index > 0 || planned.length > 0) && <Divider />}
          <BornLitter litter={litter} />
        </Stack>
      ))}
    </Stack>
  )
}
