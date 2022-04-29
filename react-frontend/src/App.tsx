import './App.css'
import { ApolloClient, ApolloProvider, InMemoryCache } from "@apollo/client";
import { SubscriptionClient } from 'subscriptions-transport-ws'
import { WebSocketLink } from '@apollo/client/link/ws';
import { useState } from 'react';
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
  const [currentPlayerId, setCurrentPlayerId] = useState<string | null>(null);
  function doSetCurrentPlayerId(playerId: string) {
    // @ts-expect-error 2341
    subscriptionClient.sendMessage(undefined, 'connection_init', {playerId});
    // @ts-expect-error 2341
    subscriptionClient.flushUnsentMessagesQueue();
    setCurrentPlayerId(playerId);
  }

  return (
    <div className="App">
      <ApolloProvider client={apolloClient}>
        {currentPlayerId
          ? <GameInterface playerId={currentPlayerId} />
          : <JoinGameForm setCurrentPlayerId={doSetCurrentPlayerId} /> }
      </ApolloProvider>
    </div>
  )
}

export default App
