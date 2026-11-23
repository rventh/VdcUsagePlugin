import React, { useEffect, useState } from 'react';
import { Banner, Button, colors, Page, Panel, Tile } from 'views/components/ui';
import { UsageFilters, useFilterState } from 'views/components/UsageFilters';
import { BarList, LineChart, SplitBar } from 'views/components/Charts';
import { axisLabels, fmt, monthLabel } from 'core/api';
import type { ApiError, Dashboard } from 'core/api';

export const DashboardPage: React.FC = () => {
    const { api, options, error: optionsError, filters, setFilters, refresh } = useFilterState();
    const [data, setData] = useState<Dashboard | null>(null);
    const [error, setError] = useState<ApiError | null>(null);
    const [loading, setLoading] = useState(true);
    const filterKey = JSON.stringify(filters);

    useEffect(() => {
        let cancelled = false;
        setLoading(true);
        void api.dashboard(filters).then(res => {
            if (cancelled) return;
            setLoading(false);
            if (res.error) { setError(res.error); setData(null); } else { setError(null); setData(res.data); }
        });
        return () => { cancelled = true; };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [api, filterKey]);

    const shownError = error ?? optionsError;
    const change = data && data.previousPoints != null && data.previousPoints > 0 && data.latestMonth
        ? ((data.latestPoints - data.previousPoints) / data.previousPoints) * 100 : null;
    const empty = !!data && data.byMonth.length === 0;

    return (
        <Page
            title='Dashboard'
            subtitle='Where Veeam Data Cloud points are going, by month, tenant and product'
            actions={<Button disabled={loading} onClick={() => void refresh().then(() => setFilters({ ...filters }))}>Reload from VSPC</Button>}
        >
            <Panel style={{ marginBottom: 14 }}><UsageFilters options={options} value={filters} onChange={setFilters} /></Panel>
            {shownError ? <Banner kind='error'>{shownError.message}{shownError.code === 'key_missing' ? ' Open Settings from the left menu.' : ''}</Banner> : null}
            {loading && !data ? <div style={{ color: colors.muted, padding: 20 }}>Loading usage from VSPC...</div> : null}
            {empty ? <Banner kind='info'>No usage matches these filters.</Banner> : null}

            {data && !empty ? (
                <div style={{ opacity: loading ? 0.6 : 1 }}>
                    <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 14 }}>
                        <Tile label='Points in selection' value={fmt(data.totalPoints)} sub={`${data.months.length} month${data.months.length === 1 ? '' : 's'}`} />
                        <Tile label={`Latest month${data.latestMonth ? ` · ${monthLabel(data.latestMonth)}` : ''}`} value={fmt(data.latestPoints)} sub={change == null ? undefined : `${change >= 0 ? '+' : ''}${fmt(change, 1)}% vs ${data.previousMonth ? monthLabel(data.previousMonth) : ''}`} trend={change == null ? null : change >= 0 ? 'up' : 'down'} />
                        <Tile label='Tenants' value={fmt(data.tenantCount, 0)} />
                        <Tile label='Products (SKUs)' value={fmt(data.skuCount, 0)} />
                    </div>

                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(440px, 1fr))', gap: 14 }}>
                        <Panel title='Points per month' hint='All tenants in the selection'>
                            <LineChart labels={axisLabels(data.byMonth.map(m => m.month))} series={[{ name: 'All tenants', values: data.byMonth.map(m => m.points) }]} />
                        </Panel>
                        <Panel title='Product split' hint='Share of points by product family and SKU'>
                            <SplitBar items={data.familyTotals.map(f => ({ label: f.family, value: f.points }))} />
                            <div style={{ marginTop: 16 }}>
                                <BarList items={data.skuTotals.map(s => ({ label: s.skuName, value: s.points }))} total={data.totalPoints} max={8} />
                            </div>
                        </Panel>
                        <Panel title='Points by tenant' hint='Highest consumers first'>
                            <BarList items={data.tenantTotals.map(t => ({ label: t.tenantName, value: t.points }))} total={data.totalPoints} max={10} />
                        </Panel>
                        <Panel title='Tenant trend' hint='Top tenants, month by month'>
                            <LineChart labels={axisLabels(data.months)} series={data.tenantTrend.map(t => ({ name: t.tenantName, values: t.values }))} />
                        </Panel>
                    </div>

                    <Panel title='Product split per tenant' hint='Top tenants' style={{ marginTop: 14 }}>
                        {data.tenantProducts.map(t => (
                            <div key={t.tenantUid} style={{ margin: '14px 0' }}>
                                <div style={{ fontWeight: 600, marginBottom: 6, color: colors.ink }}>{t.tenantName}</div>
                                <SplitBar items={t.items.map(i => ({ label: i.skuName, value: i.points }))} />
                            </div>
                        ))}
                    </Panel>
                </div>
            ) : null}
        </Page>
    );
};
