# Respuestas teóricas

Las preguntas se conservan en el contexto bancario del enunciado. Las respuestas se orientan al stack solicitado para la vacante: .NET, Python y Angular. Las tres preguntas específicas de Spring Boot se responden directamente y luego se relacionan con .NET.

## 1. Optimización en sistemas financieros

En una aplicación que procesa transacciones en tiempo real empezaría midiendo latencia por percentiles, rendimiento, saturación y tasa de errores. Optimizar sin estas mediciones puede desplazar el problema o debilitar la consistencia.

En la base de datos usaría transacciones breves, índices que correspondan a consultas reales, paginación y restricciones que protejan los invariantes. El dinero se representa con tipos decimales, nunca con punto flotante. Las operaciones que puedan repetirse reciben una clave de idempotencia para impedir cargos duplicados. La concurrencia se controla con bloqueo optimista cuando las colisiones son poco frecuentes y bloqueo pesimista o una actualización atómica condicionada cuando varias operaciones compiten por el mismo saldo.

La aplicación puede escalar horizontalmente si sus instancias no guardan estado de sesión local. Separaría el camino síncrono imprescindible de tareas posteriores como notificaciones o reportes, que pueden publicarse mediante un patrón outbox. Aplicaría backpressure, límites de concurrencia, timeouts y circuit breakers en integraciones. Un pool de conexiones bien dimensionado evita que el aumento de instancias sature la base de datos.

La caché sirve para datos de lectura que toleran una antigüedad definida, como catálogos o el estado consultado de una solicitud. No la usaría como fuente de verdad para saldos. Definiría TTL, invalidación después de una escritura confirmada y protección frente a avalanchas. Patrones como Repository no mejoran por sí solos el rendimiento; CQRS puede ser útil cuando lectura y escritura tienen perfiles muy distintos, pero requiere justificar la consistencia eventual y la operación adicional.

## 2. Seguridad en APIs financieras

La comunicación usa TLS y la API autentica identidades con tokens de vida corta. La autorización se comprueba en el servidor para cada recurso: tener un token válido no permite leer la cuenta de otra persona. Los administradores usan políticas explícitas y las operaciones sensibles dejan una auditoría inmutable. Contraseñas se almacenan con un algoritmo adaptativo, sal y parámetros actuales; secretos y claves se guardan fuera del repositorio y se rotan.

Para inyección SQL usaría Entity Framework Core o consultas parametrizadas y evitaría concatenar entradas. Validaría formato, rango y tamaño en el límite de la API, manteniendo también restricciones en la base de datos. XSS se reduce con el escape contextual de Angular, una política CSP y evitando insertar HTML no confiable. Si el JWT se envía en el encabezado `Authorization`, CSRF no se activa automáticamente como ocurre con cookies; si se usan cookies, deben ser `HttpOnly`, `Secure`, `SameSite` y acompañarse de un token antifalsificación. CORS se limita a orígenes conocidos y no sustituye la autorización.

También aplicaría límites de solicitudes, bloqueo progresivo ante intentos de acceso, encabezados de seguridad, registros sin datos sensibles, dependencias actualizadas y respuestas que no revelen trazas. Frente a IDOR/BOLA se filtra siempre por el identificador del usuario autenticado. Para SSRF se restringen destinos y protocolos salientes. En operaciones críticas se validan idempotencia, transiciones de estado y concurrencia en el servidor.

## 3. Transacciones en sistemas distribuidos

Una transferencia dentro de una sola base de datos se resuelve con una transacción ACID que registra débito, crédito y asiento contable. Cuando las cuentas pertenecen a servicios distintos no intentaría ocultar la distribución con una transacción local. Modelaría la transferencia como un proceso con identificador único y estados explícitos.

Una saga coordina los pasos: reservar fondos, acreditar al destino y confirmar el débito. Cada comando es idempotente y cada servicio guarda el cambio de negocio y el evento de salida en la misma transacción mediante transactional outbox. Un publicador entrega ese evento al broker y el consumidor registra los mensajes procesados en una inbox para tolerar entregas repetidas. Si el crédito falla de forma definitiva, se ejecuta la compensación que libera la reserva. Si el resultado es incierto, la transferencia queda en revisión o reintento; no se informa éxito antes de tener evidencia.

La consistencia es eventual entre servicios, pero cada servicio mantiene sus invariantes locales. Usaría reintentos con espera exponencial y aleatoriedad solo ante errores transitorios, timeouts acotados, circuit breaker y una cola de mensajes fallidos. La trazabilidad incluye correlation ID, estado de cada paso y conciliación periódica contra el libro mayor. Para dinero, el ledger de doble partida y los procesos de reconciliación son la última defensa contra diferencias silenciosas.

## 4. Pruebas unitarias y de integración

Las pruebas unitarias cubren reglas que no necesitan infraestructura: rangos de monto y plazo, transiciones permitidas, imposibilidad de decidir dos veces, cálculo de indicadores y denominadores. Deben ser rápidas y deterministas. En .NET usaría xUnit y dobles únicamente en límites externos; en Python, pytest para calidad y transformaciones de datos.

Las pruebas de integración levantan PostgreSQL real, aplican migraciones y ejercitan la API por HTTP. Verificaría login válido e inválido, aislamiento entre usuarios, permisos de administrador, validaciones, códigos de error, persistencia del historial, invalidación de caché y dos decisiones concurrentes sobre una misma solicitud. Testcontainers es apropiado para que la semántica sea la misma que en producción. Para contratos HTTP puede usarse `WebApplicationFactory` y un cliente real.

En Angular probaría formularios, guards, interceptor y estados de carga, vacío y error. Reservaría pruebas end to end para los caminos principales: usuario inicia sesión y solicita; administrador decide; usuario observa el estado. Las pruebas de seguridad incluyen recursos ajenos, roles incorrectos, tokens vencidos y entradas hostiles. La suite debe ejecutarse desde un estado limpio y aportar datos que expliquen el fallo.

## 5. Estado y autenticación en el front-end

Mantendría un servicio de autenticación como única fuente de la sesión y un interceptor para adjuntar el token. Los guards mejoran la navegación, pero la API vuelve a autorizar cada operación. En esta demostración el token se guarda en `sessionStorage`, lo que reduce su permanencia pero sigue expuesto ante XSS. En un sistema financiero preferiría un backend for frontend con cookies `HttpOnly`, `Secure` y protección CSRF, de modo que JavaScript no acceda al token.

El estado de saldos o préstamos se obtiene de la API y se trata como una instantánea con momento de actualización. Tras una escritura se invalida o vuelve a consultar el recurso afectado. Un servicio con signals o RxJS es suficiente para un alcance pequeño; NgRx tiene sentido cuando muchos módulos comparten transiciones complejas, auditoría de acciones o efectos coordinados. Evitaría duplicar datos derivados y cancelaría consultas obsoletas.

La interfaz no registra tokens ni datos sensibles, muestra carga, vacío y error, y cierra la sesión cuando recibe `401`. La CSP, el escape de Angular, dependencias revisadas y la prohibición de HTML dinámico reducen XSS. Los datos críticos nunca se consideran seguros por estar ocultos en la interfaz.

## 6. Spring Boot

### a. ¿Qué ocurre con `@SpringBootApplication` y cómo afecta el arranque?

`@SpringBootApplication` combina tres anotaciones. `@Configuration` declara una fuente de definiciones de beans; `@EnableAutoConfiguration` activa configuraciones condicionales según el classpath, los beans existentes y las propiedades; `@ComponentScan` busca componentes desde el paquete de la clase principal hacia abajo. `SpringApplication.run` prepara el entorno y perfiles, crea el `ApplicationContext`, registra beans, aplica postprocesadores, inicia el servidor embebido si es una aplicación web y ejecuta runners.

La ubicación de la clase afecta el alcance del escaneo, por lo que normalmente se coloca en un paquete raíz. La auto-configuración aplica valores predeterminados y retrocede cuando el desarrollador declara un bean propio. En ASP.NET Core, el papel equivalente se reparte entre `WebApplication.CreateBuilder`, el registro en `IServiceCollection`, la configuración y el pipeline de middleware; no existe una única anotación equivalente.

### b. ¿Cómo funciona el ciclo de vida de un bean y cómo intervenir?

El contenedor crea la instancia, inyecta dependencias y ejecuta callbacks aware. Después actúan los `BeanPostProcessor` antes de la inicialización; se invocan `@PostConstruct`, `InitializingBean.afterPropertiesSet` y el método `init`; finalmente actúan los postprocesadores posteriores, donde también pueden crearse proxies. Al cerrar el contexto se ejecutan `@PreDestroy`, `DisposableBean.destroy` y el método de destrucción configurado. El ciclo exacto depende del scope: singleton, prototype, request o session.

Se puede intervenir con `BeanPostProcessor`, callbacks, eventos del contexto, métodos `@Bean(initMethod, destroyMethod)` o scopes personalizados. Estas extensiones deben reservarse para necesidades transversales reales. En .NET, el contenedor administra scopes `Singleton`, `Scoped` y `Transient`; la inicialización puede vivir en hosted services y la liberación en `IDisposable`/`IAsyncDisposable`.

### c. ¿Cómo personalizar la auto-configuración sin romper “convención sobre configuración”?

Primero usaría propiedades tipadas con `@ConfigurationProperties` y perfiles para cambiar valores. Si necesito sustituir comportamiento, declararía un bean del tipo esperado porque muchas auto-configuraciones usan `@ConditionalOnMissingBean`. Para desactivar una parte concreta usaría `exclude` o `spring.autoconfigure.exclude`, con una justificación explícita. Una librería interna puede ofrecer su propia auto-configuración mediante condiciones como `@ConditionalOnClass`, `@ConditionalOnProperty` y `@ConditionalOnMissingBean`.

Evitaría redefinir internals, forzar el orden de arranque o copiar configuraciones completas. La personalización debe conservar valores predeterminados útiles, ser observable y activarse con propiedades claras. En .NET, el paralelo es configurar `Options`, registrar una implementación propia en el contenedor y encapsular convenciones en métodos de extensión sobre `IServiceCollection`.

## Decisiones aplicadas en la parte práctica

La API usa arquitectura por capas, PostgreSQL y Entity Framework Core. El dominio protege las transiciones; la base de datos añade restricciones y concurrencia optimista. Solo el rol `Admin` puede decidir. Las lecturas individuales tienen caché breve e invalidación tras escrituras. El endpoint analítico exporta identificadores técnicos, montos, plazos, estados y fechas, sin correos ni credenciales.

Python valida completitud, unicidad, dominios y coherencia temporal antes de calcular indicadores. La tasa de aprobación excluye pendientes de su denominador y el informe declara que los datos sintéticos no permiten inferir riesgo crediticio.
