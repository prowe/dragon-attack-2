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
                {player.location.charactersPresent
                    .map(c => <CharacterStatusDisplay key={c.id} currentTargetId={targetId} setTarget={setTargetId} gameCharacter={c} />)}
            </ul>

            <section>
                <AbilityButton playerId={player.id} targetId={targetId} />
            </section>
        </div>
    );
}