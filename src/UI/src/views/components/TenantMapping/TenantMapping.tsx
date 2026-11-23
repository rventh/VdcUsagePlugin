import React, { useCallback, useEffect, useState } from 'react';
import { Banner, Button, colors, Panel } from '../ui';
import { useUsageApi } from 'core/api';
import type { ApiError, MappingData } from 'core/api';

const SOURCE_TEXT: Record<string, { text: string; bg: string; fg: string }> = {
    pulse: { text: 'Set in Pulse', bg: colors.accentSoft, fg: colors.accentDark },
    manual: { text: 'Chosen here', bg: colors.accentSoft, fg: colors.accentDark },
    name: { text: 'Name match', bg: '#fdf6e8', fg: colors.warn },
    none: { text: 'Not mapped', bg: '#f1f4f7', fg: colors.muted },
};

/** Lets an admin tie each Pulse tenant to a VSPC company. Links are stored by ID in the plugin, and exact name matches are suggested. */
export const TenantMapping: React.FC = () => {
    const api = useUsageApi();
    const [data, setData] = useState<MappingData | null>(null);
    const [error, setError] = useState<ApiError | null>(null);
    const [busy, setBusy] = useState('');

    const load = useCallback(async () => {
        const res = await api.mapping();
        if (res.error) { setError(res.error); return; }
        setError(null);
        setData(res.data);
    }, [api]);

    useEffect(() => { void load(); }, [load]);

    const save = async (tenantUid: string, companyUid: string) => {
        setBusy(tenantUid);
        const res = await api.setMapping(tenantUid, companyUid);
        setBusy('');
        if (res.error) { setError(res.error); return; }
        await load();
    };

    const unmapped = data?.tenants.filter(t => t.source === 'none').length ?? 0;

    return (
        <Panel title='Tenant mapping' hint='Which VSPC company each Veeam Data Cloud tenant belongs to' style={{ marginTop: 14 }}>
            {error ? <Banner kind='error'>{error.message}</Banner> : null}
            {data && data.companies.length === 0 ? <Banner kind='warn'>No VSPC companies could be read, so there is nothing to pick from. Check that the API key can read organizations.</Banner> : null}
            {!data && !error ? <div style={{ color: colors.muted }}>Loading tenants...</div> : null}
            {data ? (
                <>
                    <div style={{ color: colors.muted, marginBottom: 10 }}>
                        {data.tenants.length} tenant{data.tenants.length === 1 ? '' : 's'}, {unmapped} not mapped. A tenant whose name exactly matches one company is matched automatically. Choose "Confirm" to keep it, or pick a different company.
                    </div>
                    <div style={{ overflow: 'auto', border: `1px solid ${colors.border}`, borderRadius: 8 }}>
                        <table className='vdc-table'>
                            <thead>
                                <tr><th style={{ textAlign: 'left' }}>Tenant (Pulse)</th><th style={{ textAlign: 'left' }}>VSPC company</th><th style={{ textAlign: 'left' }}>Status</th><th /></tr>
                            </thead>
                            <tbody>
                                {data.tenants.map(t => {
                                    const src = SOURCE_TEXT[t.source] ?? SOURCE_TEXT.none;
                                    const locked = t.source === 'pulse';
                                    return (
                                        <tr key={t.tenantUid}>
                                            <td style={{ fontWeight: 500, color: colors.ink }} title={t.tenantUid}>{t.tenantName}</td>
                                            <td style={{ minWidth: 240 }}>
                                                <select
                                                    className='vdc-select' aria-label={`VSPC company for ${t.tenantName}`} disabled={locked || busy === t.tenantUid}
                                                    value={t.companyUid} onChange={e => void save(t.tenantUid, e.target.value)}
                                                >
                                                    <option value=''>Not mapped</option>
                                                    {t.companyUid && !data.companies.some(c => c.uid === t.companyUid) ? <option value={t.companyUid}>{t.companyName || t.companyUid}</option> : null}
                                                    {data.companies.map(c => <option key={c.uid} value={c.uid}>{c.name}</option>)}
                                                </select>
                                            </td>
                                            <td><span className='vdc-pill' style={{ background: src.bg, color: src.fg }}>{src.text}</span></td>
                                            <td style={{ textAlign: 'right' }}>
                                                {t.source === 'name' ? <Button disabled={busy === t.tenantUid} onClick={() => void save(t.tenantUid, t.companyUid)}>Confirm</Button> : null}
                                                {t.source === 'manual' ? <Button quiet disabled={busy === t.tenantUid} onClick={() => void save(t.tenantUid, '')}>Clear</Button> : null}
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>
                </>
            ) : null}
        </Panel>
    );
};
