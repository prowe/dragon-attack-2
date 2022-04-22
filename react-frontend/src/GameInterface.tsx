import { useState } from "react";
import AbilityButton from "./AbilityButton";
import CharacterStatusDisplay from "./CharacterStatusDisplay";
import { JoinGameMutation } from "./generated/graphql";

export interface GameInterfaceProps{
    player: JoinGameMutation['joinGame'];
}

export default function GameInterface({player}: GameInterfaceProps) {
    const [targetId, setTargetId] = useState<string | null>(null);

    return (
        <div>
            <h1>Welcome {player.name}!</h1>

            <ul>
                <li><CharacterStatusDisplay currentTargetId={targetId} setTarget={setTargetId} gameCharacter={player} /></li>
                <li><CharacterStatusDisplay currentTargetId={targetId} setTarget={setTargetId} gameCharacter={{id: '78D6A1E6-F6A0-4A71-AE46-E86881B9B527', name: 'Target'}} /></li>
            </ul>

            <section>
                <AbilityButton playerId={player.id} targetId={targetId} />
            </section>
        </div>
    );
}