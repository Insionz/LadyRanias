import { SimpleGrid } from '@mantine/core'
import type { Cat } from '../types/types'
import CatCard from './CatCard'

export default function CatGrid({ cats }: { cats: Cat[] }) {
  return (
    <SimpleGrid cols={{ base: 1, sm: 2, md: 3 }} spacing="lg">
      {cats.map(cat => (
        <CatCard key={cat.id} cat={cat} />
      ))}
    </SimpleGrid>
  )
}
