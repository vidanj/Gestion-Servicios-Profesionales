import {
  Box,
  CloseButton,
  Flex,
  HStack,
  IconButton,
  IconButtonProps,
  Stack,
  useBreakpointValue,
  useUpdateEffect,
} from '@chakra-ui/react'
import NextLink from 'next/link'
import useRouteChanged from '@/hooks/use-route-changed'
import { usePathname } from 'next/navigation'
import { AiOutlineMenu } from 'react-icons/ai'

import * as React from 'react'

import { Logo } from '#components/layout/logo'
import siteConfig from '#data/config'

interface NavLinkProps {
  label: string
  href?: string
  isActive?: boolean
  children?: React.ReactNode
}

function NavLink({ href, children, isActive, ...rest }: NavLinkProps) {
  const pathname = usePathname()

  const [, group] = href?.split('/') || []
  isActive = isActive ?? pathname?.includes(group)

  return (
    <NextLink
      href={href ?? '#'}
      style={{ display: 'flex', flex: 1 }}
    >
      <Box
        as="span"
        display="inline-flex"
        flex="1"
        minH="40px"
        px="8"
        py="3"
        transition="0.2s all"
        fontWeight={isActive ? 'semibold' : 'medium'}
        borderColor={isActive ? 'purple.400' : undefined}
        borderBottomWidth="1px"
        color={isActive ? 'white' : undefined}
        bg={isActive ? 'purple.500' : undefined}
        _hover={{ bg: isActive ? 'purple.500' : 'gray.100' }}
        _dark={{ _hover: { bg: isActive ? 'purple.500' : 'whiteAlpha.100' } }}
      >
        {children}
      </Box>
    </NextLink>
  )
}

interface MobileNavContentProps {
  isOpen?: boolean
  onClose?: () => void
}

export function MobileNavContent(props: MobileNavContentProps) {
  const { isOpen, onClose = () => {} } = props
  const closeBtnRef = React.useRef<HTMLButtonElement>(null)
  const pathname = usePathname()

  useRouteChanged(onClose)

  const showOnBreakpoint = useBreakpointValue({ base: true, lg: false })

  React.useEffect(() => {
    if (showOnBreakpoint == false) {
      onClose()
    }
  }, [showOnBreakpoint, onClose])

  useUpdateEffect(() => {
    if (isOpen) {
      requestAnimationFrame(() => {
        closeBtnRef.current?.focus()
      })
    }
  }, [isOpen])

  return (
    <>
      {isOpen && (
          <Flex
            direction="column"
            w="100%"
            bg="whiteAlpha.900"
            _dark={{ bg: 'blackAlpha.900' }}
            h="100vh"
            overflow="auto"
            pos="absolute"
            inset="0"
            zIndex="modal"
            pb="8"
            backdropFilter="blur(5px)"
          >
            <Box>
              <Flex justify="space-between" px="8" pt="4" pb="4">
                <Logo />
                <HStack gap="5">
                  <CloseButton ref={closeBtnRef} onClick={onClose} />
                </HStack>
              </Flex>
              <Stack alignItems="stretch" gap="0">
                {siteConfig.header.links.map(
                  ({ href, id, label, ...props }: { href?: string; id?: string; label: string; [key: string]: unknown }, i: number) => {
                    return (
                      <NavLink
                        href={href || `/#${id}`}
                        key={i}
                        label={label}
                        {...(props as any)}
                      >
                        {label}
                      </NavLink>
                    )
                  },
                )}
              </Stack>
            </Box>
          </Flex>
      )}
    </>
  )
}

export const MobileNavButton = React.forwardRef(
  (props: IconButtonProps, ref: React.Ref<any>) => {
    return (
      <IconButton
        ref={ref}
        display={{ base: 'flex', md: 'none' }}
        fontSize="20px"
        variant="ghost"
        {...props}
        aria-label="Open menu"
      >
        <AiOutlineMenu />
      </IconButton>
    )
  },
)

MobileNavButton.displayName = 'MobileNavButton'
