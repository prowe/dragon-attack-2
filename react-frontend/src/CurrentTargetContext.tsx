import { createContext, ReactNode, useContext, useMemo, useState } from "react";

const Context = createContext<CurrentTargetContext>({
    currentTargetId: null,
    setCurrentTargetId: () => undefined
});

export interface CurrentTargetContext {
    currentTargetId: string | null;
    setCurrentTargetId: (id: string | null) => void;
}

export default function useCurrentTarget(): CurrentTargetContext {
    return useContext(Context);
}

export function CurrentTargetProvider({children}: {children: ReactNode}) {
    const [currentTargetId, setCurrentTargetId] = useState<string | null>(null);
    const value = useMemo((): CurrentTargetContext => ({
        currentTargetId,
        setCurrentTargetId
    }), [currentTargetId, setCurrentTargetId]);

    return (
        <Context.Provider value={value}>
            {children}
        </Context.Provider>
    );
}

