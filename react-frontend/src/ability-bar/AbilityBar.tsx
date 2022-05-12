import { GetCurrentPlayerQuery, PlayerAbilityFragment } from "../generated/graphql";
import { useMutation } from "@apollo/client";
import { UseAbilityDocument } from "../generated/graphql";
import useCurrentTarget from "../CurrentTargetContext";
import styles from './AbilityBar.module.css';
import { useEffect, useState } from "react";

export interface AbilityBarProps {
    player: GetCurrentPlayerQuery['player'];
}

interface AbilityButtonProps {
    disabled: boolean;
    ability: PlayerAbilityFragment;
}

function AbilityButton({disabled, ability}: AbilityButtonProps) {
    const {currentTargetId} = useCurrentTarget();
    const [executeAbility, {loading}] = useMutation(UseAbilityDocument);
    const [cooldownHandle, setCooldownHandle] = useState<null | NodeJS.Timeout>(null);

    useEffect(() => {
        if (!cooldownHandle) {
            return;
        }
        return () => clearTimeout(cooldownHandle);
    }, [cooldownHandle]);

    async function onUseAbility() {
        if (!currentTargetId) {
            throw new Error();
        }
        console.log('Attacking');
        await executeAbility({
            variables: {
                abilityId: ability.id,
                targetId: currentTargetId
            }
        });
        const handle = setTimeout(() => setCooldownHandle(null), ability.cooldown.total('milliseconds'));
        setCooldownHandle(handle);
    }

    const cooldownStyle = cooldownHandle ? {
        "--cooldownDuration": `${ability.cooldown.total('seconds')}s`
    } as React.CSSProperties: undefined;

    return (
        <button 
            className={styles.abilityButton} 
            onClick={onUseAbility}
            style={cooldownStyle}
            disabled={!currentTargetId || disabled || loading || Boolean(cooldownHandle)}>
            {ability.name}
            {cooldownHandle && <div className={styles.cooldownBar} />}
        </button>
    );
}

export default function AbilityBar({player}: AbilityBarProps) {
    const isDead = player.currentHealthPercent <= 0;

    return (
        <section className={styles.list}>
            {player.abilities.map(ability => <AbilityButton key={ability.id} ability={ability} disabled={isDead} />)}
        </section>
    )
}