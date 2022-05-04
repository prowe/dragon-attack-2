import { useMutation } from "@apollo/client";
import { FormEventHandler } from "react"
import { JoinGameDocument } from "../generated/graphql";
import styles from './JoinGameForm.module.css';
export interface JoinGameFormProps {
    setCurrentPlayerId: (playerId: string) => void;
}

export default function JoinGameForm({setCurrentPlayerId}: JoinGameFormProps) {
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
        const playerId = joinResult.data?.joinGame.id;
        if (playerId) {
            setCurrentPlayerId(playerId);
        }
    };

    return (
        <form onSubmit={onSubmit} className={styles.form}>
            {error && error.message}
            <label htmlFor="join-game-name">Player Name</label>
            <input name="name" required type="text" id="join-game-form" />
            <button disabled={loading} type='submit'>Join</button>
        </form>
    );
}