import { useState, type FormEvent } from 'react'
import { Alert, Button, Checkbox, Group, MultiSelect, Select, SimpleGrid, Stack, TextInput, Textarea } from '@mantine/core'
import { send, useApi } from '../../api/api'
import type { Cat, Post, PostKind } from '../../types/types'
import { postKindLabel, postKinds } from '../../utils/format'

type Props = {
  post?: Post // mangler det, lager vi et nytt innlegg
  onSaved: (post: Post) => void
  onCancel: () => void
}

const today = new Date().toISOString().slice(0, 10)

export default function PostForm({ post, onSaved, onCancel }: Props) {
  const cats = useApi<Cat[]>('/api/admin/cats')
  const [title, setTitle] = useState(post?.title ?? '')
  const [date, setDate] = useState(post?.date ?? today)
  const [kind, setKind] = useState<PostKind>(post?.kind ?? 'Other')
  const [catIds, setCatIds] = useState<string[]>(post?.catIds ?? [])
  const [body, setBody] = useState(post?.body ?? '')
  const [published, setPublished] = useState(post?.published ?? false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError(null)

    const data = { title, date, kind, catIds, body, published }
    try {
      const saved = post
        ? await send<Post>('PUT', `/api/admin/posts/${post.id}`, data)
        : await send<Post>('POST', '/api/admin/posts', data)
      if (saved) onSaved(saved)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kunne ikke lagre')
    } finally {
      setSaving(false)
    }
  }

  // Alle kattene i admin-lista, også døde og eksterne: et innlegg kan handle om en utstilling for lenge siden.
  const catOptions = (cats.data ?? [])
    .slice()
    .sort((a, b) => a.name.localeCompare(b.name, 'nb'))
    .map(c => ({ value: c.id, label: c.deceased ? `${c.name} (død)` : c.name }))

  return (
    <form onSubmit={submit}>
      <Stack>
        {error && <Alert color="red">{error}</Alert>}
        <TextInput label="Tittel" required maxLength={150} value={title} onChange={e => setTitle(e.currentTarget.value)} />
        <SimpleGrid cols={{ base: 1, sm: 2 }}>
          <TextInput
            label="Dato"
            description="Dagen det skjedde. Innleggene sorteres etter den."
            type="date"
            required
            value={date}
            onChange={e => setDate(e.currentTarget.value)}
          />
          <Select
            label="Type"
            allowDeselect={false}
            value={kind}
            onChange={value => value && setKind(value as PostKind)}
            data={postKinds.map(k => ({ value: k, label: postKindLabel(k) }))}
          />
        </SimpleGrid>
        <MultiSelect
          label="Katter"
          description="Innlegget vises også på sidene til disse kattene"
          placeholder={catIds.length === 0 ? 'Velg katter' : undefined}
          searchable
          clearable
          value={catIds}
          onChange={setCatIds}
          data={catOptions}
        />
        <Textarea
          label="Tekst"
          description="Tom linje gir nytt avsnitt. Lenker (https://…) blir klikkbare."
          autosize
          minRows={8}
          maxLength={20000}
          value={body}
          onChange={e => setBody(e.currentTarget.value)}
        />
        <Checkbox
          label="Publisert"
          description="Uten avkrysning er innlegget et utkast som bare du ser"
          checked={published}
          onChange={e => setPublished(e.currentTarget.checked)}
        />
        <Group justify="flex-end">
          <Button variant="default" onClick={onCancel} disabled={saving}>
            Avbryt
          </Button>
          <Button type="submit" loading={saving}>
            Lagre
          </Button>
        </Group>
      </Stack>
    </form>
  )
}
