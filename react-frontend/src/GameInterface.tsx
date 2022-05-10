import { Reference, useQuery, useSubscription } from "@apollo/client";
import AbilityBar from "./ability-bar/AbilityBar";
import CharactersList from "./character-list/CharactersList";
import { CurrentTargetProvider } from "./CurrentTargetContext";
import { AreaCharacterFragment, AreaCharacterFragmentDoc, GetCurrentPlayerDocument, GetCurrentPlayerQuery, WatchAreaDocument } from "./generated/graphql";
import styles from './GameInterface.module.css';
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
                    charactersPresent: (existingCharacterRefs: Reference[] = []) => {
                        const newCharacterRef = cache.writeFragment({
                            data: newCharacter,
                            fragment: AreaCharacterFragmentDoc
                        });
                          
                        if (existingCharacterRefs.some(ref => ref.__ref === newCharacterRef?.__ref)) {
                            return existingCharacterRefs;
                        }

                        console.log('Modified cache for new data', newCharacterRef);
                        return [...existingCharacterRefs, newCharacterRef];
                    }
                }
            });
        }
    });
}

export default function GameInterface({playerId}: GameInterfaceProps) {
    const {data, loading, error} = useQuery(GetCurrentPlayerDocument);
    const player = data?.player;
    useWatchArea(player?.location);
    if (loading || !player) {
        return <progress />;
    }
    const isDead = player.currentHealthPercent <= 0;
    const charactersInOrder: AreaCharacterFragment[] = [
        player,
        ...player.location.charactersPresent.filter(c => c.id !== playerId)
    ];

    return (
        <CurrentTargetProvider>
            <main className={styles.main}>
                <h1>Welcome {player.name}! 
                    {isDead && <span>(Dead)</span>}
                </h1>
                
                <CharactersList characters={charactersInOrder} />
                
                <AbilityBar player={player} />
            </main>
        </CurrentTargetProvider>
    );
}