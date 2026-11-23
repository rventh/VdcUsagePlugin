import React, { useEffect, useState } from 'react';
import { Banner, Button, colors, FamilyPill, Page, Panel, SkeletonRows } from 'views/components/ui';
import { UsageFilters, useFilterState } from 'views/components/UsageFilters';
import { fmt, formatUnits, monthLabel, saveFile } from 'core/api';
import type { ApiError, RowsResult } from 'core/api';

const PAGE_SIZE = 50;
type SortKey = 'month' | 'tenant' | 'product' | 'family' | 'units' | 'ppu' | 'points';

export const UsageReportPage: React.FC = () => {
    const { api, options, error: optionsError, filters, setFilters, refresh } = useFilterState();
    const [result, setResult] = useState<RowsResult | null>(null);
    const [error, setError] = useState<ApiError | null>(null);
    const [loading, setLoading] = useState(true);
    const [exporting, setExporting] = useState('');
    const [offset, setOffset] = useState(0);
    const [sort, setSort] = useState<{ key: SortKey; dir: 'asc' | 'desc' }>({ key: 'month', dir: 'desc' });
    const filterKey = JSON.stringify(filters);

    useEffect(() => { setOffset(0); }, [filterKey, sort.key, sort.dir]);

    useEffect(() => {
        let cancelled = false;
        setLoading(true);
        void api.rows(filters, { offset, limit: PAGE_SIZE, sort: sort.key, dir: sort.dir }).then(res => {
            if (cancelled) return;
            setLoading(false);
            if (res.error) { setError(res.error); setResult(null); } else { setError(null); setResult(res.data); }
        });
        return () => { cancelled = true; };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [api, filterKey, offset, sort.key, sort.dir]);

    const doExport = async (kind: 'csv' | 'pdf') => {
        setExporting(kind);
        const res = await api.exportFile(kind, filters);
        setExporting('');
        if (res.error || !res.data) { setError(res.error); return; }
        saveFile(res.data);
    };

    const head = (label: string, key: SortKey, right = false) => (
        <th
            className='sortable'
            onClick={() => setSort(s => ({ key, dir: s.key === key && s.dir === 'desc' ? 'asc' : 'desc' }))}
            style={{ textAlign: right ? 'right' : 'left' }}
            aria-sort={sort.key === key ? (sort.dir === 'asc' ? 'ascending' : 'descending') : 'none'}
        >
            {label}<span style={{ color: colors.accent }}>{sort.key === key ? (sort.dir === 'asc' ? ' ▲' : ' ▼') : ''}</span>
        </th>
    );

    const shownError = error ?? optionsError;
    const total = result?.total ?? 0;
    const maxPoints = Math.max(1, ...(result?.rows ?? []).map(r => r.points));

    return (
        <Page
            title='Usage report'
            subtitle={options?.pulseLastUpdate ? `Veeam Data Cloud usage from Pulse, last updated ${options.pulseLastUpdate}` : 'Veeam Data Cloud usage from Pulse'}
            actions={(
                <>
                    <Button disabled={loading} onClick={() => void refresh().then(() => setFilters({ ...filters }))}>Reload from VSPC</Button>
                    <Button disabled={!!exporting || total === 0} onClick={() => void doExport('csv')}>{exporting === 'csv' ? 'Exporting...' : 'Export CSV'}</Button>
                    <Button primary disabled={!!exporting || total === 0} onClick={() => void doExport('pdf')}>{exporting === 'pdf' ? 'Exporting...' : 'Export PDF'}</Button>
                </>
            )}
        >
            <Panel style={{ marginBottom: 14 }}><UsageFilters options={options} value={filters} onChange={setFilters} /></Panel>

            {shownError ? (
                <Banner kind='error'>
                    {shownError.message}{shownError.code === 'key_missing' ? ' Open Settings from the left menu.' : ''}
                </Banner>
            ) : null}

            <div className='vdc-card' style={{ overflow: 'hidden' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 16px', borderBottom: `1px solid ${colors.border}`, gap: 12, flexWrap: 'wrap' }}>
                    <div><b style={{ fontSize: 15, color: colors.ink }}>{fmt(result?.totalPoints ?? 0, 2)}</b> <span style={{ color: colors.muted }}>points across</span> <b>{fmt(total, 0)}</b> <span style={{ color: colors.muted }}>usage lines</span></div>
                    <div style={{ display: 'flex', gap: 8, alignItems: 'center', color: colors.muted }}>
                        <Button disabled={offset === 0 || loading} onClick={() => setOffset(Math.max(0, offset - PAGE_SIZE))}>Previous</Button>
                        <span style={{ minWidth: 90, textAlign: 'center' }}>{total === 0 ? 0 : offset + 1} to {Math.min(offset + PAGE_SIZE, total)} of {fmt(total, 0)}</span>
                        <Button disabled={offset + PAGE_SIZE >= total || loading} onClick={() => setOffset(offset + PAGE_SIZE)}>Next</Button>
                    </div>
                </div>
                <div style={{ overflow: 'auto', maxHeight: 'calc(100vh - 420px)', minHeight: 220 }}>
                    <table className='vdc-table'>
                        <thead>
                            <tr>
                                {head('Month', 'month')}{head('Tenant', 'tenant')}{head('Product', 'family')}
                                <th style={{ textAlign: 'left' }}>Edition</th><th style={{ textAlign: 'left' }}>Cloud</th><th style={{ textAlign: 'left' }}>Region type</th>
                                {head('Used', 'units', true)}{head('PPU', 'ppu', true)}{head('Points', 'points', true)}
                            </tr>
                        </thead>
                        <tbody>
                            {loading && !result ? <SkeletonRows cols={9} /> : null}
                            {(result?.rows ?? []).map((r, i) => (
                                <tr key={`${r.month}|${r.tenantUid}|${r.skuCode}|${r.location}|${i}`} style={{ opacity: loading ? 0.6 : 1 }}>
                                    <td style={{ color: colors.muted }}>{monthLabel(r.month)}</td>
                                    <td title={r.tenantUid}>
                                        <div style={{ fontWeight: 500, color: colors.ink }}>{r.tenantName}</div>
                                        {r.companyName
                                            ? <div style={{ fontSize: 11.5, color: colors.muted }}>{r.companyName}{r.mapSource === 'name' ? ' (name match)' : ''}</div>
                                            : <div style={{ fontSize: 11.5, color: colors.warn }}>Not mapped to a VSPC company</div>}
                                    </td>
                                    <td title={`${r.skuName}\n${r.skuCode}`}><FamilyPill family={r.family} title={`${r.skuName}\n${r.skuCode}${r.families.length > 1 ? `\nAlso includes: ${r.families.filter(f => f !== r.family).join(', ')}` : ''}`} /></td>
                                    <td>{r.edition}{r.families.length > 1 ? <span style={{ color: colors.muted }}> bundle</span> : null}</td>
                                    <td style={{ color: colors.muted }}>{r.cloud}</td>
                                    <td style={{ color: colors.muted }}>{r.regionType}</td>
                                    <td className='vdc-num'>{formatUnits(r.usedUnits, r.unitType)}</td>
                                    <td className='vdc-num' style={{ color: colors.muted }}>{fmt(r.ppu, 2)}</td>
                                    <td className='vdc-num' style={{ fontWeight: 600, color: colors.ink, minWidth: 110, background: `linear-gradient(270deg, rgba(0,179,54,.14) ${(r.points / maxPoints) * 100}%, transparent ${(r.points / maxPoints) * 100}%)` }}>{fmt(r.points, 2)}</td>
                                </tr>
                            ))}
                            {!loading && !shownError && total === 0 ? <tr><td colSpan={9} style={{ color: colors.muted, textAlign: 'center', padding: 40 }}>No usage matches these filters.</td></tr> : null}
                        </tbody>
                    </table>
                </div>
            </div>
            <div style={{ color: colors.muted, fontSize: 12, marginTop: 8 }}>Data is cached in the plugin for up to 10 minutes. Use Reload from VSPC to fetch it again.</div>
        </Page>
    );
};
