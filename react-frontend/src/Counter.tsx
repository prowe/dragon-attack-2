import { ApolloClient, ApolloProvider, gql, InMemoryCache, useMutation, useQuery } from "@apollo/client";
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

const apolloClient = new ApolloClient({
    cache: new InMemoryCache(),
    uri: 'http://localhost:5210/graphql',
});

export default function Counter() {
    return (
        <ApolloProvider client={apolloClient}>
            <section>
                <CounterDisplay />
                <IncrementButton />
            </section>
        </ApolloProvider>
    );
}