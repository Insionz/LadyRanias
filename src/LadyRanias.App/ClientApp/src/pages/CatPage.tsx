import { useState } from 'react'
import { Anchor, Badge, Box, Group, Image, SimpleGrid, Stack, Text, Title, UnstyledButton } from '@mantine/core'
import { Link, useParams } from 'react-router'
import { catImageUrl, useApi } from '../api/api'
import { ErrorMessage, Loading } from '../components/Status'
import type { Cat, CatImage } from '../types/types'
import { ageLabel, formatDate, sexLabel } from '../utils/format'
import NotFoundPage from './NotFoundPage'

export default function CatPage() {
  const { id } = useParams()
  const cat = useApi<Cat>(id ? `/api/public/cats/${id}` : null)
  const images = useApi<CatImage[]>(cat.data ? `/api/public/cats/${cat.data.id}/images` : null)
  const [selectedId, setSelectedId] = useState<string | null>(null)

  if (cat.notFound) return <NotFoundPage />
  if (cat.error) return <ErrorMessage message={cat.error} />
  if (!cat.data) return <Loading />

  const c = cat.data
  const list = images.data ?? []
  // Valgt bilde, eller det nyeste hvis ingen er valgt ennå.
  const current = list.find(i => i.imageId === selectedId) ?? list[0]

  return (
    <>
      <title>{`${c.name} – LadyRanias`}</title>

      <Anchor component={Link} to="/katter" size="sm">
        ← Alle katter
      </Anchor>

      {/* Én kolonne på mobil, bilde og tekst ved siden av hverandre på større skjermer. */}
      <SimpleGrid cols={{ base: 1, md: 2 }} spacing="xl" mt="md" className="fade-up">
        <Box>
          <Image
            src={current ? catImageUrl(c.id, current.imageId, 'large') : undefined}
            fallbackSrc="/cat-placeholder.svg"
            radius="md"
            alt={c.name}
            mah="70vh"
            fit="contain"
          />

          {list.length > 1 && (
            <SimpleGrid cols={{ base: 4, sm: 6 }} spacing="xs" mt="sm">
              {list.map(image => (
                <UnstyledButton
                  key={image.imageId}
                  onClick={() => setSelectedId(image.imageId)}
                  aria-label="Vis dette bildet"
                  aria-pressed={image.imageId === current?.imageId}
                  style={{
                    borderRadius: 'var(--mantine-radius-sm)',
                    outline: image.imageId === current?.imageId ? '2px solid var(--mantine-primary-color-filled)' : 'none',
                    outlineOffset: 2,
                  }}
                >
                  <Image src={catImageUrl(c.id, image.imageId, 'thumb')} radius="sm" h={64} alt="" loading="lazy" />
                </UnstyledButton>
              ))}
            </SimpleGrid>
          )}
        </Box>

        <Box>
          <Stack gap="sm">
            <Title order={2} fz={64} lh={1.1}>
              {c.name}
            </Title>
            <Group gap="xs">
              <Badge variant="light">{sexLabel(c.sex)}</Badge>
              <Badge variant="outline">{ageLabel(c.birthDate)}</Badge>
            </Group>
            <Text c="dimmed">Født {formatDate(c.birthDate)}</Text>
            {/* pre-line gjør at linjeskift i beskrivelsen vises som linjeskift. */}
            <Text style={{ whiteSpace: 'pre-line' }}>{c.description}</Text>
          </Stack>
        </Box>
      </SimpleGrid>
    </>
  )
}
