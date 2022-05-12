import { GetCurrentPlayerQuery, PlayerAbilityFragment } from "../generated/graphql";
import { useMutation } from "@apollo/client";
import { UseAbilityDocument } from "../generated/graphql";
import useCurrentTarget from "../CurrentTargetContext";
import styles from './AbilityBar.module.css';
import { Temporal } from "@js-temporal/polyfill";
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
    const [remainingCooldown, setRemainingCooldown] = useState<Temporal.Duration | null>(null);
    const [cooldownExpiration, setCooldownExpiration] = useState<Temporal.Instant | null>(null);

    useEffect(() => {
        if (!cooldownExpiration) {
            return;
        }
        function updateRemainingCooldown(expiration: Temporal.Instant) {
            const remaining = Temporal.Now.instant().until(expiration);
            setRemainingCooldown(remaining.sign > 0 ? remaining : null);
        }
        const handle = setInterval(updateRemainingCooldown, 1000, cooldownExpiration);
        return () => clearInterval(handle);
    }, [cooldownExpiration, setRemainingCooldown])

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
        setRemainingCooldown(ability.cooldown);
        setCooldownExpiration(Temporal.Now.instant().add(ability.cooldown));
    }
    return (
        <button className={styles.abilityButton} onClick={onUseAbility} disabled={!currentTargetId || disabled || loading || Boolean(remainingCooldown)}>
            {ability.name}
            {remainingCooldown && ` ${remainingCooldown.total('seconds').toFixed(0)}`}
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