import { useMutation } from "@apollo/client";
import { FormEventHandler } from "react"
import { JoinGameDocument, JoinGameMutation, JoinGameMutationVariables } from "./generated/graphql";

export interface JoinGameFormProps {
    setPlayer: (player: JoinGameMutation['joinGame'] | null) => void;
}

export default function JoinGameForm({setPlayer}: JoinGameFormProps) {
    const [callJoin, { loading, error, data }] = useMutation(JoinGameDocument);

    const onSubmit: FormEventHandler<HTMLFormElement> = async (event) => {
        event.preventDefault();
        const form = event.currentTarget;
        const name = (form.elements.namedItem('name') as HTMLInputElement).value as string;
        const joinResult = await callJoin({
            variables: {
                name
            }
        });
        setPlayer(joinResult.data?.joinGame ?? null);
    };

    return (
        <form onSubmit={onSubmit}>
            {error && error.message}
            <label htmlFor="join-game-name">Name</label>
            <input name="name" required type="text" id="join-game-form" />
            <button disabled={loading} type='submit'>Join</button>
        </form>
    );
}