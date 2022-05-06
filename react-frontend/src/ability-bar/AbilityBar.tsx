import { GetCurrentPlayerQuery, PlayerAbilityFragment } from "../generated/graphql";
import { useMutation } from "@apollo/client";
import { UseAbilityDocument } from "../generated/graphql";
import useCurrentTarget from "../CurrentTargetContext";
import styles from './AbilityBar.module.css';
import { Temporal } from "@js-temporal/polyfill";
import { useCallback, useEffect, useState } from "react";

export interface AbilityBarProps {
    player: GetCurrentPlayerQuery['player'];
}

interface AbilityButtonProps {
    playerId: string;
    disabled: boolean;
    ability: PlayerAbilityFragment;
}

function useCooldownState(cooldown: Temporal.Duration) {
    const [expiration, setExpiration] = useState<Temporal.Instant | null>(null);
    const [remainingCooldown, setRemainingCooldown] = useState<Temporal.Duration | null>(null);
    const updateRemainingCooldown = useCallback(() => {
        const remaining = expiration && Temporal.Now.instant().until(expiration);
        setRemainingCooldown(remaining?.sign === 1 ? remaining : null);
    }, [expiration]);

    useEffect(() => {
        if (!expiration) {
            return;
        }
        const handle = setInterval(updateRemainingCooldown, 1000);
        return () => clearInterval(handle);
    }, [expiration, setRemainingCooldown])

    const startCooldown = () => {
        if (cooldown.total("milliseconds") < 1)
        {
            return;
        }
        setExpiration(Temporal.Now.instant().add(cooldown));
        updateRemainingCooldown();
    };
    return {
        remainingCooldown,
        startCooldown
    };
}

function AbilityButton({playerId, disabled, ability}: AbilityButtonProps) {
    const {currentTargetId} = useCurrentTarget();
    const [executeAbility, {loading}] = useMutation(UseAbilityDocument);
    // const {remainingCooldown, startCooldown} = useCooldownState(ability.cooldown);

    async function onAttack() {
        if (!currentTargetId) {
            throw new Error();
        }
        console.log('Attacking');
        await executeAbility({
            variables: {
                playerId,
                abilityId: ability.id,
                targetId: currentTargetId
            }
        });
        // startCooldown();
    }
    return (
        <button onClick={onAttack} disabled={!currentTargetId || disabled || loading}>
            {ability.name}
            {/* {remainingCooldown && ` ${remainingCooldown.total('seconds').toFixed(0)}`} */}
        </button>
    );
}

export default function AbilityBar({player}: AbilityBarProps) {
    const isDead = player.currentHealthPercent <= 0;

    return (
        <ul className={styles.list}>
            {player.abilities.map(ability => <li key={ability.id}><AbilityButton ability={ability} playerId={player.id} disabled={isDead} /></li>)}
        </ul>
    )
}