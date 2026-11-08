import { Box, Button, Container, Group, Stack, Text, Title } from '@mantine/core'
import { Link } from 'react-router'
import { useApi } from '../api/api'
import CatGrid from '../components/CatGrid'
import Hero from '../components/Hero'
import PostList from '../components/PostList'
import PostText from '../components/PostText'
import { ErrorMessage, Loading } from '../components/Status'
import type { Cat, Post, SiteSettings } from '../types/types'
import classes from './HomePage.module.css'

export default function HomePage() {
  const cats = useApi<Cat[]>('/api/public/cats')
  const posts = useApi<Post[]>('/api/public/posts')
  // Velkomstteksten og forsidebildet styrer mamma selv i admin («Forside»).
  const site = useApi<SiteSettings>('/api/public/site')

  return (
    <Stack gap={48}>
      <Hero settings={site.data}>
        <Group justify="space-between" align="flex-end" gap="md">
          <Title order={2} className={classes.breed}>
            Maine Coon
          </Title>
          <Title order={2} className={classes.breed}>
            Hellig Birma
          </Title>
        </Group>
      </Hero>

      <Container size="sm" ta="center">
        <Title order={2} fz={48} mb="sm">
          Velkommen
        </Title>
        {site.data?.welcomeText && (
          <Box fz="lg">
            <PostText body={site.data.welcomeText} />
          </Box>
        )}
        {site.data?.aboutText && (
          <Button component={Link} to="/om-oss" variant="subtle" mt="sm">
            Les mer om oss
          </Button>
        )}
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

      {/* De tre siste nyhetene. Vises bare når det finnes noen. */}
      {posts.data && posts.data.length > 0 && (
        <Box>
          <Group justify="space-between" align="baseline" mb="md">
            <Title order={2} fz={44}>
              Siste nytt
            </Title>
            <Button component={Link} to="/nyheter" variant="subtle">
              Alle nyheter
            </Button>
          </Group>
          <PostList posts={posts.data.slice(0, 3)} />
        </Box>
      )}
    </Stack>
  )
}
