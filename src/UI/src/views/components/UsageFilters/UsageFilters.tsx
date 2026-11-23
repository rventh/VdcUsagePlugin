import React from 'react';
import { MultiSelect } from './MultiSelect';
import { Button } from '../ui';
import { emptyFilters, monthLabel, presetRange } from 'core/api';
import type { Filters, Options } from 'core/api';

type PresetId = 'all' | '3' | '6' | '12' | 'ytd';
const PRESETS: { id: PresetId; label: string }[] = [
    { id: '3', label: '3 months' },
    { id: '6', label: '6 months' },
    { id: '12', label: '12 months' },
    { id: 'ytd', label: 'This year' },
    { id: 'all', label: 'All' },
];

type ListKey = 'tenant' | 'family' | 'sku' | 'cloud' | 'regionType' | 'location';

export const UsageFilters: React.FC<{ options: Options | null; value: Filters; onChange: (f: Filters) => void }> = ({ options, value, onChange }) => {
    const set = <K extends keyof Filters>(key: K, v: Filters[K]) => onChange({ ...value, [key]: v });
    const o = options;
    const months = o?.months ?? [];
    const asOptions = (list: string[]) => list.map(v => ({ value: v, label: v }));

    // the SKU list follows the chosen product families; a bundle shows under every family it includes
    const skuOptions = (o?.skus ?? [])
        .filter(s => value.family.length === 0 || s.families.some(f => value.family.includes(f)))
        .map(s => ({ value: s.name, label: s.label + (s.inData ? '' : ' (no usage)') }));
    const showVault = value.family.length === 0 || value.family.includes('Vault');

    const changeFamily = (family: string[]) => {
        const allowed = new Set((o?.skus ?? []).filter(s => family.length === 0 || s.families.some(f => family.includes(f))).map(s => s.name));
        const vault = family.length === 0 || family.includes('Vault');
        onChange({ ...value, family, sku: value.sku.filter(n => allowed.has(n)), regionType: vault ? value.regionType : [], cloud: vault ? value.cloud : [] });
    };

    const activePreset = PRESETS.find(p => { const r = presetRange(months, p.id); return r.from === value.from && r.to === value.to; })?.id;

    const monthSelect = (label: string, key: 'from' | 'to') => (
        <div className='vdc-field' style={{ minWidth: 140 }}>
            <label className='vdc-label' htmlFor={`vdc-${key}`}>{label}</label>
            <select id={`vdc-${key}`} className='vdc-select' value={value[key]} onChange={e => set(key, e.target.value)} disabled={!o}>
                <option value=''>{key === 'from' ? 'First month' : 'Latest month'}</option>
                {months.map(m => <option key={m} value={m}>{monthLabel(m)}</option>)}
            </select>
        </div>
    );

    // removable chips for everything that is currently filtered
    const labelFor = (key: ListKey, v: string): string => {
        if (key === 'tenant') return o?.tenants.find(t => t.uid === v)?.name ?? v;
        if (key === 'sku') return o?.skus.find(s => s.name === v)?.label ?? v;
        return v;
    };
    const chips: { id: string; text: string; remove: () => void }[] = [];
    (['tenant', 'family', 'sku', 'regionType', 'cloud', 'location'] as ListKey[]).forEach(key => {
        value[key].forEach(v => chips.push({ id: `${key}:${v}`, text: labelFor(key, v), remove: () => set(key, value[key].filter(x => x !== v) as Filters[typeof key]) }));
    });
    if (value.mapping) chips.push({ id: 'mapping', text: value.mapping === 'mapped' ? 'Mapped to VSPC' : 'Not mapped', remove: () => set('mapping', '') });
    const active = JSON.stringify(value) !== JSON.stringify(emptyFilters());

    return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'flex-end' }}>
                {monthSelect('From', 'from')}
                {monthSelect('To', 'to')}
                <div className='vdc-seg' role='group' aria-label='Quick period'>
                    {PRESETS.map(p => (
                        <button key={p.id} type='button' className={activePreset === p.id ? 'on' : ''} disabled={!o} onClick={() => onChange({ ...value, ...presetRange(months, p.id) })}>{p.label}</button>
                    ))}
                </div>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(190px, 1fr))', gap: 12, alignItems: 'end' }}>
                <MultiSelect label='Tenant' disabled={!o} options={(o?.tenants ?? []).map(t => ({ value: t.uid, label: t.name }))} value={value.tenant} onChange={v => set('tenant', v)} />
                <MultiSelect label='Product family' disabled={!o} options={asOptions(o?.families ?? [])} value={value.family} onChange={changeFamily} />
                <MultiSelect label={value.family.length === 0 ? 'Product (SKU)' : 'Product (SKU) in family'} disabled={!o} options={skuOptions} value={value.sku} onChange={v => set('sku', v)} />
                {showVault ? <MultiSelect label='Vault region type' disabled={!o} options={asOptions(o?.regionTypes ?? [])} value={value.regionType} onChange={v => set('regionType', v)} /> : null}
                {showVault ? <MultiSelect label='Vault cloud' disabled={!o} options={asOptions(o?.clouds ?? [])} value={value.cloud} onChange={v => set('cloud', v)} /> : null}
                <MultiSelect label='Location' disabled={!o} options={asOptions(o?.locations ?? [])} value={value.location} onChange={v => set('location', v)} />
                <div className='vdc-field'>
                    <label className='vdc-label' htmlFor='vdc-mapping'>Tenant mapping</label>
                    <select id='vdc-mapping' className='vdc-select' value={value.mapping} onChange={e => set('mapping', e.target.value as Filters['mapping'])} disabled={!o}>
                        <option value=''>All</option>
                        <option value='mapped'>Mapped to VSPC</option>
                        <option value='unmapped'>Not mapped</option>
                    </select>
                </div>
            </div>
            {active ? (
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, alignItems: 'center' }}>
                    {chips.map(c => <span key={c.id} className='vdc-chip'><span title={c.text}>{c.text}</span><button type='button' aria-label={`Remove ${c.text}`} onClick={c.remove}>×</button></span>)}
                    <Button quiet onClick={() => onChange(emptyFilters())} style={{ height: 26, padding: '0 8px' }}>Reset all</Button>
                </div>
            ) : null}
        </div>
    );
};
