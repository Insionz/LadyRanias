import { useState } from 'react'
import { Alert, Badge, Button, Group, Modal, Table, Text } from '@mantine/core'
import { send, useApi } from '../../api/api'
import ConfirmButton from '../../components/ConfirmButton'
import { ErrorMessage, Loading } from '../../components/Status'
import type { Post } from '../../types/types'
import { formatDate, postKindLabel } from '../../utils/format'
import CatImagesEditor from './CatImagesEditor'
import PostForm from './PostForm'

export default function AdminPosts() {
  const posts = useApi<Post[]>('/api/admin/posts')
  // 'new' = nytt innlegg, et Post = rediger det, null = ingen skjema åpent
  const [editing, setEditing] = useState<Post | 'new' | null>(null)
  const [imagesFor, setImagesFor] = useState<Post | null>(null)
  const [error, setError] = useState<string | null>(null)

  async function remove(post: Post) {
    setError(null)
    try {
      await send('DELETE', `/api/admin/posts/${post.id}`)
      posts.reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kunne ikke slette')
    }
  }

  if (posts.loading) return <Loading />
  if (posts.error) return <ErrorMessage message={posts.error} />

  return (
    <>
      <Group justify="space-between" mb="md">
        <Text c="dimmed">{posts.data?.length ?? 0} innlegg</Text>
        <Button onClick={() => setEditing('new')}>Nytt innlegg</Button>
      </Group>

      {error && (
        <Alert color="red" mb="md">
          {error}
        </Alert>
      )}

      <Table.ScrollContainer minWidth={640}>
        <Table striped highlightOnHover verticalSpacing="sm">
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Dato</Table.Th>
              <Table.Th>Tittel</Table.Th>
              <Table.Th>Type</Table.Th>
              <Table.Th />
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {posts.data?.map(post => (
              <Table.Tr key={post.id}>
                <Table.Td style={{ whiteSpace: 'nowrap' }}>{formatDate(post.date)}</Table.Td>
                <Table.Td fw={600}>
                  {post.title}
                  {!post.published && (
                    <Badge ml="xs" variant="outline" color="yellow">
                      Utkast
                    </Badge>
                  )}
                </Table.Td>
                <Table.Td>{postKindLabel(post.kind)}</Table.Td>
                <Table.Td>
                  <Group gap="xs" justify="flex-end" wrap="nowrap">
                    <Button size="xs" variant="light" onClick={() => setEditing(post)}>
                      Rediger
                    </Button>
                    <Button size="xs" variant="light" onClick={() => setImagesFor(post)}>
                      Bilder
                    </Button>
                    <ConfirmButton label="Slett" question={`Slette «${post.title}»? Bildene slettes også.`} onConfirm={() => remove(post)} />
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </Table.ScrollContainer>

      <Modal
        opened={editing !== null}
        onClose={() => setEditing(null)}
        title={editing === 'new' ? 'Nytt innlegg' : `Rediger «${editing?.title ?? ''}»`}
        size="lg"
      >
        {editing !== null && (
          <PostForm
            key={editing === 'new' ? 'new' : editing.id}
            post={editing === 'new' ? undefined : editing}
            onCancel={() => setEditing(null)}
            onSaved={saved => {
              // Et nytt innlegg går rett videre til bildene, for det er gjerne neste steg.
              const wasNew = editing === 'new'
              setEditing(null)
              posts.reload()
              if (wasNew) setImagesFor(saved)
            }}
          />
        )}
      </Modal>

      <Modal opened={imagesFor !== null} onClose={() => setImagesFor(null)} title={`Bilder til «${imagesFor?.title ?? ''}»`} size="lg">
        {imagesFor && <CatImagesEditor key={imagesFor.id} owner="posts" ownerId={imagesFor.id} />}
      </Modal>
    </>
  )
}
