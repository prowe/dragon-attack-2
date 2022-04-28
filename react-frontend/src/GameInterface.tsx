import { gql, useSubscription } from "@apollo/client";
import { useState } from "react";
import AbilityButton from "./AbilityButton";
import CharacterStatusDisplay from "./CharacterStatusDisplay";
import { JoinGameMutation, WatchAreaDocument } from "./generated/graphql";

export interface GameInterfaceProps{
    player: JoinGameMutation['joinGame'];
}

export default function GameInterface({player}: GameInterfaceProps) {
    const [targetId, setTargetId] = useState<string | null>(null);
    
    useSubscription(WatchAreaDocument, {
        variables: {
            areaId: player.location.id
        },
        onSubscriptionData: ({subscriptionData, client}) => {
            const cache = client.cache;
            cache.modify({
                id: cache.identify(player.location),
                fields: {
                    charactersPresent: (existingCharacters = [], {readField}) => {
                        const newCharacterRef = cache.writeFragment({
                            data: subscriptionData.data?.watchArea.gameCharacter,
                            fragment: gql`
                                fragment NewCharacter on GameCharacter {
                                    id
                                    name
                                }
                            `
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