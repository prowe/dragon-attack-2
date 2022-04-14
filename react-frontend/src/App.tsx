import './App.css'
import { ApolloClient, ApolloProvider, InMemoryCache } from "@apollo/client";
import { SubscriptionClient } from 'subscriptions-transport-ws'
import { WebSocketLink } from '@apollo/client/link/ws';
import Counter from './Counter'
import CharacterStatusDisplay from './CharacterStatusDisplay'

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
  return (
    <div className="App">
      <ApolloProvider client={apolloClient}>
        <Counter />
        <CharacterStatusDisplay characterId='empty' />
      </ApolloProvider>
    </div>
  )
}

export default App
