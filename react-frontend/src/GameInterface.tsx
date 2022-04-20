import AbilityButton from "./AbilityButton";
import CharacterStatusDisplay from "./CharacterStatusDisplay";
import { JoinGameMutation } from "./generated/graphql";

export interface GameInterfaceProps{
    player: JoinGameMutation['joinGame'];
}

const targetId = '78D6A1E6-F6A0-4A71-AE46-E86881B9B527';

export default function GameInterface({player}: GameInterfaceProps) {
    return (
        <div>
            <h1>Welcome {player.name}!</h1>

            <ul>
                <li><CharacterStatusDisplay target={player} /></li>
                <li><CharacterStatusDisplay target={{id: targetId, name: 'Target'}} /></li>
            </ul>

            <AbilityButton playerId={player.id} targetId={targetId} />
        </div>
    );
}