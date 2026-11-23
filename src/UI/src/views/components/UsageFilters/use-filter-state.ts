import { useCallback, useEffect, useState } from 'react';
import { getSharedFilters, setSharedFilters, useUsageApi } from 'core/api';
import type { ApiError, Filters, Options } from 'core/api';

/** Loads the filter choices once and keeps the chosen filters shared between the report and the dashboard. */
export const useFilterState = () => {
    const api = useUsageApi();
    const [options, setOptions] = useState<Options | null>(null);
    const [error, setError] = useState<ApiError | null>(null);
    const [filters, setFiltersState] = useState<Filters>(getSharedFilters());

    const setFilters = useCallback((f: Filters) => { setSharedFilters(f); setFiltersState(f); }, []);

    const loadOptions = useCallback(async () => {
        setError(null);
        const res = await api.options();
        if (res.error) setError(res.error); else setOptions(res.data);
    }, [api]);

    useEffect(() => { void loadOptions(); }, [loadOptions]);

    const refresh = useCallback(async () => {
        setError(null);
        const res = await api.refresh();
        if (res.error) { setError(res.error); return false; }
        await loadOptions();
        return true;
    }, [api, loadOptions]);

    return { api, options, error, filters, setFilters, refresh };
};
