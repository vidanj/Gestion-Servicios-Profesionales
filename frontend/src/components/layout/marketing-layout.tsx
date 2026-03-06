'use client'

import { Box } from '@chakra-ui/react'

import { ReactNode } from 'react'

import {
  AnnouncementBanner,
  AnnouncementBannerProps,
} from '../announcement-banner'
import { Footer, FooterProps } from './footer'
import { Header, HeaderProps } from './header'

interface LayoutProps {
  children: ReactNode
  announcementProps?: AnnouncementBannerProps
  headerProps?: HeaderProps
  footerProps?: FooterProps
}

export const MarketingLayout: React.FC<LayoutProps> = (props) => {
  const { children, announcementProps, headerProps, footerProps } = props
  return (
    <Box>
      <a href="#skip-content" style={{ position: "absolute", top: "-40px", left: 0, zIndex: 100 }}>Skip to content</a>
      {announcementProps ? <AnnouncementBanner {...announcementProps} /> : null}
      <Header {...headerProps} />
      <Box as="main">
        <div id="skip-content" />
        {children}
      </Box>
      <Footer {...footerProps} />
    </Box>
  )
}
