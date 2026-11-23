import React from 'react';
import { colors } from '../ui';
import { fmt } from 'core/api';

export const LineChart: React.FC<{ labels: string[]; series: { name: string; values: number[] }[]; height?: number }> = ({ labels, series, height = 220 }) => {
    const w = 640, h = height, l = 48, r = 12, t = 12, b = 28;
    const max = Math.max(1, ...series.flatMap(s => s.values));
    const x = (i: number) => l + (labels.length <= 1 ? (w - l - r) / 2 : (i * (w - l - r)) / (labels.length - 1));
    const y = (v: number) => t + (h - t - b) * (1 - v / max);
    const ticks = [0, 0.25, 0.5, 0.75, 1].map(f => f * max);
    return (
        <div>
            <svg viewBox={`0 0 ${w} ${h}`} width='100%' role='img' aria-label='Trend chart' style={{ display: 'block' }}>
                {ticks.map(v => (
                    <g key={v}>
                        <line x1={l} x2={w - r} y1={y(v)} y2={y(v)} stroke={colors.border} strokeWidth={1} strokeDasharray={v === 0 ? undefined : '3 4'} />
                        <text x={l - 6} y={y(v) + 4} fontSize={10} textAnchor='end' fill={colors.muted}>{fmt(v, 0)}</text>
                    </g>
                ))}
                {labels.map((m, i) => (labels.length <= 12 || i % Math.ceil(labels.length / 12) === 0)
                    ? <text key={m} x={x(i)} y={h - 8} fontSize={10} textAnchor='middle' fill={colors.muted}>{m}</text> : null)}
                {series.map((s, si) => {
                    const c = colors.series[si % colors.series.length];
                    return (
                        <g key={s.name}>
                            {series.length === 1 && s.values.length > 1 ? (
                                <>
                                    <defs>
                                        <linearGradient id='vdc-area' x1='0' y1='0' x2='0' y2='1'>
                                            <stop offset='0%' stopColor={c} stopOpacity={0.22} />
                                            <stop offset='100%' stopColor={c} stopOpacity={0} />
                                        </linearGradient>
                                    </defs>
                                    <polygon fill='url(#vdc-area)' points={`${x(0)},${y(0)} ${s.values.map((v, i) => `${x(i)},${y(v)}`).join(' ')} ${x(s.values.length - 1)},${y(0)}`} />
                                </>
                            ) : null}
                            <polyline fill='none' stroke={c} strokeWidth={2.5} strokeLinejoin='round' strokeLinecap='round' points={s.values.map((v, i) => `${x(i)},${y(v)}`).join(' ')} />
                            {s.values.map((v, i) => <circle key={i} cx={x(i)} cy={y(v)} r={3.5} fill='#fff' stroke={c} strokeWidth={2}><title>{`${s.name}, ${labels[i]}: ${fmt(v)} points`}</title></circle>)}
                        </g>
                    );
                })}
            </svg>
            {series.length > 1 ? <Legend items={series.map(s => s.name)} /> : null}
        </div>
    );
};

export const Legend: React.FC<{ items: string[] }> = ({ items }) => (
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: '4px 14px', marginTop: 8, fontSize: 12 }}>
        {items.map((n, i) => (
            <span key={n} style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
                <span style={{ width: 10, height: 10, borderRadius: 3, background: colors.series[i % colors.series.length], display: 'inline-block' }} />{n}
            </span>
        ))}
    </div>
);

/** Horizontal bars: label, bar, value. Share is relative to the total passed in. */
export const BarList: React.FC<{ items: { label: string; value: number }[]; total?: number; max?: number }> = ({ items, total, max = 10 }) => {
    const shown = items.slice(0, max);
    const top = Math.max(1, ...shown.map(i => i.value));
    const sum = total ?? items.reduce((a, i) => a + i.value, 0);
    if (shown.length === 0) return <div style={{ color: colors.muted }}>No data for these filters.</div>;
    return (
        <div>
            {shown.map((i, idx) => (
                <div key={i.label} style={{ display: 'flex', alignItems: 'center', gap: 8, margin: '5px 0' }}>
                    <div title={i.label} style={{ width: 190, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{i.label}</div>
                    <div style={{ flex: 1, background: colors.bg, borderRadius: 6, height: 10 }}>
                        <div style={{ width: `${(i.value / top) * 100}%`, height: 10, borderRadius: 6, background: colors.series[idx % colors.series.length] }} />
                    </div>
                    <div style={{ width: 120, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{fmt(i.value)}{sum > 0 ? ` (${fmt((i.value / sum) * 100, 0)}%)` : ''}</div>
                </div>
            ))}
            {items.length > max ? <div style={{ color: colors.muted, fontSize: 12 }}>+ {items.length - max} more</div> : null}
        </div>
    );
};

/** One stacked bar showing how a total divides across products. */
export const SplitBar: React.FC<{ items: { label: string; value: number }[] }> = ({ items }) => {
    const total = items.reduce((a, i) => a + i.value, 0);
    if (total <= 0) return <div style={{ color: colors.muted }}>No data.</div>;
    return (
        <div>
            <div style={{ display: 'flex', height: 14, borderRadius: 7, overflow: 'hidden', gap: 2 }} role='img' aria-label='Product split'>
                {items.map((i, idx) => <div key={i.label} title={`${i.label}: ${fmt(i.value)} points`} style={{ width: `${(i.value / total) * 100}%`, background: colors.series[idx % colors.series.length] }} />)}
            </div>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '3px 14px', marginTop: 6, fontSize: 12 }}>
                {items.map((i, idx) => (
                    <span key={i.label}>
                        <span style={{ display: 'inline-block', width: 9, height: 9, borderRadius: 2, background: colors.series[idx % colors.series.length], marginRight: 5 }} />
                        {i.label}: {fmt(i.value)} ({fmt((i.value / total) * 100, 0)}%)
                    </span>
                ))}
            </div>
        </div>
    );
};
