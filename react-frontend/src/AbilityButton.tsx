import { useMutation } from "@apollo/client";
import { AttackWithAbilityDocument } from "./generated/graphql";

export interface AbilityButtonProps {
    playerId: string;
    targetId: string;
}

export default function AbilityButton({playerId, targetId}: AbilityButtonProps) {
    const [executeAbility] = useMutation(AttackWithAbilityDocument, {
        variables: {
            playerId,
            targetId,
            abilityId: 'stab'
        }
    });

    async function onAttack() {
        console.log('Attacking');
        executeAbility();
    }

    return <button onClick={onAttack}>Stab</button>;
}