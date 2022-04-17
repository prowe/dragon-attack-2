import { gql, useMutation } from "@apollo/client";
import { IncrementCounterMutation } from "./generated/graphql";

const IncrementCounter = gql`
    mutation IncrementCounter {
        incrementCounter
    }
`;

function IncrementButton() {
    const [executeIncrement] = useMutation<IncrementCounterMutation>(IncrementCounter, {
    });
    async function onIncrement() {
        console.log('Incrementing');
        executeIncrement();
    }

    return <button onClick={onIncrement}>Increment</button>;
}

export default function Counter() {
    return (
        <section>
            <IncrementButton />
        </section>
    );
}