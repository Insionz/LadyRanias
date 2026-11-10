import { useState, type ReactNode } from 'react'
import { Anchor, Badge, Box, Button, Group, Image, Paper, SimpleGrid, Stack, Text, Title, UnstyledButton } from '@mantine/core'
import { Link, useParams } from 'react-router'
import { catImageUrl, useApi } from '../api/api'
import { ParentName } from '../components/Parents'
import Lightbox from '../components/Lightbox'
import { Rosette, TitleList } from '../components/TitleRosettes'
import PostList from '../components/PostList'
import { ErrorMessage, Loading } from '../components/Status'
import type { Cat, CatImage, Litter, Post } from '../types/types'
import { contactLink } from '../utils/contactLink'
import { groupByWeek } from '../utils/imageWeeks'
import { parentFromCat, type ParentInfo } from '../utils/parents'
import { highestTitle, parseTitles } from '../utils/titles'
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
  const [viewerIndex, setViewerIndex] = useState<number | null>(null)

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
  const allTitles = [...titles.before, ...titles.after]
  const best = highestTitle(c.titles)

  // For en kjøpt katt: en katt i systemet, ellers navnet som er skrevet inn.
  const ownParent = (found: Cat | null, name?: string | null): ParentInfo | null =>
    found ? parentFromCat(found) : name ? { name } : null
  // Født her: mor og far fra kullet. Kjøpt: fra katten selv.
  const mother = litter.data ? litter.data.mother : ownParent(motherCat.data, c.motherName)
  const father = litter.data ? litter.data.father : ownParent(fatherCat.data, c.fatherName)

  // Bildene i den rekkefølgen de vises: per uke for en katt født her, ellers alle på rad.
  const groups = c.litterId ? groupByWeek(list, c.birthDate) : [{ weeks: 0, label: '', images: list }]
  const viewerImages = groups.flatMap(group =>
    group.images.map(image => ({ id: image.imageId, src: catImageUrl(c.id, image.imageId, 'large'), caption: group.label || undefined })),
  )
  const openViewer = (imageId: string) => setViewerIndex(viewerImages.findIndex(i => i.id === imageId))

  // Linja under navnet: «Maine Coon · Hunnkatt · 4 år».
  const summary = [
    c.breed === 'Unknown' ? null : breedLabel(c.breed),
    sexLabel(c.sex),
    deceased ? lifeYears(c.birthDate, c.deathDate) : ageLabel(c.birthDate),
  ]
    .filter(Boolean)
    .join(' · ')
  const outOfBreeding = !deceased ? outOfBreedingLabel(c) : null

  // Stamkortet: bare det som er fylt ut. En kattunge har ofte ikke farge eller EMS-kode ennå.
  const facts: [string, ReactNode][] = [
    ['Født', `${formatDate(c.birthDate)}${deceased && c.deathDate ? ` · død ${formatDate(c.deathDate)}` : ''}`],
    ['Farge', [c.color, c.emsCode].filter(Boolean).join(' · ')],
    ['Reg.nr.', c.registrationNumber ?? ''],
    // Står det allerede «Kastrert» på merket, trengs det ikke her også.
    ['Kastrert', c.neutered && !outOfBreeding ? 'Ja' : ''],
    ['Mor', mother || father ? <ParentName parent={mother} /> : ''],
    ['Far', mother || father ? <ParentName parent={father} /> : ''],
    [
      'Kull',
      litter.data ? (
        <Anchor component={Link} to="/kull">
          {litter.data.name}
        </Anchor>
      ) : (
        ''
      ),
    ],
  ]
  const shownFacts = facts.filter(([, value]) => value !== '')

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
          <Box pos="relative">
            <UnstyledButton
              display="block"
              w="100%"
              onClick={() => current && openViewer(current.imageId)}
              aria-label="Vis bildet i full skjerm"
              style={{ cursor: current ? 'zoom-in' : 'default' }}
            >
              <Image
                src={current ? catImageUrl(c.id, current.imageId, 'large') : undefined}
                fallbackSrc="/cat-placeholder.svg"
                radius="md"
                alt={c.name}
                mah="70vh"
                fit="contain"
              />
            </UnstyledButton>
            {/* Den høyeste tittelen festet på bildet, som sløyfen på buret etter en utstilling. */}
            {best && (
              <Box pos="absolute" top={12} right={12} style={{ transform: 'rotate(8deg)', pointerEvents: 'none' }}>
                <Rosette title={best} size={56} />
              </Box>
            )}
          </Box>

          {/* Et trykk på et lite bilde åpner det i full skjerm med en gang, så man slipper å rulle opp. */}
          {list.length > 1 &&
            groups.map(group => (
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
                      onClick={() => {
                        setSelectedId(image.imageId)
                        openViewer(image.imageId)
                      }}
                      aria-label="Vis bildet i full skjerm"
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

        <Stack gap="lg">
          <Stack gap={6}>
            {/*
              Navnet står i skriveskriften, men titlene gjør ikke det: forkortelser som «GIC JCH KCH» er nesten
              umulige å lese i en løkkeskrift. De står i vanlig skrift, mindre, på samme plass som i det fulle navnet.
            */}
            <Title order={2} fz={{ base: 40, sm: 52 }} lh={1.1}>
              {titles.before.length > 0 && <span style={titleStyle}>{titles.before.map(t => t.text).join(' ')} </span>}
              {c.name}
              {titles.after.length > 0 && <span style={titleStyle}> {titles.after.map(t => t.text).join(' ')}</span>}
            </Title>
            <Text c="dimmed">{summary}</Text>
            {(sale || outOfBreeding) && (
              <Group gap="xs">
                {sale && <Badge color={sale.color}>{sale.label}</Badge>}
                {outOfBreeding && (
                  <Badge variant="outline" color="gray">
                    {outOfBreeding}
                  </Badge>
                )}
              </Group>
            )}
          </Stack>

          {/* Fortellingen først. pre-line gjør at linjeskift i beskrivelsen vises som linjeskift. */}
          {c.description && (
            <Text size="lg" lh={1.7} style={{ whiteSpace: 'pre-line' }}>
              {c.description}
            </Text>
          )}

          {c.saleStatus === 'Available' && isKitten && !deceased && (
            <Button component={Link} to={contactLink(`Kattungen ${c.name}`)} size="md" style={{ alignSelf: 'flex-start' }}>
              Spør om {c.name}
            </Button>
          )}

          {/* Stamkortet: fakta for seg, med små etiketter. */}
          {(shownFacts.length > 0 || c.nrrId) && (
            <Paper bg="var(--mantine-color-gray-light)" radius="md" p="md">
              <Box style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', columnGap: 'var(--mantine-spacing-lg)', rowGap: 6 }}>
                {shownFacts.map(([label, value]) => (
                  <FactRow key={label} label={label}>
                    {value}
                  </FactRow>
                ))}
              </Box>
              {/* Den offisielle stamtavlen ligger hos NRR. Vi lenker dit i stedet for å kopiere den. */}
              {c.nrrId && (
                <Anchor href={nrrCatUrl(c.nrrId, 'pedigree')} target="_blank" rel="noopener noreferrer" size="sm" mt="sm" display="inline-block">
                  Stamtavle hos NRR ↗
                </Anchor>
              )}
            </Paper>
          )}

          {allTitles.some(t => t.kind !== 'unknown') && (
            <Stack gap="xs">
              <Text fw={600}>Titler</Text>
              <TitleList titles={allTitles} />
            </Stack>
          )}
        </Stack>
      </SimpleGrid>

      <Lightbox images={viewerImages} index={viewerIndex} onChange={setViewerIndex} onClose={() => setViewerIndex(null)} alt={c.name} />

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

/** Én rad i stamkortet: liten etikett til venstre, verdien til høyre. */
function FactRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <Text size="xs" c="dimmed" tt="uppercase" lts="0.05em" pt={3}>
        {label}
      </Text>
      <Text size="sm" component="div">
        {children}
      </Text>
    </>
  )
}
