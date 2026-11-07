import { Badge, Card, Group, Image, Text } from '@mantine/core'
import { Link } from 'react-router'
import { imageUrl, useApi } from '../api/api'
import type { CatImage, Post } from '../types/types'
import { formatDate, postKindLabel } from '../utils/format'

/** Et innlegg i lista: første bilde, dato, type, tittel og starten på teksten. Hele kortet er en lenke. */
export default function PostCard({ post }: { post: Post }) {
  const { data: images } = useApi<CatImage[]>(`/api/public/posts/${post.id}/images`)
  const first = images?.[0]

  return (
    <Card withBorder radius="md" padding="lg" component={Link} to={`/nyheter/${post.id}`} className="fade-up">
      {first && (
        <Card.Section mb="md">
          <Image src={imageUrl('posts', post.id, first.imageId, 'thumb')} h={200} alt="" loading="lazy" />
        </Card.Section>
      )}
      <Group gap="xs" mb={4}>
        <Badge variant="light">{postKindLabel(post.kind)}</Badge>
        <Text size="sm" c="dimmed">
          {formatDate(post.date)}
        </Text>
      </Group>
      <Text fw={600} size="lg" mb={4}>
        {post.title}
      </Text>
      {post.body && (
        <Text size="sm" lineClamp={3}>
          {post.body}
        </Text>
      )}
    </Card>
  )
}
