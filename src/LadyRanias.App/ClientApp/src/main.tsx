import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import { createTheme, MantineProvider } from '@mantine/core'

// Rekkefølgen betyr noe: Mantine først, så fonten, og våre egne stiler til slutt så de kan overstyre.
import '@mantine/core/styles.css'
import '@fontsource/pinyon-script'
import './index.css'

import App from './App.tsx'

const theme = createTheme({
  primaryColor: 'grape',
  headings: {
    // Pinyon Script har bare én vekt. Med 400 slipper vi at nettleseren lager en «falsk» fet variant.
    fontFamily: '"Pinyon Script", cursive',
    fontWeight: '400',
  },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <MantineProvider theme={theme}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </MantineProvider>
  </StrictMode>,
)
