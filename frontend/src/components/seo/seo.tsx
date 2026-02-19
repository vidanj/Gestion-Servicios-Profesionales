'use client'

import React from 'react'
import Head from 'next/head'

import siteConfig from '#data/config'

export interface SEOProps {
  title?: string
  description?: string
  openGraph?: Record<string, any>
  twitter?: Record<string, any>
  [key: string]: any
}

export const SEO = ({ title, description, openGraph, twitter }: SEOProps) => {
  const metaTitle = title ?? siteConfig.seo?.title
  const metaDescription = description ?? siteConfig.seo?.description

  return (
    <Head>
      {metaTitle ? <title>{metaTitle}</title> : null}
      {metaDescription ? <meta name="description" content={metaDescription} /> : null}

      {/* Open Graph */}
      <meta property="og:title" content={metaTitle ?? ''} />
      <meta property="og:description" content={metaDescription ?? ''} />
      {openGraph?.images?.[0] ? (
        <meta property="og:image" content={openGraph.images[0].url ?? openGraph.images[0]} />
      ) : null}

      {/* Twitter */}
      <meta name="twitter:card" content="summary_large_image" />
      {twitter?.handle ? <meta name="twitter:creator" content={twitter.handle} /> : null}
      {twitter?.site ? <meta name="twitter:site" content={twitter.site} /> : null}
    </Head>
  )
}
