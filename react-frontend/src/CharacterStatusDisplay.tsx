import { useSubscription } from "@apollo/client";
import { WatchCharacterDocument } from "./generated/graphql";

export interface CharacterStatusDisplayProps {
    target: {id: string, name: string};
}

export default function CharacterStatusDisplay({target}: CharacterStatusDisplayProps) {
    const {data, loading, error} = useSubscription(WatchCharacterDocument, {
        variables: {
            characterId: target.id
        }
    });

    return (
        <div>
            <h5>{target.name}{loading ?? '...'}</h5>
            <div>{data?.watchCharacter?.resultingHealthPercent}%</div>
            {error && error.message}
        </div>
    );
}