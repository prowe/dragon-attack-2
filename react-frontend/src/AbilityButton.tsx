import { useMutation } from "@apollo/client";
import { AttackWithAbilityDocument } from "./generated/graphql";

export interface AbilityButtonProps {
    playerId: string;
    targetId: string | null;
}

export default function AbilityButton({playerId, targetId}: AbilityButtonProps) {
    const [executeAbility] = useMutation(AttackWithAbilityDocument);

    async function onAttack() {
        if (!targetId) {
            throw new Error();
        }
        console.log('Attacking');
        executeAbility({
            variables: {
                playerId,
                targetId,
                abilityId: 'stab'
            }
        });
    }

    return <button onClick={onAttack} disabled={!targetId} >Stab</button>;
}