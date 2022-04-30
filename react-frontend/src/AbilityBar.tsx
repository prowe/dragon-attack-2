import { GetCurrentPlayerQuery } from "./generated/graphql";
import { useMutation } from "@apollo/client";
import { UseAbilityDocument } from "./generated/graphql";
import useCurrentTarget from "./CurrentTargetContext";

export interface AbilityBarProps {
    player: GetCurrentPlayerQuery['player'];
}

interface AbilityButtonProps {
    playerId: string;
    disabled: boolean;
}

function AbilityButton({playerId, disabled}: AbilityButtonProps) {
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
                abilityId: 'stab',
                targetId: currentTargetId
            }
        });
    }

    return <button onClick={onAttack} disabled={!currentTargetId || disabled} >Stab</button>;
}

export default function AbilityBar({player}: AbilityBarProps) {
    const isDead = player.currentHealthPercent <= 0;

    return (
        <ul>
            <li><AbilityButton playerId={player.id} disabled={isDead} /></li>
        </ul>
    )
}