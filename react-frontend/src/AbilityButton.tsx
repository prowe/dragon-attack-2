import { useMutation } from "@apollo/client";
import { UseAbilityDocument } from "./generated/graphql";

export interface AbilityButtonProps {
    playerId: string;
    targetId: string | null;
}

export default function AbilityButton({playerId, targetId}: AbilityButtonProps) {
    const [executeAbility] = useMutation(UseAbilityDocument);

    async function onAttack() {
        if (!targetId) {
            throw new Error();
        }
        console.log('Attacking');
        executeAbility({
            variables: {
                playerId,
                abilityId: 'stab',
                targetId,
            }
        });
    }

    return <button onClick={onAttack} disabled={!targetId} >Stab</button>;
}