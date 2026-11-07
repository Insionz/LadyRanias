import { SimpleGrid, Text } from '@mantine/core'
import type { Post } from '../types/types'
import PostCard from './PostCard'

export default function PostList({ posts, empty = 'Ingen nyheter ennå.' }: { posts: Post[]; empty?: string }) {
  if (posts.length === 0) return <Text c="dimmed">{empty}</Text>

  return (
    <SimpleGrid cols={{ base: 1, sm: 2, md: 3 }} spacing="lg">
      {posts.map(post => (
        <PostCard key={post.id} post={post} />
      ))}
    </SimpleGrid>
  )
}
