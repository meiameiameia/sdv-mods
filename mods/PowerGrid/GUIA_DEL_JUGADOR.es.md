# Guía del Jugador - PowerGrid

Esta es una guía rápida para jugar con PowerGrid sin tener que aprenderlo todo a base de prueba y error.

PowerGrid añade generadores, baterías, cables, conductos, Biocombustible y máquinas artesanales con energía. Las máquinas con energía no duplican los productos ni cambian lo que producen. Simplemente funcionan más rápido cuando su red tiene suficiente EU.

## Configuración Inicial

Empieza con algo pequeño:

1. Coloca un Generador de Vapor.
2. Aliméntalo con Carbón, Madera o Madera Noble.
3. Coloca Cable de Cobre junto al generador.
4. Coloca una máquina con energía junto al cable o junto a otra máquina con energía.
5. Añade una Batería de Energía Básica cuando puedas.
6. Abre la Pestaña de Energía con `P` o `K` para revisar la red.

Las máquinas y los objetos de PowerGrid se conectan hacia arriba, abajo, izquierda y derecha. Las diagonales no conectan.

Las máquinas con energía pueden pasar energía a otras máquinas con energía que estén tocándolas, así que no necesitas poner cable entre todas las máquinas. Usa los cables como líneas principales.

## Reglas Importantes

- Los generadores con combustible producen todo su EU mientras funcionan.
- El EU sobrante se guarda en baterías si la red tiene espacio.
- Si no hay espacio en baterías, el EU sobrante se desperdicia.
- Las baterías ayudan a suavizar picos de demanda y reducen bastante el desperdicio.
- Los Generadores Eólicos solo producen energía en exteriores.
- Las máquinas con energía siguen funcionando normalmente sin energía.
- La energía es un bono de velocidad, no un reemplazo del comportamiento normal de las máquinas.

## Desbloqueos

| Paquete | Condición de desbloqueo | Recetas |
| --- | --- | --- |
| Inicio de Red | Minería 5 o conocer Pararrayos | Cable de Cobre, Generador de Vapor, Batería de Energía Básica |
| Artesanía Energizada | Conocer Envasadora o Barril | Envasadora Industrial, Barril Metálico |
| Tecnología de Combustible | Minería 7 y conocer Pararrayos | Biocombustible, Cable de Hierro, Generador de Combustión |
| Red Avanzada | Minería 9 y conocer Pararrayos | Cable de Iridio, Generador Eólico, Batería de Energía de Iridio, Conducto de Energía, Tonel Metálico y Barril de Iridio Reforzado |
| Red de Alta Densidad | Minería 10, conocer Pararrayos, conocer Panel Solar y conocer Batería de Energía de Iridio | Cable de Iridio Energizado y Generador Radioisotópico |

Si usas Generic Mod Config Menu, el comportamiento de los desbloqueos puede cambiarse dentro del juego.

## Generadores

| Generador | Producción | Combustible | Observaciones |
| --- | ---: | --- | --- |
| Generador de Vapor | 75 EU/tick | Carbón, Madera, Madera Noble | Generador inicial. Va bien para salas pequeñas. |
| Generador de Combustión | 240 EU/tick | Biocombustible | Generador de mitad de juego. Va bien para grupos mayores de máquinas. |
| Generador Radioisotópico | 900 EU/tick | Combustible de Radioisótopos, Barra Radiactiva | Generador de alta densidad para juego tardío y salas de producción pesadas. |
| Generador Eólico | 25 EU/tick base | ninguno | Energía pasiva, solo en exteriores y ajustada por el clima. |

La producción eólica cambia con el clima:

- Tiempo despejado: producción normal
- Lluvia o tormenta: producción mayor
- Nieve: producción menor

## Combustible

| Combustible | Usado por | Observaciones |
| --- | --- | --- |
| Carbón | Generador de Vapor | El combustible más fuerte para el Generador de Vapor. |
| Madera | Generador de Vapor | Combustible de respaldo fácil de conseguir. |
| Madera Noble | Generador de Vapor | Mejor que la Madera. |
| Biocombustible | Generador de Combustión | Combustible fabricado para mitad de juego. |
| Combustible de Radioisótopos | Generador Radioisotópico | Combustible fabricado de juego tardío. Mucho más eficiente que barras en bruto. |
| Barra Radiactiva | Generador Radioisotópico | Combustible en bruto heredado. Menos eficiente que el Combustible de Radioisótopos. |

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
| Cable de Iridio Energizado | Barra de Iridio x4, Barra Radiactiva x1, Pila x1, Cuarzo Refinado x2 | 10 cables, 3.000 EU/tick |

La capacidad es la cantidad de EU que una red puede mover por tick. Si una red mezcla cables de distintos niveles, el cable más débil limita toda la red.

## Baterías

| Batería | Receta | Almacenamiento |
| --- | --- | ---: |
| Batería de Energía Básica | Pila x1, Barra de Cobre x4, Cuarzo Refinado x1 | 500 EU |
| Batería de Energía de Iridio | Pila x2, Barra de Iridio x2, Cuarzo Refinado x3 | 2.000 EU |

Vale la pena construir baterías desde temprano. Guardan el EU sobrante, cubren picos de demanda y mantienen las máquinas con energía cuando los generadores se quedan sin combustible o cuando baja la producción eólica.

## Conductos de Energía

Los Conductos de Energía enlazan redes entre ubicaciones, como de la Granja a un Cobertizo o a la Casa de la Granja.

Para enlazar conductos:

1. Coloca un conducto en cada ubicación.
2. Conecta cada conducto a su red local.
3. Abre la Pestaña de Energía y selecciona dos conductos para enlazarlos.

También puedes hacer clic derecho en un conducto y luego clic derecho en el otro. Shift + clic derecho cancela el emparejamiento o elimina el enlace de un conducto.

## Máquinas con Energía

| Máquina | Receta | Uso de Energía | Bono Máximo |
| --- | --- | ---: | ---: |
| Envasadora Industrial | Madera x30, Carbón x4, Barra de Hierro x4, Cuarzo Refinado x1 | 20 EU/tick | 20% más rápida |
| Barril Metálico | Barra de Hierro x6, Barra de Cobre x4, Cuarzo Refinado x1 | 10 EU/tick | 20% más rápido |
| Barril de Iridio Reforzado | Barra de Iridio x4, Barra de Hierro x2, Cuarzo Refinado x1 | 30 EU/tick | 30% más rápido |
| Tonel Metálico | Madera Noble x8, Barra de Hierro x6, Barra de Iridio x2, Cuarzo Refinado x1 | 40 EU/tick | 50% más rápido al añejar |

El Barril de Iridio Reforzado está pensado para usar el comportamiento del barril de madera dura de Grapes of Ferngill. Sin Grapes of Ferngill, vuelve al comportamiento del Barril vanilla.

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
- salas de máquinas más grandes

Más adelante:

- Cable de Iridio
- Cable de Iridio Energizado
- Generador Radioisotópico
- Batería de Energía de Iridio
- Conductos de Energía
- Generadores Eólicos en exteriores
- Toneles Metálicos y Barriles de Iridio Reforzado

## Solución de Problemas

Si una máquina no tiene energía:

- Asegúrate de que esté conectada por cable o tocando otra máquina con energía.
- Asegúrate de que el generador tenga combustible o esté produciendo energía.
- Asegúrate de que la máquina esté procesando algo de verdad.
- Revisa la Pestaña de Energía para ver la red, la demanda, el almacenamiento y el estado de la máquina.

Si un Generador Eólico está desconectado:

- Asegúrate de que esté en exteriores.

Si parece que se está desperdiciando combustible:

- Añade baterías para almacenar el EU sobrante.

Si un enlace de conducto está mal:

- Usa la pestaña de Conductos en la Pestaña de Energía.
- Shift + clic derecho en un conducto para eliminar el enlace o cancelar el emparejamiento.

## Mods Recomendados

PowerGrid puede jugarse por sí solo, pero se siente mejor con:

- Grapes of Ferngill
- Automate o Event Driven Automation
- Generic Mod Config Menu

## Créditos

- Traducción al español: Hayato2236
