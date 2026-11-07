import { Stack, Text, Title } from '@mantine/core'
import { useApi } from '../api/api'
import CatGrid from '../components/CatGrid'
import { ErrorMessage, Loading } from '../components/Status'
import type { Cat } from '../types/types'

/** Kattene som har vært en del av oppdrettet. Sist døde først (sortert i API-et). */
export default function MemorialPage() {
  const { data: cats, loading, error } = useApi<Cat[]>('/api/public/cats/memorial')

  return (
    <Stack gap="lg">
      <title>Til minne – Lady Rania’s</title>

      <Stack gap={4}>
        <Title order={2} fz={52}>
          Til minne
        </Title>
        <Text c="dimmed">Kattene som har vært en del av oppdrettet vårt, og som vi savner.</Text>
      </Stack>

      {loading && <Loading />}
      {error && <ErrorMessage message={error} />}
      {cats && cats.length === 0 && <Text c="dimmed">Ingen her.</Text>}
      {cats && cats.length > 0 && <CatGrid cats={cats} />}
    </Stack>
  )
}
