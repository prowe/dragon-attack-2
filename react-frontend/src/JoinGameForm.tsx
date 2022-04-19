import { gql, useMutation } from "@apollo/client";
import { FormEventHandler } from "react"

const JoinGame = gql`
    mutation JoinGame($name: string!) {
        joinGame(name: $name) {
            id
        }
    }
`;

export default function JoinGameForm() {
    const [callJoin, {loading} = useMutation<JoinGameMutation>(JoinGame);

    const onSubmit: FormEventHandler<HTMLFormElement> = async (event) => {
        event.preventDefault();
        const form = event.currentTarget;
        const name = (form.elements.namedItem('name') as HTMLInputElement).value as string;
        const player = await callJoin({
            name
        });
    };

    return (
        <form onSubmit={onSubmit}>
           <label htmlFor="join-game-name">Name</label>
           <input name="name" required type="text" id="join-game-form" />
        </form>
    );
}