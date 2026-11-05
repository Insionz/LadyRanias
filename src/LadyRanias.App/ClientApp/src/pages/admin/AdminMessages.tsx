import { useState } from 'react'
import { Alert, Anchor, Badge, Card, Group, Select, Stack, Text } from '@mantine/core'
import { send, useApi } from '../../api/api'
import ConfirmButton from '../../components/ConfirmButton'
import { ErrorMessage, Loading } from '../../components/Status'
import type { ContactMessage, ContactStatus } from '../../types/types'
import { contactStatusBadge, formatDateTime } from '../../utils/format'

const statuses: ContactStatus[] = ['New', 'Read', 'Answered']

/** mailto-lenke som åpner e-postprogrammet med svar til den som skrev. */
function replyLink(message: ContactMessage) {
  const subject = `Sv: ${message.subject ?? 'Din henvendelse til LadyRanias'}`
  return `mailto:${message.email}?subject=${encodeURIComponent(subject)}`
}

export default function AdminMessages() {
  const messages = useApi<ContactMessage[]>('/api/admin/contact')
  const [error, setError] = useState<string | null>(null)

  async function run(action: () => Promise<unknown>) {
    setError(null)
    try {
      await action()
      messages.reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Noe gikk galt')
    }
  }

  if (messages.loading) return <Loading />
  if (messages.error) return <ErrorMessage message={messages.error} />

  const all = messages.data ?? []
  const unread = all.filter(m => m.status === 'New').length

  return (
    <Stack>
      <Text c="dimmed">
        {all.length} meldinger{unread > 0 && `, ${unread} nye`}
      </Text>

      {error && <Alert color="red">{error}</Alert>}

      {all.length === 0 && <Text c="dimmed">Ingen meldinger ennå.</Text>}

      {all.map(message => {
        const badge = contactStatusBadge(message.status)
        return (
          <Card key={message.id} withBorder radius="md" padding="lg" style={{ opacity: message.status === 'Answered' ? 0.7 : 1 }}>
            <Stack gap="xs">
              <Group justify="space-between" align="flex-start" wrap="wrap">
                <div>
                  <Group gap="xs">
                    <Text fw={700}>{message.name}</Text>
                    <Badge color={badge.color} variant="light">
                      {badge.label}
                    </Badge>
                  </Group>
                  <Text size="sm" c="dimmed">
                    {formatDateTime(message.createdAt)}
                    {message.subject && ` · ${message.subject}`}
                  </Text>
                </div>
                <Group gap="xs">
                  <Select
                    size="xs"
                    w={130}
                    aria-label="Status"
                    allowDeselect={false}
                    value={message.status}
                    data={statuses.map(s => ({ value: s, label: contactStatusBadge(s).label }))}
                    onChange={value => {
                      if (value && value !== message.status) {
                        void run(() => send('PUT', `/api/admin/contact/${message.id}/status`, { status: value }))
                      }
                    }}
                  />
                  <ConfirmButton
                    label="Slett"
                    question={`Slette meldingen fra ${message.name}?`}
                    onConfirm={() => run(() => send('DELETE', `/api/admin/contact/${message.id}`))}
                  />
                </Group>
              </Group>

              <Text size="sm">
                <Anchor href={replyLink(message)}>{message.email}</Anchor>
                {message.phone && (
                  <>
                    {' · '}
                    <Anchor href={`tel:${message.phone.replace(/\s/g, '')}`}>{message.phone}</Anchor>
                  </>
                )}
              </Text>

              {/* pre-line beholder linjeskiftene. Teksten vises som tekst, aldri som HTML (React gjør det trygt). */}
              <Text style={{ whiteSpace: 'pre-line' }}>{message.message}</Text>

              {!message.emailSentAt && (
                <Text size="xs" c="dimmed">
                  E-postvarsel er ikke sendt ennå.
                </Text>
              )}
            </Stack>
          </Card>
        )
      })}
    </Stack>
  )
}
