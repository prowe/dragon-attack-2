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
        }
    });
    const currentHealthPercent = data?.watchCharacter?.resultingHealthPercent ?? gameCharacter.currentHealthPercent;

    return (
        <li className={styles.statusContainer} aria-selected={characterId === currentTargetId} onClick={() => setTarget(characterId)}>
            <h5>{gameCharacter.name}{loading ?? '...'}</h5>
            <div>{currentHealthPercent}%</div>
            {error && error.message}
        </li>
    );
}