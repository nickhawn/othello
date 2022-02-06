# Othello

An Othello player written in C#.

## Server

Usage:
```
java -jar othello.jar
java -jar othello.jar --p1-type remote --p2-type random --wait-for-ui
```

Options:
```
      --p1-type TYPE          :remote     Player one's type - remote, random, or robot
      --p2-type TYPE          :remote     Player two's type - remote, random, or robot
      --p1-name NAME          Player One  Player one's team name
      --p2-name NAME          Player Two  Player two's team name
      --p1-moves MOVES        []          Moves for a P1 robot player
      --p2-moves MOVES        []          Moves for a P2 robot player
      --p1-port PORT          1337        Port number for the P1 client
      --p2-port PORT          1338        Port number for the P2 client
      --ui-port PORT          8080        Port number for UI clients
  -w, --wait-for-ui                       Wait for a UI client to connect before starting game
  -m, --min-turn-time MILLIS  1000        Minimum amount of time to wait between turns.
  -x, --max-turn-time MILLIS  15000       Maximum amount of time to allow an AI for a turn.
  -h, --help
```
## Client

Usage:
```
dotnet run --project cs/othello -- -p 1337
```

## Demo

![Win Screenshot](/docs/win.png)

