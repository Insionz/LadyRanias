import { Stack, Text, Title } from '@mantine/core'
import { useApi } from '../api/api'
import PostText from '../components/PostText'
import { ErrorMessage, Loading } from '../components/Status'
import type { SiteSettings } from '../types/types'

/** «Om oss»: teksten mamma skriver selv i admin («Forside»). */
export default function AboutPage() {
  const site = useApi<SiteSettings>('/api/public/site')

  return (
    <Stack gap="lg" maw={760}>
      <title>Om oss – Lady Rania’s</title>

      <Title order={2} fz={52}>
        Om oss
      </Title>

      {site.loading && <Loading />}
      {site.error && <ErrorMessage message={site.error} />}
      {site.data && (site.data.aboutText ? <PostText body={site.data.aboutText} /> : <Text c="dimmed">Kommer snart.</Text>)}
    </Stack>
  )
}
