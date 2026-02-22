import type { ElementType } from 'react'

const siteConfig = {
  logo: undefined as ElementType | undefined,
  seo: {
    title: 'Sistema de Servicios Profesionales',
    description:
      'Plataforma para publicar, gestionar y contratar servicios profesionales de forma segura.',
  },
  header: {
    links: [
      { label: 'Inicio', href: '/' },
      { label: 'Acerca de', href: '/about' },
<<<<<<< HEAD
=======
      { label: 'Solicitudes', href: '/solicitudes' },
>>>>>>> 38fc090 (interfaz solicitudes - issue 22)
      { label: 'Servicios', id: 'features', href: '/#features' },
      { label: 'Precios', id: 'pricing', href: '/#pricing' },
      { label: 'Ingresar', href: '/login' },
    ],
  },
  footer: {
    copyright: '© 2025 Sistema de Servicios Profesionales. Todos los derechos reservados.',
    links: [
      { label: 'Aviso legal', href: '/legal' },
      { label: 'Privacidad', href: '/privacy' },
    ],
  },
  signup: {
    features: [
      {
        title: "Publicación de servicios",
        description:
          "Publica tus servicios con precios y descripciones detalladas.",
        icon: "FiBriefcase",
      },
      {
        title: "Gestión de pedidos",
        description:
          "Administra solicitudes y entregas desde un panel centralizado.",
        icon: "FiClipboard",
      },
      {
        title: "Pagos seguros",
        description: "Procesa pagos de forma segura y transparente.",
        icon: "FiShield",
      },
      {
        title: "Valoraciones y reseñas",
        description:
          "Construye tu reputación con el sistema de valoraciones integrado.",
        icon: "FiStar",
      },
    ],
  },
};

export default siteConfig;
