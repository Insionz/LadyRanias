import { useEffect } from 'react'
import { AppShell, Burger, Group, Title, UnstyledButton } from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { Link, NavLink, Route, Routes, useLocation } from 'react-router'
import classes from './App.module.css'
import HomePage from './pages/HomePage'
import CatsPage from './pages/CatsPage'
import CatPage from './pages/CatPage'
import MemorialPage from './pages/MemorialPage'
import NewsPage from './pages/NewsPage'
import PostPage from './pages/PostPage'
import LittersPage from './pages/LittersPage'
import ContactPage from './pages/ContactPage'
import NotFoundPage from './pages/NotFoundPage'
import AdminPage from './pages/admin/AdminPage'

const links = [
  { to: '/', label: 'Hjem' },
  { to: '/katter', label: 'Katter' },
  { to: '/kull', label: 'Kull' },
  { to: '/nyheter', label: 'Nyheter' },
  { to: '/minne', label: 'Til minne' },
  { to: '/kontakt', label: 'Kontakt' },
]

function NavLinks({ onClick }: { onClick?: () => void }) {
  return links.map(link => (
    <UnstyledButton
      key={link.to}
      className={classes.control}
      component={NavLink}
      to={link.to}
      // Uten «end» ville «Hjem» (/) også vært markert på alle andre sider.
      end={link.to === '/'}
      onClick={onClick}
    >
      {link.label}
    </UnstyledButton>
  ))
}

export default function App() {
  const [opened, { toggle, close }] = useDisclosure()
  const { pathname } = useLocation()

  // En SPA beholder rulleposisjonen når man bytter side. Vi starter øverst, som på en vanlig nettside.
  useEffect(() => {
    window.scrollTo(0, 0)
  }, [pathname])

  return (
    <AppShell
      header={{ height: 64 }}
      navbar={{ width: 300, breakpoint: 'sm', collapsed: { desktop: true, mobile: !opened } }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md" wrap="nowrap">
          <Burger opened={opened} onClick={toggle} hiddenFrom="sm" size="sm" aria-label="Meny" />
          <Group justify="space-between" wrap="nowrap" style={{ flex: 1, minWidth: 0 }}>
            {/* Title kan ikke være en lenke selv, så vi legger lenken rundt den. */}
            <Link to="/" className={classes.logo} onClick={close}>
              <Title order={1} fz={{ base: 28, sm: 36 }} className={classes.logoTitle}>
                (N) Lady Rania’s
              </Title>
            </Link>
            <Group gap="md" visibleFrom="sm" wrap="nowrap">
              <NavLinks />
            </Group>
          </Group>
        </Group>
      </AppShell.Header>

      {/* Mobilmenyen lukkes når man trykker på en lenke. */}
      <AppShell.Navbar py="md" px={20}>
        <NavLinks onClick={close} />
      </AppShell.Navbar>

      <AppShell.Main>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/katter" element={<CatsPage />} />
          <Route path="/katter/:id" element={<CatPage />} />
          <Route path="/minne" element={<MemorialPage />} />
          <Route path="/nyheter" element={<NewsPage />} />
          <Route path="/nyheter/:id" element={<PostPage />} />
          <Route path="/kull" element={<LittersPage />} />
          <Route path="/kontakt" element={<ContactPage />} />
          {/* Står ikke i menyen. Tilgangen styres av API-et, ikke av om lenken vises. */}
          <Route path="/admin" element={<AdminPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </AppShell.Main>
    </AppShell>
  )
}
