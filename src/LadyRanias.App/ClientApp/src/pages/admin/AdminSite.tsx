import { useState, type FormEvent } from 'react'
import { Alert, Button, Card, Group, Image, Radio, SimpleGrid, Stack, Text, Textarea, Title, UnstyledButton } from '@mantine/core'
import { imageUrl, send, SITE_IMAGES_ID, useApi } from '../../api/api'
import { ErrorMessage, Loading } from '../../components/Status'
import type { CatImage, HeroMode, SiteSettings } from '../../types/types'
import CatImagesEditor from './CatImagesEditor'

const modes: { value: HeroMode; label: string; description: string }[] = [
  { value: 'Newest', label: 'Automatisk', description: 'Det nyeste bildet av den nyeste katten' },
  { value: 'Fixed', label: 'Ett bestemt bilde', description: 'Velg bildet under' },
  { value: 'Random', label: 'Tilfeldig', description: 'Et nytt av forsidebildene hver gang siden åpnes' },
  { value: 'Slideshow', label: 'Lysbilder', description: 'Forsidebildene toner over i hverandre' },
]

/** Skjemaet. Egen komponent, så startverdiene kan leses fra innstillingene når de er hentet. */
function SiteForm({ settings, onSaved }: { settings: SiteSettings; onSaved: () => void }) {
  const images = useApi<CatImage[]>(`/api/public/site/${SITE_IMAGES_ID}/images`)
  const [welcomeText, setWelcomeText] = useState(settings.welcomeText)
  const [aboutText, setAboutText] = useState(settings.aboutText)
  const [heroMode, setHeroMode] = useState<HeroMode>(settings.heroMode)
  const [heroImageId, setHeroImageId] = useState<string | null>(settings.heroImageId ?? null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError(null)
    setSaved(false)
    try {
      await send('PUT', '/api/admin/site', { welcomeText, aboutText, heroMode, heroImageId })
      setSaved(true)
      onSaved()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kunne ikke lagre')
    } finally {
      setSaving(false)
    }
  }

  const list = images.data ?? []
  const noImages = images.data != null && list.length === 0

  return (
    <form onSubmit={submit}>
      <Stack gap="xl">
        {error && <Alert color="red">{error}</Alert>}

        <Card withBorder radius="md" padding="lg">
          <Stack>
            <Title order={3} fz={32}>
              Forsidebildet
            </Title>
            <Radio.Group value={heroMode} onChange={value => setHeroMode(value as HeroMode)}>
              <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="xs">
                {modes.map(mode => (
                  <Radio key={mode.value} value={mode.value} label={mode.label} description={mode.description} />
                ))}
              </SimpleGrid>
            </Radio.Group>
            {heroMode !== 'Newest' && noImages && (
              <Alert color="yellow">Last opp forsidebilder under. Til da vises det nyeste bildet av den nyeste katten.</Alert>
            )}

            {/* Velg bildet ved å trykke på det. Bare når «Ett bestemt bilde» er valgt. */}
            {heroMode === 'Fixed' && list.length > 0 && (
              <>
                <Text size="sm" c="dimmed">
                  Trykk på bildet som skal vises
                </Text>
                <SimpleGrid cols={{ base: 2, sm: 4 }} spacing="xs">
                  {list.map(image => {
                    const chosen = image.imageId === heroImageId
                    return (
                      <UnstyledButton
                        key={image.imageId}
                        onClick={() => setHeroImageId(image.imageId)}
                        aria-pressed={chosen}
                        aria-label="Bruk dette bildet på forsiden"
                        style={{
                          borderRadius: 'var(--mantine-radius-sm)',
                          outline: chosen ? '3px solid var(--mantine-primary-color-filled)' : 'none',
                          outlineOffset: 2,
                        }}
                      >
                        <Image src={imageUrl('site', SITE_IMAGES_ID, image.imageId, 'thumb')} radius="sm" h={100} alt="" />
                      </UnstyledButton>
                    )
                  })}
                </SimpleGrid>
              </>
            )}

            <Text fw={600} mt="sm">
              Forsidebilder
            </Text>
            <Text size="sm" c="dimmed">
              Bilder som bare er til forsiden. Bredformat (liggende) blir finest.
            </Text>
            {/* Henter lista på nytt når bildene endres, så velgeren over er oppdatert. */}
            <CatImagesEditor owner="site" ownerId={SITE_IMAGES_ID} onChanged={images.reload} />
          </Stack>
        </Card>

        <Card withBorder radius="md" padding="lg">
          <Stack>
            <Title order={3} fz={32}>
              Tekster
            </Title>
            <Textarea
              label="Velkommen"
              description="Den korte teksten på forsiden"
              autosize
              minRows={3}
              maxLength={2000}
              value={welcomeText}
              onChange={e => setWelcomeText(e.currentTarget.value)}
            />
            <Textarea
              label="Om oss"
              description="Siden «Om oss». Tom linje gir nytt avsnitt, og lenker blir klikkbare."
              autosize
              minRows={10}
              maxLength={30000}
              value={aboutText}
              onChange={e => setAboutText(e.currentTarget.value)}
            />
          </Stack>
        </Card>

        <Group justify="flex-end">
          {saved && (
            <Text size="sm" c="green">
              Lagret
            </Text>
          )}
          <Button type="submit" loading={saving} disabled={heroMode === 'Fixed' && !heroImageId}>
            Lagre
          </Button>
        </Group>
      </Stack>
    </form>
  )
}

export default function AdminSite() {
  const site = useApi<SiteSettings>('/api/public/site')

  if (site.loading) return <Loading />
  if (site.error || !site.data) return <ErrorMessage message={site.error ?? 'Fant ikke innstillingene'} />

  return <SiteForm settings={site.data} onSaved={site.reload} />
}
