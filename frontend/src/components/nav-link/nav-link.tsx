import { forwardRef } from 'react'
import { Button, ButtonProps } from '@chakra-ui/react'

import Link from 'next/link'

export interface NavLinkProps extends ButtonProps {
  isActive?: boolean
  href?: string
  id?: string
}

export const NavLink = forwardRef<HTMLButtonElement, NavLinkProps>((props, ref) => {
  const { href, type, isActive, children, ...rest } = props

  return (
    <Button
      asChild
      ref={ref}
      variant="ghost"
      lineHeight="2rem"
      fontWeight="medium"
      aria-current={isActive ? 'page' : undefined}
      {...rest}
    >
      <Link href={href ?? '#'}>{children}</Link>
    </Button>
  )
})

NavLink.displayName = 'NavLink'
