import { Box, Flex, Heading, VisuallyHidden } from '@chakra-ui/react'
import NextLink from 'next/link'

import * as React from 'react'

import siteConfig from '#data/config'

export interface LogoProps {
  href?: string
  onClick?: (e: React.MouseEvent<HTMLAnchorElement>) => void
}

export const Logo = ({ href = '/', onClick }: LogoProps) => {
  let logo
  if (siteConfig.logo) {
    logo = <Box as={siteConfig.logo} height="32px" mt="-4px" />
  } else {
    logo = (
      <Heading as="h1" size="md">
        {siteConfig.seo?.title}
      </Heading>
    )
  }

  return (
    <Flex h="8" flexShrink="0" alignItems="flex-start">
      <NextLink
        href={href}
        style={{ display: 'flex', padding: '4px', borderRadius: '4px' }}
        onClick={onClick}
      >
        {logo}
        <VisuallyHidden>{siteConfig.seo?.title}</VisuallyHidden>
      </NextLink>
    </Flex>
  )
}
