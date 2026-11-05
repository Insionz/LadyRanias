import { Alert, Button, Container, Group, Stack, Tabs, Text, Title } from '@mantine/core'
import { login, useAuth } from '../../api/auth'
import { ErrorMessage, Loading } from '../../components/Status'
import AdminCats from './AdminCats'
import AdminLitters from './AdminLitters'

/**
 * Utlogging er et vanlig skjema som sender POST. Nettleseren følger da omdirigeringen til Entra og tilbake,
 * noe en fetch ikke kan.
 */
function LogoutButton() {
  return (
    <form method="post" action="/api/auth/logout">
      <Button type="submit" variant="subtle" size="xs">
        Logg ut
      </Button>
    </form>
  )
}

export default function AdminPage() {
  const auth = useAuth()

  return (
    <>
      <title>Admin – LadyRanias</title>

      {auth.kind === 'loading' && <Loading />}

      {auth.kind === 'error' && <ErrorMessage message={auth.message} />}

      {auth.kind === 'anonymous' && (
        <Container size="xs" ta="center" py="xl">
          <Title order={2} fz={48} mb="sm">
            Admin
          </Title>
          <Text mb="lg">Logg inn med e-postadressen din. Du får en engangskode på e-post.</Text>
          <Button size="md" onClick={() => login()}>
            Logg inn
          </Button>
        </Container>
      )}

      {auth.kind === 'forbidden' && (
        <Container size="sm" py="xl">
          <Alert color="yellow" title="Ingen tilgang">
            Du er logget inn som {auth.me.email ?? auth.me.name}, men har ikke tilgang til adminsidene.
          </Alert>
          <Group justify="center" mt="md">
            <LogoutButton />
          </Group>
        </Container>
      )}

      {auth.kind === 'admin' && (
        <Stack gap="lg">
          <Group justify="space-between" align="center">
            <Title order={2} fz={48}>
              Admin
            </Title>
            <Group gap="xs">
              <Text size="sm" c="dimmed">
                {auth.me.name ?? auth.me.email}
              </Text>
              <LogoutButton />
            </Group>
          </Group>

          <Tabs defaultValue="cats" keepMounted={false}>
            <Tabs.List>
              <Tabs.Tab value="cats">Katter</Tabs.Tab>
              <Tabs.Tab value="litters">Kull</Tabs.Tab>
            </Tabs.List>
            <Tabs.Panel value="cats" pt="md">
              <AdminCats />
            </Tabs.Panel>
            <Tabs.Panel value="litters" pt="md">
              <AdminLitters />
            </Tabs.Panel>
          </Tabs>
        </Stack>
      )}
    </>
  )
}
