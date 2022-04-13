import { useState } from 'react'
import logo from './logo.svg'
import './App.css'
import Counter from './Counter'

function App() {
  const [count, setCount] = useState(2)

  return (
    <div className="App">
      <Counter />
    </div>
  )
}

export default App
