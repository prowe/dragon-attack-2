import { useSubscription } from "@apollo/client";
import { WatchCharacterDocument } from "./generated/graphql";
import styles from './CharacterStatusDisplay.module.css';

export interface CharacterStatusDisplayProps {
    gameCharacter: {id: string, name: string, currentHealthPercent: number};
    currentTargetId: string | null;
    setTarget: (targetId: string | null) => void;
}

export default function CharacterStatusDisplay({gameCharacter, currentTargetId, setTarget}: CharacterStatusDisplayProps) {
    const characterId = gameCharacter.id;
    const {data, loading, error} = useSubscription(WatchCharacterDocument, {
        variables: {
            characterId
        },
        shouldResubscribe: true,
    });
    const currentHealthPercent = data?.watchCharacter?.resultingHealthPercent ?? gameCharacter.currentHealthPercent;

    return (
        <li className={styles.statusContainer} aria-selected={characterId === currentTargetId} onClick={() => setTarget(characterId)}>
            <label>{gameCharacter.name}{loading ?? '...'}</label>
            {error && <div>{error.message}</div>}
            <progress value={currentHealthPercent} max={100} />
        </li>
    );
}
