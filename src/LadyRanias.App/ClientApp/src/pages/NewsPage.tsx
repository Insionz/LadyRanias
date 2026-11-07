import { useState } from 'react'
import { Group, Select, Stack, Title } from '@mantine/core'
import { useApi } from '../api/api'
import PostList from '../components/PostList'
import { ErrorMessage, Loading } from '../components/Status'
import type { Post, PostKind } from '../types/types'
import { postKindLabel, postKinds } from '../utils/format'

export default function NewsPage() {
  const posts = useApi<Post[]>('/api/public/posts')
  const [kind, setKind] = useState<PostKind | null>(null)

  const shown = (posts.data ?? []).filter(post => !kind || post.kind === kind)

  return (
    <Stack gap="lg">
      <title>Nyheter – Lady Rania’s</title>

      <Group justify="space-between" align="center">
        <Title order={2} fz={52}>
          Nyheter
        </Title>
        <Select
          aria-label="Vis bare"
          placeholder="Alle nyheter"
          clearable
          w={200}
          value={kind}
          onChange={value => setKind(value as PostKind | null)}
          data={postKinds.map(k => ({ value: k, label: postKindLabel(k) }))}
        />
      </Group>

      {posts.loading && <Loading />}
      {posts.error && <ErrorMessage message={posts.error} />}
      {posts.data && <PostList posts={shown} empty={kind ? 'Ingen nyheter av denne typen.' : 'Ingen nyheter ennå.'} />}
    </Stack>
  )
}
