import { JoinGameMutation } from "./generated/graphql";

export interface GameInterfaceProps{
    player: JoinGameMutation['joinGame'];
}

export default function GameInterface({player}: GameInterfaceProps) {
    return (
        <div>
            <h1>Welcome {player.name}!</h1>
        </div>
    );
}