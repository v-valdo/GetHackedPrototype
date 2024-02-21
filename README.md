<h1 align="center">GetHacked - A real-time horror game</h1>

## About
GetHacked is a real-time text based terminal game that is written in C# and works through a HTTP API. The game can be played in the Game Client or simply with the curl commands.
The game is about hacking your opponents and stealing HackerCoinz while defending yourself against other players and trying not to get caught by the police.

## Functions
- Register - register a user with a username and password

- Show stats - shows your current detection rate, firewall health and IP address

- Notepad - shows your targeted attacks against other players, which includes the target's IP address, number of attacks, keyword and password

- IP Scanner - returns 3 random IP addresses (costs 5 HackerCoinz/scan)

- Attack - attack one of the IP addresses, lower their firewall health and receive a letter of the keyword (earn 5 HackerCoinz/attack)

- Auto decrypt - decrypt the password of an opponent with the full keyword and IP address

- Final hack - when firewall health is 0 and you have the opponent's password you can complete the hacking sequence (steal 50 HackerCoinz from opponent)

- Hide me - changes your own IP address and resets your detection rate to 0 (costs 30 HackerCoinz)

- Heal - heals the firewall to max health (costs 10 HackerCoinz)

## Further information
The curl commands can be found in the [`commands`](GameServer/commands) file.

The contributors of the project can be found [here](CONTRIBUTORS.md).
