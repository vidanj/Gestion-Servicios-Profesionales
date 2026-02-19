'use client'
import { IconButton } from '@chakra-ui/react'
import { FiMoon, FiSun } from 'react-icons/fi'
import { useEffect, useState } from 'react'

const ThemeToggle = () => {
  const [isDark, setIsDark] = useState(false)

  useEffect(() => {
    const theme = document.documentElement.getAttribute('data-theme')
    setIsDark(theme === 'dark')
  }, [])

  const toggle = () => {
    const newTheme = isDark ? 'light' : 'dark'
    document.documentElement.setAttribute('data-theme', newTheme)
    setIsDark(!isDark)
  }

  return (
    <IconButton
      variant="ghost"
      aria-label="theme toggle"
      borderRadius="md"
      onClick={toggle}
    >
      {isDark ? <FiSun size="14" /> : <FiMoon size="14" />}
    </IconButton>
  )
}

export default ThemeToggle
