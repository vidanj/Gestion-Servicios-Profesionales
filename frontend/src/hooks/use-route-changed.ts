import { usePathname } from 'next/navigation'
import { useEffect } from 'react'

export default function useRouteChanged(callback: () => void) {
  const pathname = usePathname()
  useEffect(() => {
    callback()
  }, [pathname])
}
