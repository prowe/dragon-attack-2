import { gql, useSubscription } from "@apollo/client";
import { WatchCharacterSubscription, WatchCharacterSubscriptionVariables } from "./generated/graphql";

const watchCharacterSubscription = gql`
    subscription WatchCharacter($characterId: ID!) {
        watchCharacter(id: $characterId) {
            id
            name
            healthPercent
        }
    }
`;

export interface CharacterStatusDisplayProps {
    characterId: string;
}

export default function CharacterStatusDisplay({characterId}: CharacterStatusDisplayProps) {
    const {data, loading, error} = useSubscription<WatchCharacterSubscription, WatchCharacterSubscriptionVariables>(watchCharacterSubscription, {
        variables: {
            characterId
        }
    });

    return (
        <div>
            <h5>{data?.watchCharacter?.name} {loading ?? '...'}</h5>
            <div>{data?.watchCharacter?.healthPercent}%</div>
        </div>
    );
}