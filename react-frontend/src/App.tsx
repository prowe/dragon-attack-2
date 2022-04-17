import './App.css'
import { ApolloClient, ApolloProvider, InMemoryCache } from "@apollo/client";
import { SubscriptionClient } from 'subscriptions-transport-ws'
import { WebSocketLink } from '@apollo/client/link/ws';
import Counter from './Counter'
import CharacterStatusDisplay from './CharacterStatusDisplay'
import AbilityButton from './AbilityButton';

const wsLink = new WebSocketLink(
  new SubscriptionClient('ws://localhost:5000/graphql', {
  })
);

const apolloClient = new ApolloClient({
  cache: new InMemoryCache(),
  link: wsLink,
  // uri: 'http://localhost:5210/graphql',
});

function App() {
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
