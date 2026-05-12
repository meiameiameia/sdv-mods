# Guía del Jugador - PowerGrid

Esta es la guía práctica rápida para usar PowerGrid sin tener que aprender todo por prueba y error.

PowerGrid añade generadores, baterías, cables, conductos, objetos de combustible, máquinas artesanales energizadas y una pequeña tier industrial de procesamiento. Las máquinas energizadas no duplican objetos ni ignoran insumos. Simplemente funcionan más rápido cuando la red puede suministrar suficiente EU.

## Primer Montaje

Empieza con algo pequeño:

1. Coloca un `Generador de Vapor`.
2. Cárgalo con `Carbón`, `Madera` o `Madera Noble`.
3. Coloca `Cable de Cobre` junto al generador.
4. Coloca una máquina energizada junto al cable o junto a otra máquina energizada.
5. Añade una `Batería de Energía Básica` cuando puedas.
6. Abre la Pestaña de Energía con `P` o `K` para inspeccionar la red.

Las máquinas y los objetos de PowerGrid se conectan hacia arriba, abajo, izquierda y derecha. Las diagonales no conectan.

Las máquinas energizadas pueden pasar energía a otras máquinas energizadas que estén tocándolas, así que no necesitas poner cable entre todas las máquinas. Usa los cables como líneas principales.

## Reglas Importantes

- Los generadores con combustible producen todo su EU mientras funcionan.
- El EU sobrante se guarda en baterías si la red tiene espacio.
- Si no hay espacio en baterías, el EU sobrante se desperdicia.
- Las baterías ayudan a suavizar picos de demanda y reducen mucho el desperdicio.
- Los Generadores Eólicos solo producen energía en exteriores.
- La mayoría de las máquinas energizadas siguen funcionando normalmente sin energía.
- La energía es un bono de velocidad, no un reemplazo del funcionamiento normal de las máquinas.
- La principal excepción es el `Horno Eléctrico`: necesita una red energizada activa para empezar a fundir.

## Desbloqueos

| Paquete | Condición de Desbloqueo | Recetas |
| --- | --- | --- |
| Inicio de Red | Minería 5 o conocer Pararrayos | Cable de Cobre, Generador de Vapor, Batería de Energía Básica |
| Artesanía Energizada | Conocer Envasadora o Barril | Envasadora Industrial, Barril Metálico |
| Tecnología de Combustible | Minería 7 y conocer Pararrayos | Biocombustible, Cable de Hierro, Generador de Combustión, Horno Eléctrico, Bobina de Calentamiento, Núcleo de Eficiencia, Cámara Catalítica, Recicladora Industrial, Imán Clasificador, Deshidratador Energizado, Conjunto de Rejillas de Secado, Regulador Térmico |
| Red Avanzada | Minería 9 y conocer Pararrayos | Cable de Iridio, Generador Eólico, Batería de Energía de Iridio, Conducto de Energía, Tonel Metálico, Barril de Iridio Reforzado |
| Red de Alta Densidad | Minería 10, conocer Pararrayos, conocer Panel Solar y conocer Batería de Energía de Iridio | Cable de Iridio Energizado, Combustible de Radioisótopos, Generador de Radioisótopos |

Si usas Generic Mod Config Menu, el comportamiento de los desbloqueos puede cambiarse dentro del juego.

## Generadores

| Generador | Producción | Combustible | Observaciones |
| --- | ---: | --- | --- |
| Generador de Vapor | 75 EU/tick | Carbón, Madera, Madera Noble | Generador inicial. Va bien para salas pequeñas. |
| Generador de Combustión | 240 EU/tick | Biocombustible | Generador de mitad de juego. Va bien para grupos grandes de máquinas. |
| Generador de Radioisótopos | 900 EU/tick | Combustible de Radioisótopos, Barra Radiactiva | Generador de alta densidad para salas de producción pesadas en el juego tardío. |
| Generador Eólico | 25 EU/tick base | ninguno | Energía pasiva, solo en exteriores, ajustada por el clima. |

Recetas de generadores:

| Generador | Receta |
| --- | --- |
| Generador de Vapor | Barra de Hierro x5, Barra de Cobre x2, Carbón x6, Cuarzo Refinado x1 |
| Generador de Combustión | Generador de Vapor x1, Barra de Hierro x8, Barra de Oro x5, Cuarzo Refinado x3 |
| Generador de Radioisótopos | Generador de Combustión x1, Barra Radiactiva x3, Barra de Iridio x6, Batería de Energía de Iridio x1, Cuarzo Refinado x4 |
| Generador Eólico | Barra de Hierro x6, Cuarzo Refinado x4, Batería x1, Carbón x4 |

La producción eólica cambia con el clima:

- Tiempo despejado: producción normal
- Lluvia o tormenta: producción mayor
- Nieve: producción menor

Haz clic derecho en un `Generador de Vapor` mientras sostienes `Carbón`, `Madera` o `Madera Noble` para cargarlo. Haz clic derecho en un `Generador de Combustión` mientras sostienes `Biocombustible` para cargarlo. Haz clic derecho en un `Generador de Radioisótopos` mientras sostienes `Combustible de Radioisótopos` o una `Barra Radiactiva` para cargarlo.

## Combustible

| Combustible | Usado Por | Observaciones |
| --- | --- | --- |
| Carbón | Generador de Vapor | El combustible más fuerte para el Generador de Vapor. |
| Madera | Generador de Vapor | Combustible de respaldo fácil de conseguir. |
| Madera Noble | Generador de Vapor | Mejor que la Madera. |
| Biocombustible | Generador de Combustión | Combustible fabricado de mitad de juego. |
| Combustible de Radioisótopos | Generador de Radioisótopos | Combustible fabricado de juego tardío. Mucho más eficiente que las barras en bruto. |
| Barra Radiactiva | Generador de Radioisótopos | Combustible heredado en bruto. Menos eficiente que el Combustible de Radioisótopos. |

Receta de Biocombustible:

| Receta | Resultado |
| --- | ---: |
| Fibra x10, Madera x5, Carbón x1 | Biocombustible x8 |

Receta de Combustible de Radioisótopos:

| Receta | Resultado |
| --- | ---: |
| Barra Radiactiva x1, Cuarzo Refinado x1 | Combustible de Radioisótopos x7 |

## Cables

| Cable | Receta | Capacidad |
| --- | --- | ---: |
| Cable de Cobre | Barra de Cobre x3 | 10 cables, 50 EU/tick |
| Cable de Hierro | Barra de Hierro x3 | 10 cables, 250 EU/tick |
| Cable de Iridio | Barra de Iridio x2, Cuarzo Refinado x1 | 10 cables, 1.000 EU/tick |
| Cable de Iridio Energizado | Barra de Iridio x4, Barra Radiactiva x1, Batería x1, Cuarzo Refinado x2 | 10 cables, 3.000 EU/tick |

La capacidad es la cantidad de EU que una red puede mover por tick. Si una red mezcla cables de distintos niveles, el cable más débil limita toda la red.

## Baterías

| Batería | Receta | Capacidad |
| --- | --- | ---: |
| Batería de Energía Básica | Batería x1, Barra de Cobre x4, Cuarzo Refinado x1 | 500 EU |
| Batería de Energía de Iridio | Batería x2, Barra de Iridio x2, Cuarzo Refinado x3 | 2.000 EU |

Vale la pena construir baterías pronto. Guardan el EU sobrante, cubren picos de demanda y mantienen las máquinas energizadas cuando los generadores se quedan sin combustible o cuando baja la producción eólica.

El EU almacenado se queda en el propio objeto de la batería cuando la recoges y la colocas en otro sitio.

## Conductos de Energía

Los Conductos de Energía conectan redes entre ubicaciones, como de la Granja a un Cobertizo o de la Granja a la Casa de la Granja.

| Objeto | Receta |
| --- | --- |
| Conducto de Energía | Barra de Iridio x1, Batería x1, Cuarzo Refinado x1 |

Para enlazar conductos:

1. Coloca un conducto en cada ubicación.
2. Conecta cada conducto a su red local.
3. Abre la Pestaña de Energía y selecciona dos conductos para enlazarlos.

También puedes hacer clic derecho en un conducto y luego clic derecho en el otro. `Shift + clic derecho` cancela el emparejamiento o elimina el enlace de un conducto.

## Máquinas Artesanales Energizadas

| Máquina | Receta | Uso de Energía | Bono Máximo |
| --- | --- | ---: | ---: |
| Envasadora Industrial | Madera x30, Carbón x4, Barra de Hierro x4, Cuarzo Refinado x1 | 20 EU/tick | 20% más rápida |
| Barril Metálico | Barra de Hierro x6, Barra de Cobre x4, Cuarzo Refinado x1 | 10 EU/tick | 20% más rápido |
| Barril de Iridio Reforzado | Barra de Iridio x4, Barra de Hierro x2, Cuarzo Refinado x1 | 30 EU/tick | 30% más rápido |
| Tonel Metálico | Madera Noble x8, Barra de Hierro x6, Barra de Iridio x2, Cuarzo Refinado x1 | 40 EU/tick | 50% más rápido al añejar |

El Barril de Iridio Reforzado está pensado para usar el comportamiento del Hardwood Keg de Grapes of Ferngill. Sin Grapes of Ferngill, vuelve al comportamiento del Barril vanilla.

## Máquinas Industriales de Procesamiento de la 0.3

### Horno Eléctrico

| Máquina | Receta | Uso de Energía | Bono Máximo |
| --- | --- | ---: | ---: |
| Horno Eléctrico | Barra de Hierro x8, Barra de Oro x4, Cuarzo Refinado x3, Batería x1 | 40 EU/tick | 50% más rápido |

Qué hace:

- Funde mineral en barras.
- Necesita una red energizada activa antes de empezar.
- Si pierde la conexión de energía a mitad del proceso, el mineral se devuelve en vez de terminar gratis.

Recetas base compatibles:

| Entrada | Salida | Tiempo Base |
| --- | --- | ---: |
| Mineral de Cobre x5 | Barra de Cobre x1 | 30m |
| Mineral de Hierro x5 | Barra de Hierro x1 | 120m |
| Mineral de Oro x5 | Barra de Oro x1 | 300m |
| Mineral de Iridio x5 | Barra de Iridio x1 | 480m |
| Mineral Radiactivo x5 | Barra Radiactiva x1 | 600m |

Probabilidad base de barra extra: 5%

### Recicladora Industrial

| Máquina | Receta | Uso de Energía | Bono Máximo |
| --- | --- | ---: | ---: |
| Recicladora Industrial | Barra de Hierro x6, Cuarzo Refinado x4, Barra de Cobre x4, Carbón x4 | 20 EU/tick | 35% más rápida |

Qué hace:

- Recicla basura compatible en materiales útiles.
- Sigue funcionando sin energía, pero la energía acelera el proceso.
- El tiempo base del proceso es 60m.

Salidas base compatibles:

| Entrada | Salida |
| --- | --- |
| Basura | Piedra x2 |
| Madera Flotante | Madera x2 |
| Gafas Rotas | Cuarzo Refinado x1 |
| CD Roto | Cuarzo Refinado x1 |
| Periódico Empapado | Fibra x3 |
| Refresco Joja | Carbón x1 |

### Deshidratador Energizado

| Máquina | Receta | Uso de Energía | Bono Máximo |
| --- | --- | ---: | ---: |
| Deshidratador Energizado | Barra de Hierro x6, Madera Noble x10, Cuarzo Refinado x3, Batería x1 | 20 EU/tick | 40% más rápido |

Qué hace:

- Procesa frutas y setas como el Deshidratador vanilla.
- Sigue funcionando sin energía, pero la energía acelera el proceso.

## Panel de Máquina y Mejoras

Abre el panel de máquina con `Shift + clic derecho` sobre:

- `Horno Eléctrico`
- `Recicladora Industrial`
- `Deshidratador Energizado`

Cómo usarlo:

1. Sostén una mejora compatible.
2. Abre el panel con `Shift + clic derecho`.
3. Instala la mejora desde el panel.
4. Usa el mismo panel después si quieres quitarla.

Cada nueva máquina industrial tiene actualmente un solo espacio de mejora.

### Recetas de Mejoras

| Mejora | Receta |
| --- | --- |
| Bobina de Calentamiento | Barra de Oro x3, Cuarzo Refinado x2, Carbón x6 |
| Núcleo de Eficiencia | Cuarzo Refinado x4, Batería x1, Barra de Oro x2 |
| Cámara Catalítica | Barra de Iridio x2, Barra de Oro x4, Cuarzo Refinado x4 |
| Imán Clasificador | Barra de Hierro x4, Cuarzo Refinado x2 |
| Conjunto de Rejillas de Secado | Madera x20, Madera Noble x4, Barra de Oro x1 |
| Regulador Térmico | Barra de Oro x2, Cuarzo Refinado x2, Carbón x4 |

### Efectos de las Mejoras

| Mejora | Máquina | Efecto |
| --- | --- | --- |
| Bobina de Calentamiento | Horno Eléctrico | Reduce el tiempo de fundición y aumenta la demanda de EU. |
| Núcleo de Eficiencia | Horno / Recicladora / Deshidratador | Reduce la demanda de EU. |
| Cámara Catalítica | Horno Eléctrico | Aumenta la probabilidad de barra extra y aumenta la demanda de EU. |
| Imán Clasificador | Recicladora Industrial | Añade mejores probabilidades de recuperar metal y aumenta la demanda de EU. |
| Conjunto de Rejillas de Secado | Deshidratador Energizado | Puede mejorar la cantidad producida por lote y aumenta la demanda de EU. |
| Regulador Térmico | Deshidratador Energizado | Reduce el tiempo de deshidratación y aumenta la demanda de EU. |

## Progresión Sugerida

Temprano:

- Generador de Vapor
- Cable de Cobre
- Batería de Energía Básica
- unas cuantas Envasadoras Industriales o Barriles Metálicos

Mitad del juego:

- Biocombustible
- Generador de Combustión
- Cable de Hierro
- Horno Eléctrico
- Recicladora Industrial
- Deshidratador Energizado
- primeras mejoras

Más adelante:

- Cable de Iridio
- Cable de Iridio Energizado
- Generador de Radioisótopos
- Batería de Energía de Iridio
- Conductos de Energía
- Generadores Eólicos en exteriores
- Toneles Metálicos y Barriles de Iridio Reforzado
- ajustes de mejoras en el juego tardío

## Solución de Problemas

Si una máquina no tiene energía:

- Asegúrate de que esté conectada por cable o tocando otra máquina energizada.
- Asegúrate de que el generador tenga combustible o esté produciendo energía.
- Asegúrate de que la máquina esté procesando algo si esperas un bono de velocidad.
- Revisa la Pestaña de Energía para ver la red, la demanda, el almacenamiento y el estado de la máquina.

Si el `Horno Eléctrico` dice que necesita energía:

- No puede empezar a fundir desde una red muerta o desconectada.
- Revisa primero generación activa, combustible, baterías y la adyacencia de los cables.

Si una mejora no se instala:

- Sostén el objeto de mejora antes de abrir el panel de máquina.
- Comprueba que la mejora coincida con esa máquina.
- Recuerda que cada máquina tiene actualmente un solo espacio de mejora.

Si un `Generador Eólico` está desconectado:

- Asegúrate de que esté en exteriores.

Si parece que se está desperdiciando combustible:

- Añade baterías para guardar el EU sobrante.

Si un enlace de conducto está mal:

- Usa la pestaña de Conductos en la Pestaña de Energía.
- `Shift + clic derecho` en un conducto elimina el enlace o cancela el emparejamiento.

## Mods Recomendados

PowerGrid puede jugarse por sí solo, pero se siente mejor con:

- Grapes of Ferngill
- Automate o Event Driven Automation
- Generic Mod Config Menu

## Créditos

- Traducción al español revisada por Hayato2236.
