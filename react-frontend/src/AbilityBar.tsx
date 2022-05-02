import { GetCurrentPlayerQuery, PlayerAbilityFragment } from "./generated/graphql";
import { useMutation } from "@apollo/client";
import { UseAbilityDocument } from "./generated/graphql";
import useCurrentTarget from "./CurrentTargetContext";
import styles from './AbilityBar.module.css';

export interface AbilityBarProps {
    player: GetCurrentPlayerQuery['player'];
}

interface AbilityButtonProps {
    playerId: string;
    disabled: boolean;
    ability: PlayerAbilityFragment;
}

function AbilityButton({playerId, disabled, ability}: AbilityButtonProps) {
    const {currentTargetId} = useCurrentTarget();
    const [executeAbility] = useMutation(UseAbilityDocument);

    async function onAttack() {
        if (!currentTargetId) {
            throw new Error();
        }
        console.log('Attacking');
        executeAbility({
            variables: {
                playerId,
                abilityId: ability.id,
                targetId: currentTargetId
            }
        });
    }

    return <button onClick={onAttack} disabled={!currentTargetId || disabled}>{ability.name}</button>;
}

export default function AbilityBar({player}: AbilityBarProps) {
    const isDead = player.currentHealthPercent <= 0;

    return (
        <ul className={styles.list}>
            {player.abilities.map(ability => <li key={ability.id}><AbilityButton ability={ability} playerId={player.id} disabled={isDead} /></li>)}
        </ul>
    )
}