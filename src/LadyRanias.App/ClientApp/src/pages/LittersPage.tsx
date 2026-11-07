import { useState } from 'react'
import { Accordion, Anchor, Card, Divider, Group, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { Link } from 'react-router'
import { useApi } from '../api/api'
import KittenCard from '../components/KittenCard'
import LitterPhotos from '../components/LitterPhotos'
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
  // Kattunger som ikke klarte seg, vises ikke her. Admin ser dem fortsatt.
  const kittens = litter.kittens.filter(k => !k.deceased)
  const available = kittens.filter(k => k.status === 'Available').length

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

      <LitterPhotos litter={litter} />

      {kittens.length === 0 ? (
        <Text c="dimmed">Kattungene legges inn snart.</Text>
      ) : (
        <>
          <Text size="sm" fw={600}>
            {available === 0 ? 'Ingen ledige kattunger' : available === 1 ? '1 ledig kattunge' : `${available} ledige kattunger`}
          </Text>
          <SimpleGrid cols={{ base: 1, xs: 2, md: 4 }} spacing="md">
            {kittens.map(kitten => (
              <KittenCard key={kitten.id} kitten={kitten} />
            ))}
          </SimpleGrid>
        </>
      )}
    </Stack>
  )
}

const halfYearMs = 182 * 24 * 60 * 60 * 1000

/**
 * Et kull er «aktuelt» så lenge det er under et halvt år gammelt, eller har kattunger som ikke har fått et hjem ennå.
 * Resten er tidligere kull: historien, som legges sammen nederst så de nye kullene ikke drukner.
 */
function isCurrent(litter: Litter, now = Date.now()) {
  if (litter.status === 'Planned') return true
  const young = litter.birthDate != null && now - Date.parse(litter.birthDate) < halfYearMs
  const open = litter.kittens.some(k => !k.deceased && (k.status === 'Available' || k.status === 'Reserved' || k.status === 'UnderEvaluation'))
  return young || open
}

/** «A-kullet · 2018 · Kiara × Børre» */
function previousTitle(litter: Litter) {
  return [litter.name, litter.birthDate?.slice(0, 4), litter.mother && litter.father ? `${litter.mother.name} × ${litter.father.name}` : null]
    .filter(Boolean)
    .join(' · ')
}

export default function LittersPage() {
  // Bare ett tidligere kull er åpent om gangen. Innholdet (bildene) hentes først når det åpnes.
  const [openPrevious, setOpenPrevious] = useState<string | null>(null)
  // Mor og far følger med i kullet, også en ekstern far som ikke står blant kattene våre.
  const litters = useApi<Litter[]>('/api/public/litters')

  if (litters.loading) return <Loading />
  if (litters.error) return <ErrorMessage message={litters.error} />

  const all = litters.data ?? []
  const planned = all.filter(l => l.status === 'Planned')
  const born = all.filter(l => l.status === 'Born' && isCurrent(l))
  // Eldste først, så historien leses fra begynnelsen.
  const previous = all
    .filter(l => l.status === 'Born' && !isCurrent(l))
    .sort((a, b) => (a.birthDate ?? '').localeCompare(b.birthDate ?? ''))

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

      {previous.length > 0 && (
        <Stack gap="md">
          {(born.length > 0 || planned.length > 0) && <Divider />}
          <Title order={2} fz={40}>
            Tidligere kull
          </Title>
          <Accordion variant="separated" value={openPrevious} onChange={setOpenPrevious}>
            {previous.map(litter => (
              <Accordion.Item key={litter.id} value={litter.id}>
                <Accordion.Control>{previousTitle(litter)}</Accordion.Control>
                <Accordion.Panel>{openPrevious === litter.id && <BornLitter litter={litter} />}</Accordion.Panel>
              </Accordion.Item>
            ))}
          </Accordion>
        </Stack>
      )}
    </Stack>
  )
}
