using Xunit;

// Las pruebas manipulan variables de entorno del proceso (estado global).
// BackupServiceTests pone env vars a null para probar validaciones, mientras
// que CustomWebApplicationFactory las establece en su constructor.
// Si ambas clases corren en paralelo, hay una condición de carrera que provoca
// fallos intermitentes en CI. Deshabilitar el paralelismo entre colecciones
// elimina la carrera sin modificar la lógica de producción ni de pruebas.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
