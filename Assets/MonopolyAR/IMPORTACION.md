# Importación de modelos existentes — Monopoly AR

> Registro histórico de la importación inicial. La versión activa actual usa los modelos simplificados y estáticos; Idle y Walk se retiraron del proyecto. Consultar `SIMPLIFICACION.md` para el estado vigente y las pruebas realizadas.

Proyecto verificado por Unity MCP: `D:/Universidad/Game/Monopoly`, Unity `2022.3.62f2`, pipeline existente `URP-HighFidelity`.

Escena guardada: `Assets/MonopolyAR/Scenes/MonopolyAR_Main.unity`.
Respaldo de la escena anterior, incluida la cámara, luz y MCP_Connection_Test: `Assets/MonopolyAR/Scenes/Backups/BeforeImport_20260923_144655.unity`.

## Archivos importados

Las rutas de la tabla son relativas a `Assets/MonopolyAR/`. Todos los prefabs conservan referencias a los FBX originales importados; no son sustituciones por primitivas.

| Modelo | FBX | Prefab |
|---|---|---|
| Empresario | Models/Characters/Empresario/Empresario.fbx | Prefabs/Characters/Empresario.prefab |
| Empresaria | Models/Characters/Empresaria/Empresaria.fbx | Prefabs/Characters/Empresaria.prefab |
| Constructor | Models/Characters/Constructor/Constructor.fbx | Prefabs/Characters/Constructor.prefab |
| Magnate | Models/Characters/Magnate/Magnate.fbx | Prefabs/Characters/Magnate.prefab |
| Tablero | Models/Board/Tablero.fbx | Prefabs/Board/Tablero.prefab |
| Dado_01 | Models/Props/Dado_01.fbx | Prefabs/Props/Dado_01.prefab |
| Dado_02 | Models/Props/Dado_02.fbx | Prefabs/Props/Dado_02.prefab |
| Casa | Models/Buildings/Casa.fbx | Prefabs/Buildings/Casa.prefab |
| Hotel | Models/Buildings/Hotel.fbx | Prefabs/Buildings/Hotel.prefab |
| Estacion | Models/Services/Estacion.fbx | Prefabs/Services/Estacion.prefab |
| CentralElectrica | Models/Services/CentralElectrica.fbx | Prefabs/Services/CentralElectrica.prefab |
| DepositoAgua | Models/Services/DepositoAgua.fbx | Prefabs/Services/DepositoAgua.prefab |

Origen: `C:/Users/NITRO V 15/Desktop/ModelosMonopoly/Blender/`. Se excluyeron respaldos, la escena conjunta, BLEND, Python y la imagen de vista previa. No se encontraron texturas necesarias: los FBX contienen materiales de color plano. Los hashes SHA256 de los 20 FBX copiados coinciden con sus fuentes, sin modificar geometría ni archivos de Blender.

## Animaciones

Cada uno de los cuatro personajes tiene:

- Fuentes: `Models/Characters/<Nombre>/Animations/<Nombre>_Idle.fbx` y `<Nombre>_Walk.fbx`.
- Clips utilizables: `Animations/<Nombre>/Idle.anim` y `Animations/<Nombre>/Walk.anim`.
- Ocho clips en total, de 1 segundo a 24 fps, configurados en bucle.
- Importación Legacy, adecuada a las transformaciones de piezas independientes de estos modelos. No se inventó un esqueleto Humanoid.
- Componente Animation en cada prefab: Idle predeterminado y Walk disponible. Se verificó que cada ruta de curva encuentra su pieza y que ambas animaciones producen movimiento al muestrearlas sobre el modelo estático.

Los FBX animados se conservaron como fuentes de los clips. No se instanciaron sus copias de geometría en la escena ni se crearon personajes duplicados a partir de ellos.

## Escena y referencias del tablero

`BoardRoot/Tablero/Tablero` conserva las 40 mallas `Tile_00`…`Tile_39` y los 40 nodos `Tile_00_Anchor`…`Tile_39_Anchor`, comprobados individualmente. La raíz adicional corresponde a la jerarquía original preservada por el importador.

`Models/Board/TileReferences.csv` contiene las rutas exactas y coordenadas leídas de los Transform importados. No se generaron posiciones de casillas. Para posicionar fichas, utilizar la posición mundial del anclaje; sus rotaciones locales conservan la conversión de ejes de Blender y no representan necesariamente el rumbo del personaje.

El tablero está horizontal en XZ, Y arriba, escala 1, tamaño 14 × 14 unidades. Los personajes miran hacia +Z y mantienen su altura de autoría, aproximadamente 1,8 m (Magnate 2,08 m incluyendo sombrero). No se ajustó todavía el tamaño físico AR.

`AssetPreviewRoot` contiene una muestra de los otros 11 prefabs fuera del perímetro para inspección visual. No representa posiciones de juego. Hay cámara y luz de revisión. `Scripts/` queda disponible, sin lógica de Monopoly ni componentes AR nuevos.

## Ajustes necesarios de importación

- Se mantuvo escala global 1 con las unidades del FBX y jerarquía completa, sin compresión de mallas ni optimización que elimine anclajes. Normales importadas.
- Se organizaron 14 materiales URP/Lit en `Materials/`, preservando los colores importados y dejando metal y suavidad en 0.
- Se consolidaron materiales con sufijos numéricos únicamente después de verificar que su shader y color coincidían con el material base.
- Los materiales rojo, marrón y amarillo se configuraron con ambas caras visibles. Unity ocultaba caras de los techos y piezas planas como la pajarita; este ajuste corrige la visibilidad sin invertir normales ni editar geometría.

## Verificación y pendientes

- 12 prefabs, 20 FBX fuente, 14 materiales y 8 clips verificados.
- Vistas de Unity revisadas: tablero, cuatro personajes, dados, construcciones y servicios. Sin materiales ausentes o shader incompatible.
- Escena guardada. No se añadieron scripts compilables al proyecto y no se detectaron errores nuevos de compilación.
- La consola conserva errores anteriores de autorización MCP. Durante la automatización apareció un aviso `Already exists ... Constructor/Idle.anim` al repetirse una operación: los ocho clips ya estaban creados; se cambió la automatización para conservar los existentes y se verificaron todos. No es un error de compilación ni un recurso pendiente.
- Paquetes y ProjectSettings comprobados por hash sin cambios. No se instalaron paquetes ni se modificaron Unity MCP, AR Foundation o ARCore.
- Pendientes para etapas posteriores: prueba en Android, configuración AR, escala física final, reglas de juego y control de las animaciones desde la lógica del juego.

Registros, hashes, scripts de automatización de esta importación y capturas están en `C:/Users/NITRO V 15/Desktop/ModelosMonopoly/UnityImport/`. Estos scripts C# se ejecutaron mediante MCP y no se copiaron al proyecto.
