import { Button, Container, Text, Title } from '@mantine/core'
import { Link } from 'react-router'

export default function NotFoundPage() {
  return (
    <Container size="sm" ta="center" py="xl">
      <title>Fant ikke siden – LadyRanias</title>
      <Title order={2} fz={56} mb="sm">
        Her var det tomt
      </Title>
      <Text mb="lg">Siden finnes ikke, eller katten er ikke lenger med på siden.</Text>
      <Button component={Link} to="/">
        Til forsiden
      </Button>
    </Container>
  )
}
