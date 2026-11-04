import { BackgroundImage, Box, Button, Container, Group, Stack, Text, Title } from '@mantine/core'
import { Link } from 'react-router'
import { catImageUrl, useApi } from '../api/api'
import CatGrid from '../components/CatGrid'
import { ErrorMessage, Loading } from '../components/Status'
import type { Cat, CatImage } from '../types/types'
import classes from './HomePage.module.css'

export default function HomePage() {
  const cats = useApi<Cat[]>('/api/public/cats')

  // Bakgrunnsbildet er det nyeste bildet av den nyeste katten. Ingen hardkodede id-er.
  const newestCat = cats.data?.[0]
  const images = useApi<CatImage[]>(newestCat ? `/api/public/cats/${newestCat.id}/images` : null)
  const heroImage = newestCat && images.data?.[0]
  const heroSrc = heroImage ? catImageUrl(newestCat.id, heroImage.imageId, 'large') : ''

  return (
    <Stack gap={48}>
      <BackgroundImage src={heroSrc} radius="md" className={`${classes.hero} fade-up`}>
        <div className={classes.shade} />
        <div className={classes.content}>
          <Group justify="space-between" align="flex-end" gap="md">
            <Title order={2} className={classes.breed}>
              Maine Coon
            </Title>
            <Title order={2} className={classes.breed}>
              Hellig Birma
            </Title>
          </Group>
        </div>
      </BackgroundImage>

      <Container size="sm" ta="center">
        <Title order={2} fz={48} mb="sm">
          Velkommen
        </Title>
        <Text size="lg">
          LadyRanias er et lite oppdrett av Maine Coon og Hellig Birma. Kattene våre vokser opp hjemme, med mennesker
          rundt seg hver dag.
        </Text>
      </Container>

      <Box>
        <Group justify="space-between" align="baseline" mb="md">
          <Title order={2} fz={44}>
            Våre katter
          </Title>
          <Button component={Link} to="/katter" variant="subtle">
            Se alle
          </Button>
        </Group>

        {cats.loading && <Loading />}
        {cats.error && <ErrorMessage message={cats.error} />}
        {cats.data && cats.data.length === 0 && <Text c="dimmed">Kattene kommer snart.</Text>}
        {cats.data && cats.data.length > 0 && <CatGrid cats={cats.data.slice(0, 3)} />}
      </Box>
    </Stack>
  )
}
