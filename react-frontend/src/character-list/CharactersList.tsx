import { useSubscription } from "@apollo/client";
import { AreaCharacterFragment, WatchCharacterDocument } from "../generated/graphql";
import styles from './CharactersList.module.css';
import useCurrentTarget from "../CurrentTargetContext";

export interface CharactersListProps {
    characters: AreaCharacterFragment[];
}

function CharacterStatusDisplay({gameCharacter}: {gameCharacter: AreaCharacterFragment}) {
    const {currentTargetId, setCurrentTargetId} = useCurrentTarget(); 
    const characterId = gameCharacter.id;
    const {data, loading, error} = useSubscription(WatchCharacterDocument, {
        variables: {
            characterId
        },
        shouldResubscribe: true,
        onSubscriptionData({subscriptionData, client}) {
            client.cache.modify({
                id: client.cache.identify(gameCharacter),
                fields: {
                    currentHealthPercent() {
                        return subscriptionData.data?.watchCharacter.resultingHealthPercent;
                    }
                }                
            });
        }
    });

    return (
        <li className={styles.statusContainer} aria-selected={characterId === currentTargetId} onClick={() => setCurrentTargetId(characterId)}>
            <label>
                {gameCharacter.name}
                {loading ?? '...'}
                {gameCharacter?.currentHealthPercent === 0 && ' (Dead)'}
            </label>
            {error && <div>{error.message}</div>}
            <progress value={gameCharacter.currentHealthPercent} max={100} />
        </li>
    );
}

export default function CharactersList({characters}: CharactersListProps) {
    return (
        <ul className={styles.list}>
            {characters.map(c => <CharacterStatusDisplay key={c.id} gameCharacter={c} />)}
        </ul>
    );
}