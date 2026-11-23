import React, { useEffect, useRef, useState } from 'react';
import { colors } from '../ui';

export interface MultiSelectOption { value: string; label: string }

export const MultiSelect: React.FC<{
    label: string; options: MultiSelectOption[]; value: string[]; onChange: (v: string[]) => void; disabled?: boolean;
}> = ({ label, options, value, onChange, disabled }) => {
    const [open, setOpen] = useState(false);
    const [query, setQuery] = useState('');
    const ref = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const close = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false); };
        document.addEventListener('mousedown', close);
        return () => document.removeEventListener('mousedown', close);
    }, []);

    const shown = options.filter(o => o.label.toLowerCase().includes(query.toLowerCase()));
    const toggle = (v: string) => onChange(value.includes(v) ? value.filter(x => x !== v) : [...value, v]);
    const summary = value.length === 0 ? 'All' : value.length === 1 ? (options.find(o => o.value === value[0])?.label ?? value[0]) : `${value.length} selected`;

    return (
        <div ref={ref} className='vdc-field' style={{ position: 'relative' }}>
            <div className='vdc-label'>{label}</div>
            <button
                type='button' disabled={disabled} onClick={() => setOpen(!open)} aria-expanded={open}
                className='vdc-input'
                style={{ textAlign: 'left', cursor: disabled ? 'default' : 'pointer', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 8, color: value.length ? colors.accentDark : colors.text, fontWeight: value.length ? 600 : 400, opacity: disabled ? 0.55 : 1 }}
            >
                <span style={{ overflow: 'hidden', textOverflow: 'ellipsis' }}>{summary}</span>
                <span aria-hidden style={{ color: colors.muted, fontSize: 10 }}>▾</span>
            </button>
            {open ? (
                <div style={{ position: 'absolute', zIndex: 20, top: '100%', marginTop: 4, left: 0, minWidth: '100%', width: 300, maxHeight: 320, overflow: 'auto', background: '#fff', border: `1px solid ${colors.border}`, borderRadius: 10, boxShadow: '0 12px 28px rgba(20,32,43,.16)', padding: 8 }}>
                    {options.length > 8 ? <input className='vdc-input' autoFocus placeholder='Search' value={query} onChange={e => setQuery(e.target.value)} style={{ marginBottom: 6 }} /> : null}
                    {value.length > 0 ? <button type='button' onClick={() => onChange([])} style={{ border: 0, background: 'none', color: colors.accentDark, cursor: 'pointer', padding: '2px 4px', fontWeight: 600 }}>Clear selection</button> : null}
                    {shown.length === 0 ? <div style={{ color: colors.muted, padding: 6 }}>No options</div> : null}
                    {shown.map(o => (
                        <label key={o.value} style={{ display: 'flex', gap: 8, alignItems: 'center', padding: '6px 6px', cursor: 'pointer', borderRadius: 6 }}>
                            <input type='checkbox' checked={value.includes(o.value)} onChange={() => toggle(o.value)} style={{ accentColor: colors.accent }} />
                            <span>{o.label}</span>
                        </label>
                    ))}
                </div>
            ) : null}
        </div>
    );
};
