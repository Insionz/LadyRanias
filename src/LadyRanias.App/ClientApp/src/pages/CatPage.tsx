import { useState } from 'react'
import { Anchor, Badge, Box, Group, Image, SimpleGrid, Stack, Table, Text, Title, UnstyledButton } from '@mantine/core'
import { Link, useParams } from 'react-router'
import { catImageUrl, useApi } from '../api/api'
import Parents from '../components/Parents'
import { ErrorMessage, Loading } from '../components/Status'
import type { Cat, CatImage, Litter } from '../types/types'
import { contactLink } from '../utils/contactLink'
import { ageLabel, breedLabel, formatDate, kittenStatusBadge, nameWithTitles, sexLabel } from '../utils/format'
import NotFoundPage from './NotFoundPage'

export default function CatPage() {
  const { id } = useParams()
  const cat = useApi<Cat>(id ? `/api/public/cats/${id}` : null)
  const images = useApi<CatImage[]>(cat.data ? `/api/public/cats/${cat.data.id}/images` : null)
  // Født her? Da viser vi kullet: mor, far og en lenke dit.
  const litter = useApi<Litter>(cat.data?.litterId ? `/api/public/litters/${cat.data.litterId}` : null)
  const [selectedId, setSelectedId] = useState<string | null>(null)

  if (cat.notFound) return <NotFoundPage />
  if (cat.error) return <ErrorMessage message={cat.error} />
  if (!cat.data) return <Loading />

  const c = cat.data
  const list = images.data ?? []
  // Valgt bilde, eller det nyeste hvis ingen er valgt ennå.
  const current = list.find(i => i.imageId === selectedId) ?? list[0]
  const isKitten = c.role === 'Kitten'
  const sale = isKitten && c.saleStatus ? kittenStatusBadge(c.saleStatus) : null

  // Bare det som er fylt ut. En kattunge har ofte ikke farge eller EMS-kode ennå.
  const facts: [string, string][] = [
    ['Rase', c.breed === 'Unknown' ? '' : breedLabel(c.breed)],
    ['Farge', c.color ?? ''],
    ['EMS-kode', c.emsCode ?? ''],
    ['Titler', c.titles ?? ''],
    ['Registreringsnummer', c.registrationNumber ?? ''],
    ['Kastrert', c.neutered ? 'Ja' : ''],
  ].filter((fact): fact is [string, string] => fact[1] !== '')

  return (
    <>
      <title>{`${c.name} – LadyRanias`}</title>

      {isKitten ? (
        <Anchor component={Link} to="/kull" size="sm">
          ← Kull og kattunger
        </Anchor>
      ) : (
        <Anchor component={Link} to="/katter" size="sm">
          ← Alle katter
        </Anchor>
      )}

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
              {nameWithTitles(c.name, c.titles)}
            </Title>
            <Group gap="xs">
              {sale && <Badge color={sale.color}>{sale.label}</Badge>}
              <Badge variant="light">{sexLabel(c.sex)}</Badge>
              <Badge variant="outline">{ageLabel(c.birthDate)}</Badge>
              {c.role === 'Retired' && (
                <Badge variant="outline" color="gray">
                  Pensjonert
                </Badge>
              )}
            </Group>
            <Text c="dimmed">Født {formatDate(c.birthDate)}</Text>

            {litter.data && (
              <Stack gap={2}>
                <Parents litter={litter.data} />
                <Text size="sm">
                  Født i{' '}
                  <Anchor component={Link} to="/kull">
                    {litter.data.name}
                  </Anchor>
                </Text>
              </Stack>
            )}

            {facts.length > 0 && (
              <Table withRowBorders={false} verticalSpacing={4} horizontalSpacing={0} w="auto">
                <Table.Tbody>
                  {facts.map(([label, value]) => (
                    <Table.Tr key={label}>
                      <Table.Td c="dimmed" pr="lg" w={1} style={{ whiteSpace: 'nowrap' }}>
                        {label}
                      </Table.Td>
                      <Table.Td>{value}</Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            )}

            {c.saleStatus === 'Available' && isKitten && (
              <Anchor component={Link} to={contactLink(`Kattungen ${c.name}`)}>
                Spør om {c.name}
              </Anchor>
            )}
            {/* pre-line gjør at linjeskift i beskrivelsen vises som linjeskift. */}
            <Text style={{ whiteSpace: 'pre-line' }}>{c.description}</Text>
          </Stack>
        </Box>
      </SimpleGrid>
    </>
  )
}
