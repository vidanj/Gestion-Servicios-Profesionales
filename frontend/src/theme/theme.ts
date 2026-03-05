import { createSystem, defaultConfig } from '@chakra-ui/react'

const customConfig = {
  ...defaultConfig,
  theme: {
    ...defaultConfig.theme,
    tokens: {
      ...defaultConfig.theme?.tokens,
      colors: {
        ...defaultConfig.theme?.tokens?.colors,
        primary: {
          50: { value: '#f3e8ff' },
          100: { value: '#e9d5ff' },
          200: { value: '#d8b4fe' },
          300: { value: '#c084fc' },
          400: { value: '#a855f7' },
          500: { value: '#9333ea' },
          600: { value: '#7e22ce' },
          700: { value: '#6b21a8' },
          800: { value: '#581c87' },
          900: { value: '#3f0f5c' },
        },
        secondary: {
          50: { value: '#f7e7ff' },
          100: { value: '#f0d5ff' },
          200: { value: '#e3acff' },
          300: { value: '#d583ff' },
          400: { value: '#c85aff' },
          500: { value: '#b83aff' },
          600: { value: '#a619d4' },
          700: { value: '#8400aa' },
          800: { value: '#620080' },
          900: { value: '#480056' },
        },
      },
    },
  },
}

export const system = createSystem(customConfig)
