import { useEffect, useRef } from 'react'
import { ActionIcon, Box, Group, Image, Modal, Text } from '@mantine/core'

export type LightboxImage = {
  /** Det store bildet */
  src: string
  /** «Uke 5», hvis bildet hører til en uke */
  caption?: string
}

/**
 * Bildene i full skjerm. Man blar med pilene, piltastene eller ved å sveipe, og lukker med Esc eller krysset.
 * Da slipper man å rulle opp til det store bildet for hvert bilde man vil se.
 */
export default function Lightbox({
  images,
  index,
  onChange,
  onClose,
  alt,
}: {
  images: LightboxImage[]
  /** Bildet som vises, eller null når visningen er lukket */
  index: number | null
  onChange: (index: number) => void
  onClose: () => void
  alt: string
}) {
  const touchStartX = useRef<number | null>(null)
  const count = images.length
  const open = index !== null && count > 0
  const current = open ? images[Math.min(index, count - 1)] : null

  // Blar rundt: etter det siste kommer det første.
  const go = (step: number) => {
    if (index === null) return
    onChange((index + step + count) % count)
  }

  useEffect(() => {
    if (index === null) return
    function onKey(e: KeyboardEvent) {
      if (e.key === 'ArrowRight') onChange((index! + 1) % count)
      if (e.key === 'ArrowLeft') onChange((index! - 1 + count) % count)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [index, count, onChange])

  // Henter naboene på forhånd, så neste bilde er klart når man blar.
  useEffect(() => {
    if (index === null || count < 2) return
    for (const step of [1, -1]) {
      const img = new window.Image()
      img.src = images[(index + step + count) % count].src
    }
  }, [index, count, images])

  return (
    <Modal
      opened={open}
      onClose={onClose}
      fullScreen
      padding={0}
      withCloseButton={false}
      transitionProps={{ duration: 150 }}
      styles={{ content: { background: 'rgba(0, 0, 0, 0.92)' }, body: { height: '100%' } }}
    >
      {current && (
        <Box
          h="100dvh"
          pos="relative"
          style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', userSelect: 'none' }}
          onTouchStart={e => (touchStartX.current = e.touches[0].clientX)}
          onTouchEnd={e => {
            const start = touchStartX.current
            touchStartX.current = null
            if (start === null) return
            const dx = e.changedTouches[0].clientX - start
            if (Math.abs(dx) > 50) go(dx < 0 ? 1 : -1)
          }}
          // Et trykk utenfor bildet lukker, som folk er vant til.
          onClick={e => e.target === e.currentTarget && onClose()}
        >
          <Image src={current.src} alt={alt} fit="contain" mah="calc(100dvh - 96px)" maw="100vw" w="auto" />

          <Group pos="absolute" top={0} left={0} right={0} p="md" justify="space-between" style={{ pointerEvents: 'none' }}>
            <Text c="white" size="sm" style={{ pointerEvents: 'auto' }}>
              {count > 1 && `${index! + 1} / ${count}`}
              {current.caption && ` · ${current.caption}`}
            </Text>
            <ActionIcon variant="subtle" color="gray.0" size="xl" aria-label="Lukk" onClick={onClose} style={{ pointerEvents: 'auto' }}>
              <span style={{ fontSize: 28, lineHeight: 1 }}>×</span>
            </ActionIcon>
          </Group>

          {count > 1 && (
            <>
              <ActionIcon
                variant="subtle"
                color="gray.0"
                size={48}
                radius="xl"
                aria-label="Forrige bilde"
                onClick={() => go(-1)}
                pos="absolute"
                left={8}
                top="50%"
                style={{ transform: 'translateY(-50%)' }}
              >
                <span style={{ fontSize: 32, lineHeight: 1 }}>‹</span>
              </ActionIcon>
              <ActionIcon
                variant="subtle"
                color="gray.0"
                size={48}
                radius="xl"
                aria-label="Neste bilde"
                onClick={() => go(1)}
                pos="absolute"
                right={8}
                top="50%"
                style={{ transform: 'translateY(-50%)' }}
              >
                <span style={{ fontSize: 32, lineHeight: 1 }}>›</span>
              </ActionIcon>
            </>
          )}
        </Box>
      )}
    </Modal>
  )
}
