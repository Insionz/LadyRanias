import { useState, type FormEvent } from 'react'
import { Alert, Button, Container, Stack, Text, TextInput, Textarea, Title } from '@mantine/core'
import { useSearchParams } from 'react-router'
import { send } from '../api/api'

export default function ContactPage() {
  // ?emne=… kommer fra lenkene «Spør om …» og «Meld interesse» på kull-siden.
  const [searchParams] = useSearchParams()

  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [subject, setSubject] = useState(searchParams.get('emne') ?? '')
  const [message, setMessage] = useState('')
  const [website, setWebsite] = useState('') // honeypot
  const [sending, setSending] = useState(false)
  const [sent, setSent] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSending(true)
    setError(null)
    try {
      await send('POST', '/api/public/contact', { name, email, phone, subject, message, website })
      setSent(true)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kunne ikke sende meldingen')
    } finally {
      setSending(false)
    }
  }

  return (
    <Container size="sm" className="fade-up">
      <title>Kontakt – LadyRanias</title>
      <Title order={2} fz={52} mb="md">
        Kontakt
      </Title>

      {sent ? (
        <Alert color="green" title="Takk for meldingen!">
          Vi svarer så snart vi kan, vanligvis innen et par dager.
        </Alert>
      ) : (
        <form onSubmit={submit}>
          <Stack>
            <Text>Lurer du på noe om kattene våre eller planlagte kull? Skriv til oss her.</Text>

            {error && <Alert color="red">{error}</Alert>}

            <TextInput label="Navn" required maxLength={100} autoComplete="name" value={name} onChange={e => setName(e.currentTarget.value)} />
            <TextInput
              label="E-post"
              type="email"
              required
              maxLength={200}
              autoComplete="email"
              value={email}
              onChange={e => setEmail(e.currentTarget.value)}
            />
            <TextInput
              label="Telefon"
              description="Valgfritt"
              type="tel"
              maxLength={30}
              autoComplete="tel"
              value={phone}
              onChange={e => setPhone(e.currentTarget.value)}
            />
            <TextInput label="Emne" description="Valgfritt" maxLength={200} value={subject} onChange={e => setSubject(e.currentTarget.value)} />
            <Textarea
              label="Melding"
              required
              minLength={10}
              maxLength={4000}
              autosize
              minRows={5}
              value={message}
              onChange={e => setMessage(e.currentTarget.value)}
            />

            {/*
              Honeypot: skjult for mennesker (også for skjermlesere og tastatur), men enkle roboter fyller ut alt.
              Ikke display:none, for det hopper noen roboter over.
            */}
            <div aria-hidden="true" style={{ position: 'absolute', left: '-10000px', width: 1, height: 1, overflow: 'hidden' }}>
              <label>
                Nettside
                <input type="text" name="website" tabIndex={-1} autoComplete="off" value={website} onChange={e => setWebsite(e.currentTarget.value)} />
              </label>
            </div>

            <Text size="xs" c="dimmed">
              Vi bruker opplysningene bare til å svare deg. Meldingen slettes automatisk etter ett år, eller før hvis du ber om det.
            </Text>

            <Button type="submit" loading={sending} style={{ alignSelf: 'flex-start' }}>
              Send melding
            </Button>
          </Stack>
        </form>
      )}
    </Container>
  )
}
