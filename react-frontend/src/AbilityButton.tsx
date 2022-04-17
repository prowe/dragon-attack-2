import { gql, useMutation } from "@apollo/client";
import { AttackWithAbilityMutation } from "./generated/graphql";

const AttackWithAbility = gql`
    mutation AttackWithAbility(
        $targetId: String!, 
        $abilityId: String!
    ) {
        attackWithAbility (targetId: $targetId, abilityId: $abilityId)
    }
`;

export interface AbilityButtonProps {
    targetId: string
}

export default function AbilityButton({targetId}: AbilityButtonProps) {
    const [executeAbility] = useMutation<AttackWithAbilityMutation>(AttackWithAbility, {
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