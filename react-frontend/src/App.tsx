import './App.css'
import { ApolloClient, ApolloProvider, InMemoryCache, NormalizedCacheObject } from "@apollo/client";
import { SubscriptionClient } from 'subscriptions-transport-ws'
import { WebSocketLink } from '@apollo/client/link/ws';
import CharacterStatusDisplay from './CharacterStatusDisplay'
import AbilityButton from './AbilityButton';
import { useState } from 'react';

function createApolloClient(playerId?: string): ApolloClient<NormalizedCacheObject> {
  const subscriptionClient = new SubscriptionClient('ws://localhost:5000/graphql', {
    connectionParams: {
      playerId: 'player-id-here'
    }
  });

  return new ApolloClient({
    cache: new InMemoryCache(),
    link: new WebSocketLink(subscriptionClient),
  });
}

function App() {
  const [apolloClient, setApolloClient] = useState(createApolloClient);
  const targetId = '78D6A1E6-F6A0-4A71-AE46-E86881B9B527';

  return (
    <div className="App">
      <ApolloProvider client={apolloClient}>
        <CharacterStatusDisplay characterId='1DA1118C-8004-4641-A031-13B624F795D5' />
        <CharacterStatusDisplay characterId={targetId} />

        <AbilityButton targetId={targetId}/>
      </ApolloProvider>
    </div>
  )
}

export default App
