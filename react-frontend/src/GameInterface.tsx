import { useQuery, useSubscription } from "@apollo/client";
import { useState } from "react";
import AbilityButton from "./AbilityButton";
import CharacterStatusDisplay from "./CharacterStatusDisplay";
import { AreaCharacterFragmentDoc, GetCurrentPlayerDocument, GetCurrentPlayerQuery, WatchAreaDocument } from "./generated/graphql";

export interface GameInterfaceProps{
    playerId: string;
}

function useWatchArea (area?: GetCurrentPlayerQuery['player']['location']) {
    useSubscription(WatchAreaDocument, {
        skip: !area,
        variables: {
            areaId: area?.id ?? ''
        },
        onSubscriptionData: ({subscriptionData, client}) => {
            if (!area) {
                throw new Error('area is null');
            }
            const newCharacter = subscriptionData.data?.watchArea.gameCharacter;
            const cache = client.cache;
            cache.modify({
                id: cache.identify(area),
                fields: {
                    charactersPresent: (existingCharacters = [], {readField}) => {
                        const newCharacterRef = cache.writeFragment({
                            data: newCharacter,
                            fragment: AreaCharacterFragmentDoc
                        });
                          
                        // Quick safety check - if the new comment is already
                        // present in the cache, we don't need to add it again.
                        // if (existingCommentRefs.some(
                        //     ref => readField('id', ref) === newComment.id
                        // )) {
                        //     return existingCommentRefs;
                        // }

                        console.log('Modified cache for new data', newCharacterRef);
                        return [...existingCharacters, newCharacterRef];
                    }
                }
            });
        }
    });
}

export default function GameInterface({playerId}: GameInterfaceProps) {
    const [targetId, setTargetId] = useState<string | null>(null);
    const {data, loading, error} = useQuery(GetCurrentPlayerDocument, {
        variables: {
            playerId
        },
    });
    const player = data?.player;
    useWatchArea(player?.location);
    if (loading || !player) {
        return <progress />;
    }

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