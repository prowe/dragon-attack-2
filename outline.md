## Outline

### Version 1.0

1. Introduction
    1. What are we building
    1. Audiance
    1. Level of detail (high level, what, why, assume you can google)
1. Hello GraphQL
    1. C#/ASP.NET
    2. HotChocolate
    3. GraphQL
    1. Demo: increment a counter via api
1. Hello React
    1. Vite
    1. Components
    1. Hooks
    1. Apollo Client
    1. Demo: Increment a counter via a Button
1. Hello Orleans
    1. Silos/Grains
    1. Distributed calling
    1. Demo: Inrement a counter stored in a grain
1. Hello Websockets
    1. Why not http
    1. websocket protocol basics/transports
    1. Demo: Increment over websockets
1. Hello Subscriptions
    1. What are graphql subscriptions
    1. Orleans Streams
    1. Glueing HotChocolate to Orleans
    1. Demo: See other clients count increments
1. Hello Game Character
    1. Change counter grain to game character
    1. Change counter value to hit points
    1. Hard code dragon id
    1. Mutation to use attack
    1. CounterEvent to HealthChangedEvent
    1. UI updates
    1. Demo: Attacking a dragon

### Version 2+

1. Join game form/mutation
1. Set the current player on the connection
1. Area support
1. Multiple Abilities
1. NPC Controller logic (turns, hatelist)
1. Orleans peristance
1. Death Support
1. Dragon re-spawn
1. Ability Cooldowns
1. Healing?
1. Targeting
