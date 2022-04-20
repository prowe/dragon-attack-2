import { gql, useSubscription } from "@apollo/client";
import { WatchCharacterDocument, WatchCharacterSubscription, WatchCharacterSubscriptionVariables } from "./generated/graphql";

export interface CharacterStatusDisplayProps {
    characterId: string;
}

export default function CharacterStatusDisplay({characterId}: CharacterStatusDisplayProps) {
    const {data, loading, error} = useSubscription(WatchCharacterDocument, {
        variables: {
            characterId
        }
    });

    return (
        <div>
            <h5>{loading ?? '...'}</h5>
            {error && error.message}
            <div>{data?.watchCharacter?.resultingHealthPercent}%</div>
        </div>
    );
}