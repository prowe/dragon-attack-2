import { useMutation } from "@apollo/client";
import { AttackWithAbilityDocument } from "./generated/graphql";

export interface AbilityButtonProps {
    targetId: string
}

export default function AbilityButton({targetId}: AbilityButtonProps) {
    const [executeAbility] = useMutation(AttackWithAbilityDocument, {
        variables: {
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