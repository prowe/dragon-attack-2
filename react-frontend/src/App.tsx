import { ApolloClient, ApolloProvider, InMemoryCache } from "@apollo/client";
import { SubscriptionClient } from 'subscriptions-transport-ws'
import { WebSocketLink } from '@apollo/client/link/ws';
import { useState } from 'react';
import JoinGameForm from './join-game/JoinGameForm';
import GameInterface from './GameInterface';
import { Temporal } from "@js-temporal/polyfill";

const graphqlEndpoint = import.meta.env['VITE_GRAPHQL_ENDPOINT_WS'] as string;
console.log('Using endpoint: ', graphqlEndpoint);
const subscriptionClient = new SubscriptionClient(graphqlEndpoint, {
  reconnect: true
});

const apolloClient = new ApolloClient({
  cache: new InMemoryCache({
    typePolicies: {
      Ability: {
        fields: {
          cooldown: {
            read: (existing) => existing ? Temporal.Duration.from(existing) : existing
          }
        }
      }
    }
  }),
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
