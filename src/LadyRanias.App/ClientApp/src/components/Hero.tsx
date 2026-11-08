import { useEffect, useState, type ReactNode } from 'react'
import { useReducedMotion } from '@mantine/hooks'
import { catImageUrl, imageUrl, SITE_IMAGES_ID, useApi } from '../api/api'
import type { Cat, CatImage, SiteSettings } from '../types/types'
import classes from '../pages/HomePage.module.css'

const slideMs = 7000

/**
 * Det store bildet øverst på forsiden. Mamma velger i admin:
 *   Newest:    det nyeste bildet av den nyeste katten (slik det var før)
 *   Fixed:     ett bilde hun har valgt
 *   Random:    et tilfeldig forsidebilde hver gang siden åpnes
 *   Slideshow: forsidebildene glir over i hverandre
 * Har hun ikke lastet opp noen forsidebilder, brukes alltid Newest.
 */
export default function Hero({ settings, children }: { settings: SiteSettings | null; children: ReactNode }) {
  const siteImages = useApi<CatImage[]>(`/api/public/site/${SITE_IMAGES_ID}/images`)
  const own = siteImages.data ?? []
  const useOwn = settings != null && settings.heroMode !== 'Newest' && own.length > 0

  // Reserven: det nyeste bildet av den nyeste katten. Hentes bare når den trengs.
  const cats = useApi<Cat[]>(settings && !useOwn && siteImages.data ? '/api/public/cats' : null)
  const newestCat = cats.data?.[0]
  const catImages = useApi<CatImage[]>(newestCat ? `/api/public/cats/${newestCat.id}/images` : null)

  // Tilfeldig startbilde, valgt én gang når siden åpnes.
  const [randomStart] = useState(() => Math.random())
  const [index, setIndex] = useState(0)
  const reduceMotion = useReducedMotion()
  const slideshow = useOwn && settings.heroMode === 'Slideshow' && own.length > 1 && !reduceMotion

  useEffect(() => {
    if (!slideshow) return
    const timer = window.setInterval(() => setIndex(i => i + 1), slideMs)
    return () => window.clearInterval(timer)
  }, [slideshow])

  let sources: string[] = []
  let active = 0
  if (useOwn) {
    const urls = own.map(image => imageUrl('site', SITE_IMAGES_ID, image.imageId, 'large'))
    if (settings.heroMode === 'Fixed') {
      // Er det valgte bildet slettet, brukes det første.
      const chosen = own.findIndex(image => image.imageId === settings.heroImageId)
      sources = [urls[Math.max(chosen, 0)]]
    } else if (settings.heroMode === 'Random') {
      sources = [urls[Math.floor(randomStart * urls.length)]]
    } else {
      sources = urls
      active = (Math.floor(randomStart * urls.length) + index) % urls.length
    }
  } else {
    const first = catImages.data?.[0]
    if (newestCat && first) sources = [catImageUrl(newestCat.id, first.imageId, 'large')]
  }

  return (
    <div className={`${classes.hero} fade-up`} style={{ borderRadius: 'var(--mantine-radius-md)' }}>
      {/* Alle bildene ligger oppå hverandre. Det aktive er synlig, og overgangen er en myk toning. */}
      {sources.map((src, i) => (
        <div
          key={src}
          className={classes.slide}
          style={{ backgroundImage: `url("${src}")`, opacity: i === active ? 1 : 0 }}
          aria-hidden
        />
      ))}
      <div className={classes.shade} />
      <div className={classes.content}>{children}</div>
    </div>
  )
}
