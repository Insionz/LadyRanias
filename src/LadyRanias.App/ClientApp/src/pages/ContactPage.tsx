import { Container, Text, Title } from '@mantine/core'

// Selve kontaktskjemaet kommer i steg 9.
export default function ContactPage() {
  return (
    <Container size="sm" className="fade-up">
      <title>Kontakt – LadyRanias</title>
      <Title order={2} fz={52} mb="md">
        Kontakt
      </Title>
      <Text size="lg">
        Lurer du på noe om kattene våre eller planlagte kull? Her kommer det snart et kontaktskjema.
      </Text>
    </Container>
  )
}
