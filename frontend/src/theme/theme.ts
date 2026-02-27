import { createSystem, defaultConfig } from '@chakra-ui/react'

const customConfig = {
  ...defaultConfig,
  theme: {
    tokens: {
      ...defaultConfig.theme?.tokens,
      colors: {
        ...defaultConfig.theme?.tokens?.colors,
        primary: {
          50: '#f3e8ff',
          100: '#e9d5ff',
          200: '#d8b4fe',
          300: '#c084fc',
          400: '#a855f7',
          500: '#9333ea',
          600: '#7e22ce',
          700: '#6b21a8',
          800: '#581c87',
          900: '#3f0f5c',
        },
        secondary: {
          50: '#f7e7ff',
          100: '#f0d5ff',
          200: '#e3acff',
          300: '#d583ff',
          400: '#c85aff',
          500: '#b83aff',
          600: '#a619d4',
          700: '#8400aa',
          800: '#620080',
          900: '#480056',
        },
      },
    },
  },
}

export const system = createSystem(customConfig)
