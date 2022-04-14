import { ApolloClient, ApolloProvider, gql, InMemoryCache, useMutation, useQuery } from "@apollo/client";
import { SubscriptionClient } from 'subscriptions-transport-ws'
import { WebSocketLink } from '@apollo/client/link/ws';
import { GetCounterQuery, IncrementCounterMutation } from "./generated/graphql";

const IncrementCounter = gql`
    mutation IncrementCounter {
        incrementCounter
    }
`;

function IncrementButton() {
    const [executeIncrement] = useMutation<IncrementCounterMutation>(IncrementCounter, {
        update(cache) {
            cache.evict({
                fieldName: 'counter',
                id: 'ROOT_QUERY'
            })
        }
    });
    async function onIncrement() {
        console.log('Incrementing');
        executeIncrement();
    }

    return <button onClick={onIncrement}>Increment</button>;
}

const GetCounter = gql`
    query GetCounter {
        counter
    }
`;

function CounterDisplay() {
    const {data, loading, error} = useQuery<GetCounterQuery>(GetCounter);

    return (
        <span>
            {data?.counter}
            {loading && '...'}
            {error && error.message}
        </span>
    )
}


export default function Counter() {
    return (
        <section>
            <CounterDisplay />
            <IncrementButton />
        </section>
    );
}