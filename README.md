# API provider for CallBrelte

Backend application for the digital implementation of the popular card game 'CallBreak'

## Core features

There's no particular order to the list:

1. Multi-player online game with realtime interactions. Anywhere from 2 to 6 players.
2. Register using a Gmail account to become a user, create rooms and invite other players to the room.
3. No need to register to play; just enter the room UID to join the active game in the room.
4. The registered user who created the room is the room admin, and controls when to start new games.
5. Only one active game per room at one time. Starting a new game in the middle of an existing one deactivates it.
6. Ability to view the the scoreboard of a room's games.
7. Game states are persistent between browser sessions, so no worries if a player gets knocked offline. It'll still be there when they come back.
8. Only one active session permitted per player/user. Multiple browsers or devices for the same player or user, and the server notifications won't reach the duplicate clients.

## Core technologies used

1. ASP.NET Core WebApi in .net 8.0
2. SignalR
3. EFCore wigh npgsql
4. Svelte 5 for the [front-end](https://github.com/globalpolicy/CallBreakFrontEnd)

## More:

Full write-up at [c0dew0rth](https://c0dew0rth.blogspot.com/2024/12/callbreak-online-card-game.html)
> You can go to [Ajashra](https://callbreak.ajashra.com) to play now
