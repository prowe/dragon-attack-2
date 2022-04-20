import './App.css'
import { ApolloClient, ApolloProvider, InMemoryCache, NormalizedCacheObject } from "@apollo/client";
import { SubscriptionClient } from 'subscriptions-transport-ws'
import { WebSocketLink } from '@apollo/client/link/ws';
import CharacterStatusDisplay from './CharacterStatusDisplay'
import AbilityButton from './AbilityButton';
import { useState } from 'react';
import { JoinGameMutation } from './generated/graphql';
import JoinGameForm from './JoinGameForm';
import GameInterface from './GameInterface';

const subscriptionClient = new SubscriptionClient('ws://localhost:5000/graphql', {
  connectionParams: {
    playerId: 'player-id-here'
  }
});

const apolloClient = new ApolloClient({
  cache: new InMemoryCache(),
  link: new WebSocketLink(subscriptionClient),
});

function App() {
  const [player, setPlayer] = useState<JoinGameMutation['joinGame'] | null>(null);
  const targetId = '78D6A1E6-F6A0-4A71-AE46-E86881B9B527';

  return (
    <div className="App">
      <ApolloProvider client={apolloClient}>
        {player
          ? <GameInterface player={player} />
          : <JoinGameForm setPlayer={setPlayer} /> }
      </ApolloProvider>
    </div>
  )
}

export default App
