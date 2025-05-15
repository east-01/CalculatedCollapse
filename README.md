# Calculated Collapse

A group project by Ethan Mullen, Huu-Khoa Nguyen, Christian Dale Ebuen, Bryle Ong, Khoa Hoang.

Roles:
  - Huu-Khoa Nguyen: Map design, Movement mechanics, Interactions
  - Ethan Mullen: Networking, UI Management, Overall game management
  - Chris Ebuen: Gun design and functionality, Powerups
  - Bryle Ong: Assets, More complicated movement mechanics, Character models
  - Khoa Hoang: Sounds, Throwable objects

#### [v0.1 Demo Video](https://drive.google.com/file/d/1pUqECTYCpDld8ZEBOKNLy0govwYHVLt_/view?usp=sharing)

## Gameplay Description:

Calculated Collapse is a sci fi multiplayer 1v1 tournament style fps game. Two players will be dropped into an arena to fight to the death with a variety of guns for them to use. The format will be a best of 3 to see who can come out on top. Before each round starts, players will be able to move around their side of the map and choose walls, platforms, and other map assets that they want to remove from the game. This will allow them to come up with a strategy to get an edge on their opponent. We wanted to incorporate some strategy into our game, on top of being a fast paced shooter. Our main focus will be on the multiplayer aspect of the game. 

## Get the game

[Downloads](https://drive.google.com/drive/folders/1CohTfrPl6hKRDzemN7bYiC-_HMFwaOLQ?usp=sharing)

Pre-compiled versions (for Windows) are served from the above link. Download the latest version, extract it, and run CalculatedCollapse.exe.<br>
Or you can compile the game yourself- see [build instructions](#build-instructions).

## How to play

Controls:
- WASD to move
- Shift to sprint
- Left Ctrl for sliding
- Left Alt for dash
- 1, 2 or Scroll Wheel to switch guns
- Mouse Left to shoot
- F to destroy walls

## Build Instructions

Currently, there are some issues with the unity package manager- the game will not run from a fresh clone. To fix:
1. Open the package manager
1. Remove the package "net.emullen.networking"
1. Re-add the package: Click "Add package from git URL" and paste `https://github.com/east-01/net.emullen.networking.git`

Once this is done, the game should run and compile as normal.

