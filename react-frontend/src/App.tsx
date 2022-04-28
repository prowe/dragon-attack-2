import './App.css'
import { ApolloClient, ApolloProvider, InMemoryCache, NormalizedCacheObject } from "@apollo/client";
import { SubscriptionClient } from 'subscriptions-transport-ws'
import { WebSocketLink } from '@apollo/client/link/ws';
import { useState } from 'react';
import { JoinGameMutation } from './generated/graphql';
import JoinGameForm from './JoinGameForm';
import GameInterface from './GameInterface';

const subscriptionClient = new SubscriptionClient('ws://localhost:5000/graphql', {
  reconnect: true
});

const apolloClient = new ApolloClient({
  cache: new InMemoryCache(),
  link: new WebSocketLink(subscriptionClient),
});

function App() {
  const [player, setPlayer] = useState<JoinGameMutation['joinGame'] | null>(null);

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
