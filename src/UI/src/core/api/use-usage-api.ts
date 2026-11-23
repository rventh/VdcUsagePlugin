import { useGlobalServices } from '@veeam-vspc/shared/components';
import { useMemo } from 'react';
import { getMyPluginPrefix } from 'core/helpers';
import { unwrap, toQuery } from './usage-api';
import type { Dashboard, Filters, KeyStatus, MappingData, Options, RowsResult, FilePayload } from './usage-api';

export const useUsageApi = () => {
    const { transportService } = useGlobalServices();
    return useMemo(() => {
        const get = <T>(path: string) => unwrap<T>(transportService.get<unknown, unknown>(getMyPluginPrefix(path), undefined, { skipRestPrefix: true }));
        return {
            options: () => get<Options>('/usage/options'),
            rows: (f: Filters, extra: Record<string, string | number>) => get<RowsResult>(`/usage/rows${toQuery(f, extra)}`),
            dashboard: (f: Filters) => get<Dashboard>(`/usage/dashboard${toQuery(f)}`),
            exportFile: (kind: 'csv' | 'pdf', f: Filters) => get<FilePayload>(`/usage/export/${kind}${toQuery(f)}`),
            refresh: () => unwrap<{ rows: number }>(transportService.post<unknown, unknown>(getMyPluginPrefix('/usage/refresh'), undefined, { skipRestPrefix: true })),
            settings: () => get<KeyStatus>('/settings'),
            mapping: () => get<MappingData>('/mapping'),
            setMapping: (tenantUid: string, companyUid: string) => unwrap<{ saved: boolean }>(transportService.put<unknown, unknown>(getMyPluginPrefix('/mapping'), { tenantUid, companyUid } as any, { skipRestPrefix: true })),
            saveKey: (apiKey: string) => unwrap<{ saved: boolean }>(transportService.put<unknown, unknown>(getMyPluginPrefix('/settings/apikey'), { apiKey } as any, { skipRestPrefix: true })),
            removeKey: () => unwrap<{ removed: boolean }>(transportService.delete<unknown, unknown>(getMyPluginPrefix('/settings/apikey'), undefined, { skipRestPrefix: true })),
        };
    }, [transportService]);
};
