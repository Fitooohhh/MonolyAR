# Simplificación de Monopoly AR

Los 12 modelos están procesados, exportados y utilizados por el prototipo de Unity. La escena `MonopolyAR_Main` conserva los dos jugadores, el movimiento por casillas, los dados, los turnos y la interfaz. Las seis pruebas de Play Mode pasan. Los personajes activos son estáticos.

## Archivos y respaldo

- Versiones nuevas: `C:/Users/NITRO V 15/Desktop/ModelosMonopoly/Blender_Simplificado/`.
- Cada modelo tiene `.blend`, `.fbx`, `.py` y `<Nombre>_verificacion.json`.
- Respaldo intacto: `C:/Users/NITRO V 15/Desktop/ModelosMonopoly_BACKUP_PRO/`, con 391 archivos, incluidos los originales animados y `Unity_MonopolyAR/Scenes/MonopolyAR_Main.unity`. No se escribió en esta carpeta.
- Copia adicional de la escena justo antes de actualizar modelos: `C:/Users/NITRO V 15/Desktop/ModelosMonopoly/Simplificacion/MonopolyAR_Main_Antes.unity`.
- Se conservaron las fuentes anteriores bajo `Blender/`; los modelos activos nuevos están bajo `Blender_Simplificado/`.

## Cambios y geometría

Las cifras corresponden a las mallas de Blender, sin contar empties. Los polígonos incluyen quads y ngons; los triángulos equivalentes se comprobaron también en los prefabs importados de Unity. No se utilizó Decimate. Todos los modelos conservan sus límites espaciales originales, materiales de colores planos y escala.

| Modelo | Vértices antes → después | Polígonos antes → después | Triángulos antes → después | Cambio |
|---|---:|---:|---:|---|
| Empresario | 115 → 103 | 85 → 64 | 169 → 127 | Ojos y camisa planos; seis caras ocultas retiradas; cuatro pivotes de animación retirados. |
| Empresaria | 134 → 123 | 98 → 78 | 194 → 155 | Ojos planos, retirado triángulo del cuello y nueve caras ocultas; conserva cabello largo, blusa y falda; sin pivotes. |
| Constructor | 112 → 104 | 84 → 67 | 168 → 134 | Ojos planos y siete caras ocultas retiradas; conserva casco, cuerpo naranja y pantalón azul; sin pivotes. |
| Magnate | 151 → 135 | 101 → 76 | 205 → 155 | Ojos planos, retirado pequeño nudo de pajarita y nueve caras ocultas; conserva sombrero, cinta, bigote y pajarita; sin pivotes. |
| Tablero | 6378 → 2160 | 6080 → 1840 | 6464 → 2114 | Letras con curvas de resolución 2 y franjas de color planas; base, casillas y anclajes preservados. |
| Dado_01 | 512 → 176 | 300 → 27 | 936 → 138 | 21 puntos planos octogonales en lugar de cilindros de 12 lados. |
| Dado_02 | 512 → 176 | 300 → 27 | 936 → 138 | Mismo cambio; se conservaron las seis caras, valores 1–6 y opuestos que suman 7. |
| Casa | 22 → 18 | 17 → 12 | 32 → 22 | Puerta plana; cuerpo verde y techo conservados. |
| Hotel | 40 → 28 | 30 → 15 | 60 → 30 | Puerta y dos ventanas planas; volumen rojo y techo conservados. |
| Estacion | 30 → 30 | 23 → 23 | 44 → 44 | Ya era mínima; se revisó y exportó sin quitar geometría útil. |
| CentralElectrica | 31 → 31 | 17 → 17 | 45 → 45 | Ya era mínima; se conservaron edificio, chimenea y rayo. |
| DepositoAgua | 57 → 57 | 43 → 43 | 90 → 90 | Ya era mínimo; se conservaron soportes, depósito y cubierta de ocho lados. |

Total: **9343 → 3192 triángulos**, reducción del **65,8 %**. Las letras se revisaron visualmente y se dejó resolución 2 para mejorar la legibilidad respecto al primer intento de resolución 1.

## Exportaciones y prefabs

Cada ruta de la primera columna existe bajo `Blender_Simplificado/` con extensiones `.blend`, `.fbx` y `.py`. En Unity, su FBX está bajo `D:/Universidad/Game/Monopoly/Assets/MonopolyAR/Models/`. La segunda columna es relativa a `Assets/MonopolyAR/Prefabs/`.

| Archivo individual, sin extensión | Prefab |
|---|---|
| Characters/Empresario/Empresario | Characters/Empresario.prefab |
| Characters/Empresaria/Empresaria | Characters/Empresaria.prefab |
| Characters/Constructor/Constructor | Characters/Constructor.prefab |
| Characters/Magnate/Magnate | Characters/Magnate.prefab |
| Board/Tablero | Board/Tablero.prefab |
| Props/Dado_01 | Props/Dado_01.prefab |
| Props/Dado_02 | Props/Dado_02.prefab |
| Buildings/Casa | Buildings/Casa.prefab |
| Buildings/Hotel | Buildings/Hotel.prefab |
| Services/Estacion | Services/Estacion.prefab |
| Services/CentralElectrica | Services/CentralElectrica.prefab |
| Services/DepositoAgua | Services/DepositoAgua.prefab |

Los 12 FBX se reimportaron en Blender antes de usarlos en Unity: todos pasaron las comprobaciones de mallas, materiales, límites espaciales, triángulos y ausencia de acciones. Registro: `Verificacion_FBX.json`.

Se actualizaron los 12 FBX existentes de Unity conservando sus GUID y archivos `.meta`. Los 12 prefabs utilizan esas mallas actualizadas. En los cuatro prefabs de personajes se retiraron las referencias de animación y los overrides que habían quedado sin destino. Los materiales URP/Lit existentes se conservaron, sin añadir texturas ni shaders.

La escena conjunta estática adicional está en `Blender_Simplificado/Characters/TodosLosPersonajes/Personajes_Completos.blend` y no se importó como modelo duplicado en Unity.

## Animaciones y scripts

- Cero componentes `Animation` o `Animator` en los 12 prefabs y en la escena activa.
- Cero clips activos en `Assets/MonopolyAR/Animations` y `Models`.
- Retirados de Unity los ocho FBX animados y los ocho clips Legacy Idle/Walk de los cuatro personajes. Antes se verificaron sus copias de respaldo mediante SHA-256 y se comprobó que ningún prefab dependía de ellos.
- `Assets/MonopolyAR/Scripts/MonopolyPlayer.cs`: retirados `modelAnimation`, `SetWalking` y sus llamadas; la validación comprueba que existe el modelo visible. El algoritmo de desplazamiento permanece igual.
- `Assets/MonopolyAR/Tests/PlayMode/PrototypePlayModeTests.cs`: sustituidas las expectativas de Idle/Walk por comprobaciones de modelos estáticos; se verifica que las piezas mantienen su pose durante una vuelta y que los cuatro prefabs no tienen animaciones.
- Sin cambios en `MonopolyBoard.cs`, `MonopolyDice.cs`, `MonopolyTurnManager.cs` ni `MonopolyPrototypeUI.cs`, comprobado mediante SHA-256.
- Los scripts de automatización de este trabajo están fuera de Assets, en `Simplificacion/`. No se añadieron scripts de producción distintos de la modificación mínima de `MonopolyPlayer`.
- Documentación actualizada: `IMPORTACION.md`, `PROTOTIPO.md`, `SIMPLIFICACION.md` y `LEEME_SIMPLIFICACION.md`.

## Tablero

Se mantienen exactamente **40 mallas Tile_00…Tile_39** y **40 Tile_00_Anchor…Tile_39_Anchor**, incluida SALIDA. No se reconstruyó el sistema de anclajes.

En Blender se compararon los nombres, padres, matrices y vértices de las 40 casillas y los 40 anclajes antes y después: igualdad exacta. En Unity se compararon los 80 identificadores `GlobalObjectId` y cada componente de sus posiciones: igualdad exacta. `MonopolyBoard.Validate()` pasó sin volver a registrar referencias ni modificar su código.

Evidencias: `Blender_Simplificado/Board/Anclajes_Antes_Despues.json`, `Unity_Before.txt` y `Unity_FinalAudit.json`.

## Pruebas realizadas

Última ejecución real por Unity MCP: **6/6 pasaron**, cero fallos, cero omitidas, **8,90 segundos**. Registro completo: `PlayModeResults_Final.json`.

1. Empresario y Empresaria empiezan en SALIDA, separados, con botones en el estado correcto y sin Animation/Animator.
2. 1000 parejas de dados: cada valor entre 1 y 6, suma 2–12, aparecen las seis caras.
3. Lanzamiento mediante la interfaz, bloqueo de acciones durante el movimiento, visitas casilla por casilla, finalización de turno, lanzamiento del segundo jugador y vuelta al primero.
4. Recorrido explícito 39 → 0 → 1 usando los anclajes originales.
5. Vuelta completa por las 40 casillas, regreso a SALIDA y pose interna de las piezas sin cambios.
6. Los cuatro prefabs tienen materiales URP/Lit válidos y no contienen componentes ni clips de animación.

Además se abrió Play Mode para revisar visualmente los dos personajes y los dados sobre el tablero. Capturas: `Unity_Prototipo_Estatico.png` y `Personajes_Estaticos.png`.

Escena final guardada: `D:/Universidad/Game/Monopoly/Assets/MonopolyAR/Scenes/MonopolyAR_Main.unity`, fuera de Play Mode, sin cambios pendientes. No se implementaron reglas nuevas ni AR, y no se instalaron paquetes.

La comparación final de 47 archivos protegidos no detectó cambios: Packages, ProjectSettings, materiales y los cuatro scripts funcionales citados. `ProjectSettings.asset` cambió temporalmente durante Play Mode y después volvió a su hash inicial; no se editó manualmente.

## Incidencias y pendiente

- No se detectaron errores de ejecución en las pruebas. La consola histórica conserva ocho mensajes del primer script temporal de inspección MCP: usaba `Newtonsoft`, no disponible en esa compilación dinámica. Se corrigió el script para no depender de esa biblioteca; no se añadió al proyecto ni afecta a la compilación del juego. Los mensajes se conservaron como evidencia, no se ocultaron.
- Blender informó que su complemento usa protocolo 9 y el servidor espera 11; las operaciones utilizadas funcionaron mediante compatibilidad. No se modificó ni actualizó MCP.
- **Pendiente de limpieza de archivos antiguos:** la revisión automática rechazó la eliminación de las carpetas originales de animaciones fuera de Unity, con motivo literal `blocked by policy`. No se intentó eludir el bloqueo. Esas copias permanecen sin uso bajo `Blender/Characters/*/Animations` y en el respaldo histórico anterior, además del respaldo `ModelosMonopoly_BACKUP_PRO`. Por tanto, todavía no se cumple que las copias antiguas existan exclusivamente dentro de `ModelosMonopoly_BACKUP_PRO`. Los nuevos BLEND/FBX y Unity sí están completamente libres de animaciones.
- No se realizó una compilación Android ni pruebas en dispositivo; no formaban parte de esta etapa de simplificación.
