# Juego-FdV
# BubuPop

Juego de acción multijugador local en un tablero por casillas: coloca burbujas explosivas, rompe obstáculos, recoge power-ups y sé el último en pie.

Proyecto de la asignatura — Grupo 10.
---

## Cómo abrir el juego

### Jugar (build)
1. Descarga el .zip de la entrega y descomprímelo entero。
2. Abre BubuPop.exe (Windows).
3. Si vais a jugar con mando, conectadlo antes de abrir el juego.

### La IA
Los personajes que no controla ninguna persona los maneja la IA. La IA

### Abrir el proyecto en Unity
1. Clona el repositorio: https://github.com/Syting730/BubuPop
2. Abre la carpeta con Unity 6000.6.2f1 desde Unity Hub
3. Abre la escena Assets/Scenes/Menu inicio y pulsa ▶ Play.

---

## Cómo se juega (tutorial)
1. En el menú principal elige:
   - Modo de juego: Clásico o Asalto al Diamante (Boss Volcán todavía no está disponible).
   - Nº de jugadores: de 1 a 4 jugadores reales. El resto de los 4 personajes los controla la IA.
   - Color: cada jugador real elige su color por turnos. Las IA se quedan con los que sobran.
2. Pulsa START.
3. Muévete por los pasillos y pon burbujas. Al cabo de 2 segundos explotan en cruz (2 casillas hacia cada lado):
   - Los muros grises no se rompen y paran la explosión.
   - Los obstáculos morados se rompen (y reaparecen al cabo de un tiempo).
   - Si la explosión te alcanza, pierdes una vida (tienes 3).
4. ¡Apártate de tus propias burbujas! Las casillas que van a explotar se marcan antes de la explosión.

### Modos de juego

Clásico
- Cada jugador tiene 3 vidas.
- Gana el último superviviente.

Asalto al Diamante
- La partida dura 3 minutos.
- El diamante aparece en el centro del tablero, rodeado de obstáculos.
- Cógelo pasando por encima y llévalo a tu base.
- Mientras lo llevas vas más lento y no puedes poner burbujas.
- Si te quitan una vida, el diamante cae al suelo y cualquiera puede cogerlo.
- Puedes soltarlo cuando quieras con tu tecla de soltar (Q, enter, x del mando).
- Puntos: diamante entregado 15, cristal 2, obstáculo roto 1, eliminar a un rival.
- Gana quien tenga más puntos al acabar el tiempo.

### Power-ups
Aparecen en el tablero cada cierto tiempo. Se recogen pasando por encima.

- Escudo: invulnerable durante 5 segundos.
- Guante: permite empujar una burbuja 2 casillas.
- Burbuja extra: permite tener una burbuja más a la vez (máximo 2 extra).


## Controles
Hasta 4 jugadores: 2 en el teclado (Jugador 1 y Jugador 2) y 2 con mando (Jugador 3 y Jugador 4).

### Teclado — Jugador 1 y Jugador 2
- Moverse: W A S D y Flechas
- Poner burbuja: Espacio y Enter 
- Soltar diamante: Q + Ctrl derecho
- Usar guante: Q

### Mando — Jugador 3 y Jugador 4
El primer mando conectado es el del Jugador 3 y el segundo el del Jugador 4.
El jugador se mueve con eo stick izquierdo y se utiliza la A/X (Xbox/PlayStation) para poner burbujas, RB/R1 (Xbox/PS) y menú/opciones para pausar el juego.
