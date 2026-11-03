import { useEffect, useState } from 'react'
import './App.css'
import type { Cat } from './types/types'

function App() {
  const [cats, setCats] = useState<Cat[]>([])
  const [error, setError] = useState();

  useEffect(() => {
    fetch("/api/public/cats")
    .then(response => {
      if(!response.ok) {
        throw new Error(`API svarte ${response.status}`)
      }
      return response?.json()
    })
    .then(data => setCats(data))
    .catch(error => setError(error.message))
  },[])

  return (
    <>
    <ul>
      {cats.map(cat => (
        <li key={cat.id}>{cat.name}</li>
      ))}
    </ul>
    <div>{error}</div>
    </>
  )
}

export default App
