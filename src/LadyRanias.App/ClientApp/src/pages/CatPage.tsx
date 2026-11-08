import { useState } from 'react'
import { Anchor, Badge, Box, Group, Image, SimpleGrid, Stack, Table, Text, Title, UnstyledButton } from '@mantine/core'
import { Link, useParams } from 'react-router'
import { catImageUrl, useApi } from '../api/api'
import Parents, { ParentsLine } from '../components/Parents'
import TitleRosettes from '../components/TitleRosettes'
import PostList from '../components/PostList'
import { ErrorMessage, Loading } from '../components/Status'
import type { Cat, CatImage, Litter, Post } from '../types/types'
import { contactLink } from '../utils/contactLink'
import { groupByWeek } from '../utils/imageWeeks'
import { parentFromCat, type ParentInfo } from '../utils/parents'
import { parseTitles } from '../utils/titles'
import { nrrCatUrl } from '../utils/nrr'
import { ageLabel, breedLabel, formatDate, kittenStatusBadge, lifeYears, outOfBreedingLabel, sexLabel } from '../utils/format'
import NotFoundPage from './NotFoundPage'

/** Titlene i navnet: vanlig skrift, mindre og litt sperret, så hver bokstav kan leses. */
const titleStyle: React.CSSProperties = {
  fontFamily: 'var(--mantine-font-family)',
  fontSize: '0.38em',
  fontWeight: 600,
  letterSpacing: '0.06em',
  verticalAlign: 'middle',
  color: 'var(--mantine-color-dimmed)',
}

export default function CatPage() {
  const { id } = useParams()
  const cat = useApi<Cat>(id ? `/api/public/cats/${id}` : null)
  const images = useApi<CatImage[]>(cat.data ? `/api/public/cats/${cat.data.id}/images` : null)
  // Født her? Da viser vi kullet: mor, far og en lenke dit.
  const litter = useApi<Litter>(cat.data?.litterId ? `/api/public/litters/${cat.data.litterId}` : null)
  // Kjøpt katt: mor og far er valgt fra kattene i systemet (eller skrevet inn som navn).
  const posts = useApi<Post[]>(cat.data ? `/api/public/posts?catId=${cat.data.id}` : null)
  const motherCat = useApi<Cat>(cat.data && !cat.data.litterId && cat.data.motherId ? `/api/public/cats/${cat.data.motherId}` : null)
  const fatherCat = useApi<Cat>(cat.data && !cat.data.litterId && cat.data.fatherId ? `/api/public/cats/${cat.data.fatherId}` : null)
  const [selectedId, setSelectedId] = useState<string | null>(null)

  if (cat.notFound) return <NotFoundPage />
  if (cat.error) return <ErrorMessage message={cat.error} />
  if (!cat.data) return <Loading />

  const c = cat.data
  const list = images.data ?? []
  // Valgt bilde, eller det nyeste hvis ingen er valgt ennå.
  const current = list.find(i => i.imageId === selectedId) ?? list[0]
  const isKitten = c.role === 'Kitten'
  const deceased = c.deceased
  const sale = isKitten && c.saleStatus && !deceased ? kittenStatusBadge(c.saleStatus) : null
  const titles = parseTitles(c.titles)

  // For en kjøpt katt: en katt i systemet, ellers navnet som er skrevet inn.
  const ownParent = (found: Cat | null, name?: string | null): ParentInfo | null =>
    found ? parentFromCat(found) : name ? { name } : null
  const mother = ownParent(motherCat.data, c.motherName)
  const father = ownParent(fatherCat.data, c.fatherName)

  // Bare det som er fylt ut. En kattunge har ofte ikke farge eller EMS-kode ennå.
  const facts: [string, string][] = [
    ['Rase', c.breed === 'Unknown' ? '' : breedLabel(c.breed)],
    ['Farge', c.color ?? ''],
    ['EMS-kode', c.emsCode ?? ''],
    ['Registreringsnummer', c.registrationNumber ?? ''],
    // Står det allerede «Kastrert» på merket, trengs det ikke i tabellen også.
    ['Kastrert', c.neutered && (c.role !== 'Retired' || deceased) ? 'Ja' : ''],
  ].filter((fact): fact is [string, string] => fact[1] !== '')

  return (
    <>
      <title>{`${c.name} – Lady Rania’s`}</title>

      {deceased && !isKitten ? (
        <Anchor component={Link} to="/minne" size="sm">
          ← Til minne
        </Anchor>
      ) : isKitten ? (
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

          {/* Født her: bildene gruppert per uke, så man ser kattungen vokse. Ellers én rad med bilder. */}
          {list.length > 1 &&
            (c.litterId ? groupByWeek(list, c.birthDate) : [{ weeks: 0, label: '', images: list }]).map(group => (
              <Box key={group.label} mt="sm">
                {group.label && (
                  <Text size="sm" fw={600} mb={4}>
                    {group.label}
                  </Text>
                )}
                <SimpleGrid cols={{ base: 4, sm: 6 }} spacing="xs">
                  {group.images.map(image => (
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
              </Box>
            ))}
        </Box>

        <Box>
          <Stack gap="sm">
            {/*
              Navnet står i skriveskriften, men titlene gjør ikke det: forkortelser som «GIC JC KC» er nesten
              umulige å lese i en løkkeskrift. De står i vanlig skrift, mindre, på samme plass som i det fulle navnet.
            */}
            <Title order={2} fz={{ base: 40, sm: 52 }} lh={1.1}>
              {titles.before.length > 0 && <span style={titleStyle}>{titles.before.map(t => t.text).join(' ')} </span>}
              {c.name}
              {titles.after.length > 0 && <span style={titleStyle}> {titles.after.map(t => t.text).join(' ')}</span>}
            </Title>
            <TitleRosettes titles={[...titles.before, ...titles.after]} size={44} />
            <Group gap="xs">
              {sale && <Badge color={sale.color}>{sale.label}</Badge>}
              <Badge variant="light">{sexLabel(c.sex)}</Badge>
              {deceased ? (
                <Badge variant="outline" color="gray">
                  {lifeYears(c.birthDate, c.deathDate)}
                </Badge>
              ) : (
                <Badge variant="outline">{ageLabel(c.birthDate)}</Badge>
              )}
              {outOfBreedingLabel(c) && !deceased && (
                <Badge variant="outline" color="gray">
                  {outOfBreedingLabel(c)}
                </Badge>
              )}
            </Group>
            <Text c="dimmed">
              Født {formatDate(c.birthDate)}
              {deceased && (c.deathDate ? ` · Død ${formatDate(c.deathDate)}` : ' · Død')}
            </Text>

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
            {!c.litterId && (mother || father) && <ParentsLine mother={mother} father={father} />}

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

            {/* Den offisielle stamtavlen ligger hos NRR. Vi lenker dit i stedet for å kopiere den. */}
            {c.nrrId && (
              <Anchor href={nrrCatUrl(c.nrrId, 'pedigree')} target="_blank" rel="noopener noreferrer" size="sm">
                Stamtavle hos NRR ↗
              </Anchor>
            )}

            {c.saleStatus === 'Available' && isKitten && !deceased && (
              <Anchor component={Link} to={contactLink(`Kattungen ${c.name}`)}>
                Spør om {c.name}
              </Anchor>
            )}
            {/* pre-line gjør at linjeskift i beskrivelsen vises som linjeskift. */}
            <Text style={{ whiteSpace: 'pre-line' }}>{c.description}</Text>
          </Stack>
        </Box>
      </SimpleGrid>

      {/* Innleggene mamma har skrevet om katten: utstillinger, titler, da den kom hit. */}
      {posts.data && posts.data.length > 0 && (
        <Stack gap="md" mt={48}>
          <Title order={3} fz={40}>
            Nyheter om {c.name}
          </Title>
          <PostList posts={posts.data} />
        </Stack>
      )}
    </>
  )
}
