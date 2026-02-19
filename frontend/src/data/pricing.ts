const pricing = {
  title: "Planes y precios",
  description: "Elige el plan que mejor se adapte a tus necesidades.",
  plans: [
    {
      id: "basic",
      title: "Básico",
      description: "Ideal para empezar.",
      price: "Gratis",
      features: [
        { title: "1 servicio activo" },
        { title: "Hasta 3 pedidos por mes" },
        { title: "Soporte por email" },
      ],
      action: { href: "/signup", label: "Comenzar gratis" },
    },
    {
      id: "pro",
      title: "Profesional",
      description: "Para freelancers activos.",
      price: "$9.99/mes",
      isRecommended: true,
      features: [
        { title: "Servicios ilimitados" },
        { title: "Pedidos ilimitados" },
        { title: "Soporte prioritario" },
        { title: "Analíticas básicas" },
      ],
      action: { href: "/signup", label: "Empezar ahora" },
    },
    {
      id: "enterprise",
      title: "Empresa",
      description: "Para equipos y agencias.",
      price: "$29.99/mes",
      features: [
        { title: "Todo lo del plan Pro" },
        { title: "Gestión de equipos" },
        { title: "API personalizada" },
        { title: "Soporte 24/7" },
      ],
      action: { href: "/contact", label: "Contactar ventas" },
    },
  ],
};

export default pricing;
