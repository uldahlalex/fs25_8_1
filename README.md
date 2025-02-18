
# Basic WebSocket Connection Management with Dictionaries


(+ Internship Info Meeting with Jesper Ottosen)

Connection management is a difficult subject, so we will ease into it by doing today's content with C# Dictionaries, and make it more scalable next time using Redis.

Today's video shows basic demonstration:

LINK: `https://www.youtube.com/watch?v=uWpmjxv5_6g`
![alt text](image-1.png)

#### Remote repo for today's lesson: `https://github.com/uldahlalex/fs25_8_1`

### Agenda

- 08:15: Presentation
- 09:00: Internship Info Meeting
- 10:00 -> 11:30 Tons of fun with dictionaries

### Topics:

- WebSocket Connection Management and relevant data structures

### Exercises


<!-- #region ex A -->

<details>
    <summary>Adding and removing connections</summary>


<div style="margin: 20px; padding: 5px;  box-shadow: 10px 10px 10px grey;">

## Task:
When clients connect to the API, store the IWebSocktConnection in a dictionary. If they disconnect, remove them from the dictionary.

It is preferable to use a ConcurrentDictionary<Key, Value> due to its thread-safety rather than a plain "Dictionary<Key, Value>".

</div>
</details>

<!-- #endregion ex A -->

_______


<!-- #region ex B -->

<details>
 <summary>Subscribing, unsubscribing and broadcasting</summary>

<div style="margin: 20px; padding: 5px;  box-shadow: 10px 10px 10px grey;">


## Task

Make a dictionary which keeps track of "topic IDs"(key) and a list OR hashset of the socket ID's subscribed to the topic (value).

It is common practice to **also** make a "reverse looup" dictionary which instead uses the connection as a key and list of topics the connection is subscribed to as a value. 

It should be possible for two clients to:
- Connecto the API
- Join a topic (could be something as typical as a "game-room", "receive notifications from X device", etc )
- Broadcast to all members of a topic

</div>
</details>

<!-- #endregion ex B -->

_______


<!-- #region ex C -->

<details>
 <summary>Client ID's vs Socket ID's</summary>

<div style="margin: 20px; padding: 5px;  box-shadow: 10px 10px 10px grey;">


## Task



</div>
</details>

<!-- #endregion ex C -->

_______

