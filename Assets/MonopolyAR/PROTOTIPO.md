# Prototipo de turnos para dos jugadores

> Actualización: los cuatro personajes ahora son estáticos. `MonopolyPlayer` ya no requiere ni reproduce Idle/Walk; el recorrido, dados, turnos y botones se conservan. Consultar `SIMPLIFICACION.md`. Las menciones de animaciones más abajo describen la versión anterior.

Abrir `Scenes/MonopolyAR_Main.unity` y pulsar **Play**. Empresario y Empresaria comienzan en la casilla 00, separados visualmente. Pulsar **Lanzar dados**, esperar a que termine el recorrido y pulsar **Finalizar turno**. El control pasa al otro jugador. No hay turnos extra por dobles ni reglas económicas.

## Scripts

- `Scripts/MonopolyBoard.cs`: registra las 40 casillas y sus anclajes originales por nombre, valida las referencias y calcula la posición desde esos Transform. La numeración no depende del orden de la jerarquía.
- `Scripts/MonopolyPlayer.cs`: recorre cada anclaje intermedio; reproduce Walk durante el movimiento e Idle al detenerse. Conserva la separación de cada ficha y registra el recorrido para las pruebas.
- `Scripts/MonopolyDice.cs`: genera dos valores de 1 a 6, calcula su suma y anima visualmente las instancias originales de los dados.
- `Scripts/MonopolyTurnManager.cs`: controla los estados de lanzamiento, movimiento y finalización. Impide lanzamientos repetidos y cambios de turno durante el movimiento. Admite una lista de entre dos y cuatro jugadores.
- `Scripts/MonopolyPrototypeUI.cs`: interfaz uGUI con jugador, casilla, resultados individuales, suma, botones y numeración de casillas. Usa controles laterales en horizontal y paneles superior e inferior en vertical. Se crea al entrar en Play.
- `Tests/PlayMode/PrototypePlayModeTests.cs`: pruebas de integración ejecutables en Unity Test Runner, PlayMode, ensamblado `MonopolyAR.PlayModeTests`.

## Escena y recursos

`BoardRoot` conserva el tablero original. El componente MonopolyBoard contiene las referencias serializadas de `Tile_00_Anchor` hasta `Tile_39_Anchor`. El archivo original `Blender/Board/Casillas.json` identifica 00 como SALIDA; la secuencia numérica sigue el perímetro y vuelve de 39 a 00. No se reconstruyó el tablero ni se inventaron coordenadas.

`PrototypeRoot/Players` contiene dos contenedores con instancias de los prefabs originales. Se utiliza escala de instancia 0,55 para que las fichas quepan en las casillas y separaciones de -0,25 y +0,25 unidades locales del tablero. No se alteraron los prefabs ni las mallas.

`PrototypeRoot/Dice` contiene las instancias de Dado_01 y Dado_02. `AssetPreviewRoot` queda desactivado, conservando la galería de importación. La cámara encuadra el tablero automáticamente durante la partida.

Idle y Walk siguen siendo los clips Legacy existentes. No se generaron animaciones nuevas. Para ampliar a cuatro jugadores, añadir dos contenedores con las instancias originales de Constructor y Magnate, configurar MonopolyPlayer con sus Animation y separaciones distintas, y asignarlos al arreglo `players` del administrador.

Respaldo previo: `Scenes/Backups/BeforePrototype_20260923_150256.unity`.

## Pruebas realizadas

Las pruebas comprueban:

1. Ambos jugadores comienzan en 00, en sus anclajes con separación e Idle; estado inicial de los botones.
2. 1.000 pares aleatorios producen valores de 1 a 6 y sumas de 2 a 12, con aparición de las seis caras.
3. Los eventos de los botones lanzan, bloquean las acciones mientras hay movimiento, recorren los anclajes uno a uno y alternan ambos turnos. Se comprueban Walk e Idle.
4. Un movimiento real desde 39 visita 00 y después 01.
5. Una vuelta completa visita las 40 casillas y regresa al anclaje de salida.

Los informes de ejecución y las capturas están en `C:/Users/NITRO V 15/Desktop/ModelosMonopoly/UnityPrototype/`.

Verificación final del 27/09/2026: **5 pruebas aprobadas, 0 fallidas**, ejecutadas de nuevo después del ajuste de interfaz (10,08 segundos). Resultado en `PlayModeResults_Final.json` y captura real del Game View en `Prototype_Final.png`. Se revisó la interfaz horizontal en el Editor. Se conservaron por hash todos los modelos, materiales, animaciones y prefabs existentes, además de Packages. `ProjectSettings.asset` difiere de la instantánea del 23/09; no se restauró ni se editó durante esta finalización para no sobrescribir cambios posteriores del proyecto.

## Límites deliberados

- Las caras visibles de los dados no se orientan al resultado: la animación es decorativa y los números de la interfaz son el resultado válido.
- No hay compras, alquileres, tarjetas, construcciones, cárcel funcional, premios por salida, guardado de partida ni reglas adicionales.
- No se configuró AR ni se instalaron paquetes. La validación es en el Editor; falta comprobar el prototipo en un dispositivo Android.
- La interfaz es provisional y los números de casilla se pueden ocultar para despejar el tablero.
