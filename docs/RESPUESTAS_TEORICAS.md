# Respuestas teóricas

Las respuestas están orientadas a .NET, Python y Angular. Las preguntas específicas de Spring Boot se responden directamente.

## 1. Optimización en sistemas financieros

Primero mediría latencia, errores y consumo de recursos. Luego optimizaría consultas e índices, usaría paginación y mantendría las transacciones cortas. Para dinero usaría `decimal` e idempotencia para evitar operaciones duplicadas.

La aplicación debería ser stateless para escalar horizontalmente. Usaría caché solo en lecturas que toleren datos temporales y procesos asíncronos para tareas no críticas, como reportes o notificaciones.

## 2. Seguridad en APIs financieras

Usaría HTTPS, JWT de corta duración y autorización por roles y por propietario del recurso. Las contraseñas deben almacenarse con hash seguro y los secretos fuera del repositorio.

Entity Framework evita inyección SQL mediante consultas parametrizadas. También aplicaría validación de entradas, CORS restringido, rate limiting, auditoría y respuestas sin información interna. Angular ayuda contra XSS, pero no reemplaza las validaciones del backend.

## 3. Transacciones en sistemas distribuidos

Si todo ocurre en una base de datos, usaría una transacción ACID. Si intervienen varios servicios, usaría una saga con pasos idempotentes y acciones de compensación.

El patrón outbox permite guardar el cambio y el evento en la misma transacción. También agregaría reintentos controlados, correlation ID y conciliación para detectar inconsistencias.

## 4. Pruebas unitarias y de integración

Las pruebas unitarias cubren reglas como montos, plazos y transiciones de estado. En .NET usaría xUnit y en Python, pytest.

Las pruebas de integración verifican endpoints, permisos y persistencia. Probaría login, aislamiento entre usuarios, decisiones del administrador y manejo de errores. Los flujos principales del frontend pueden cubrirse con pocas pruebas end to end.

## 5. Estado y autenticación en el front-end

Centralizaría la sesión en un servicio y usaría un interceptor para enviar el token. Los guards controlan la navegación, pero la autorización real siempre se valida en la API.

Para una aplicación pequeña, signals o RxJS son suficientes. Tras crear o modificar un préstamo, volvería a consultar los datos. En producción preferiría cookies `HttpOnly` frente a almacenar el token en el navegador.

## 6. Spring Boot

### a. ¿Qué ocurre con `@SpringBootApplication` y cómo afecta el arranque?

Combina `@Configuration`, `@EnableAutoConfiguration` y `@ComponentScan`. Configura beans, activa configuración automática y busca componentes desde el paquete principal. En .NET, una función similar la cumplen `WebApplication.CreateBuilder` y el contenedor de dependencias.

### b. ¿Cómo funciona el ciclo de vida de un bean y cómo intervenir?

Spring crea el bean, inyecta sus dependencias, ejecuta la inicialización y finalmente su destrucción al cerrar el contexto. Se puede intervenir con `@PostConstruct`, `@PreDestroy` o `BeanPostProcessor`.

### c. ¿Cómo personalizar la auto-configuración sin romper “convención sobre configuración”?

Usaría propiedades y perfiles. Si necesito cambiar un comportamiento, declararía mi propio bean o excluiría únicamente la auto-configuración necesaria. La idea es conservar los valores predeterminados y personalizar solo lo indispensable.

## Decisiones aplicadas en la parte práctica

La solución usa .NET, Angular, PostgreSQL y Python. El backend controla roles y estados de los préstamos; Python valida los datos y genera indicadores sin exponer información sensible.
