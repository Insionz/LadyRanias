import { useEffect, useState } from 'react'
import './App.css'
import type { Cat } from './types/types'

function App() {
  const [cats, setCats] = useState<Cat[]>([])

  useEffect(() => {
    fetch("/api/public/cats")
    .then(response => response.json())
    .then(data => setCats(data))
  },[])

  return (
    <ul>
      {cats.map(cat => (
        <li key={cat.id}>{cat.name}</li>
      ))}
    </ul>
  )
}

export default App
