import { useState } from 'react'
import { Anchor, Badge, Group, Image, Modal, SimpleGrid, Stack, Text, Title, UnstyledButton } from '@mantine/core'
import { Link, useParams } from 'react-router'
import { imageUrl, useApi } from '../api/api'
import PostText from '../components/PostText'
import { ErrorMessage, Loading } from '../components/Status'
import type { Cat, CatImage, Post } from '../types/types'
import { formatDate, postKindLabel } from '../utils/format'
import NotFoundPage from './NotFoundPage'

/** En katt nevnt i innlegget, som lenke til siden dens. Er katten slettet eller ekstern, vises den ikke. */
function CatLink({ id }: { id: string }) {
  const cat = useApi<Cat>(`/api/public/cats/${id}`)
  if (!cat.data || cat.data.role === 'External') return null

  return (
    <Badge component={Link} to={`/katter/${id}`} variant="light" size="lg" style={{ cursor: 'pointer', textTransform: 'none' }}>
      {cat.data.name}
    </Badge>
  )
}

export default function PostPage() {
  const { id } = useParams()
  const post = useApi<Post>(id ? `/api/public/posts/${id}` : null)
  const images = useApi<CatImage[]>(post.data ? `/api/public/posts/${post.data.id}/images` : null)
  const [open, setOpen] = useState<CatImage | null>(null)

  if (post.notFound) return <NotFoundPage />
  if (post.error) return <ErrorMessage message={post.error} />
  if (!post.data) return <Loading />

  const p = post.data
  const list = images.data ?? []

  return (
    <Stack gap="md" maw={820}>
      <title>{`${p.title} – Lady Rania’s`}</title>

      <Anchor component={Link} to="/nyheter" size="sm">
        ← Alle nyheter
      </Anchor>

      <Group gap="xs">
        <Badge variant="light">{postKindLabel(p.kind)}</Badge>
        <Text size="sm" c="dimmed">
          {formatDate(p.date)}
        </Text>
      </Group>

      <Title order={2} fz={52} lh={1.1}>
        {p.title}
      </Title>

      {p.catIds.length > 0 && (
        <Group gap="xs">
          {p.catIds.map(catId => (
            <CatLink key={catId} id={catId} />
          ))}
        </Group>
      )}

      {list[0] && (
        <UnstyledButton onClick={() => setOpen(list[0])} aria-label="Vis bildet stort">
          <Image src={imageUrl('posts', p.id, list[0].imageId, 'large')} radius="md" mah="60vh" fit="contain" alt="" />
        </UnstyledButton>
      )}

      {p.body && <PostText body={p.body} />}

      {list.length > 1 && (
        <SimpleGrid cols={{ base: 3, sm: 4 }} spacing="xs">
          {list.slice(1).map(image => (
            <UnstyledButton key={image.imageId} onClick={() => setOpen(image)} aria-label="Vis bildet stort">
              <Image src={imageUrl('posts', p.id, image.imageId, 'thumb')} radius="sm" h={110} alt="" loading="lazy" />
            </UnstyledButton>
          ))}
        </SimpleGrid>
      )}

      <Modal opened={open !== null} onClose={() => setOpen(null)} size="xl" title={p.title} centered>
        {open && <Image src={imageUrl('posts', p.id, open.imageId, 'large')} radius="sm" fit="contain" mah="80vh" alt="" />}
      </Modal>
    </Stack>
  )
}
