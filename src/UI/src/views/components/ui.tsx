import React from 'react';

export const colors = {
    accent: '#00b336', accentDark: '#008a29', accentSoft: '#e8f8ee',
    ink: '#14202b', text: '#26323d', muted: '#6b7a88', border: '#e1e7ee', borderStrong: '#cdd5df', bg: '#f3f5f8', soft: '#f8fafc', card: '#ffffff',
    error: '#c0392b', warn: '#b7791f',
    series: ['#00b336', '#2563eb', '#f59e0b', '#8b5cf6', '#ec4899', '#06b6d4', '#84cc16', '#ef4444', '#64748b', '#0ea5e9'],
    family: { 'Microsoft 365': '#2563eb', 'Microsoft Entra ID': '#8b5cf6', Salesforce: '#06b6d4', 'Microsoft Azure': '#0ea5e9', Vault: '#00a82f' } as Record<string, string>,
};

const css = `
.vdc-root{font-family:inherit;color:${colors.text};font-size:13px;line-height:1.45;}
.vdc-root *{box-sizing:border-box;}
.vdc-btn{display:inline-flex;align-items:center;justify-content:center;gap:6px;height:32px;padding:0 14px;border-radius:8px;border:1px solid ${colors.borderStrong};background:#fff;color:${colors.text};font:inherit;font-weight:500;cursor:pointer;white-space:nowrap;transition:background .15s,border-color .15s,box-shadow .15s;}
.vdc-btn:hover:not(:disabled){background:${colors.bg};border-color:#b4bfcb;}
.vdc-btn:disabled{opacity:.5;cursor:default;}
.vdc-btn-primary{background:${colors.accent};border-color:${colors.accent};color:#fff;box-shadow:0 1px 2px rgba(0,120,36,.25);}
.vdc-btn-primary:hover:not(:disabled){background:#009f30;border-color:#009f30;}
.vdc-btn-quiet{border-color:transparent;background:transparent;color:${colors.muted};}
.vdc-btn:focus-visible,.vdc-input:focus-visible,.vdc-select:focus-visible,.vdc-seg button:focus-visible,.vdc-chip button:focus-visible{outline:2px solid ${colors.accent};outline-offset:1px;}
.vdc-input,.vdc-select{height:32px;padding:0 10px;border:1px solid ${colors.borderStrong};border-radius:8px;background:#fff;color:${colors.text};font:inherit;width:100%;min-width:130px;}
.vdc-field{display:flex;flex-direction:column;gap:4px;min-width:150px;}
.vdc-label{font-size:11px;font-weight:600;letter-spacing:.04em;text-transform:uppercase;color:${colors.muted};}
.vdc-seg{display:inline-flex;border:1px solid ${colors.borderStrong};border-radius:8px;overflow:hidden;background:#fff;}
.vdc-seg button{height:30px;padding:0 12px;border:0;border-right:1px solid ${colors.border};background:#fff;color:${colors.text};font:inherit;cursor:pointer;white-space:nowrap;}
.vdc-seg button:last-child{border-right:0;}
.vdc-seg button:hover:not(:disabled){background:${colors.bg};}
.vdc-seg button.on{background:${colors.accentSoft};color:${colors.accentDark};font-weight:600;}
.vdc-seg button:disabled{opacity:.5;cursor:default;}
.vdc-chip{display:inline-flex;align-items:center;gap:4px;max-width:260px;padding:2px 4px 2px 10px;border-radius:999px;background:${colors.accentSoft};color:${colors.accentDark};font-size:12px;font-weight:500;}
.vdc-chip span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;}
.vdc-chip button{border:0;background:transparent;color:inherit;cursor:pointer;width:18px;height:18px;border-radius:50%;line-height:16px;padding:0;font-size:14px;}
.vdc-chip button:hover{background:rgba(0,138,41,.15);}
.vdc-table{border-collapse:separate;border-spacing:0;width:100%;}
.vdc-table th{position:sticky;top:0;z-index:1;background:${colors.soft};padding:9px 12px;font-size:11px;font-weight:600;letter-spacing:.04em;text-transform:uppercase;color:${colors.muted};border-bottom:1px solid ${colors.border};white-space:nowrap;user-select:none;}
.vdc-table th.sortable{cursor:pointer;}
.vdc-table th.sortable:hover{color:${colors.ink};}
.vdc-table td{padding:9px 12px;border-bottom:1px solid ${colors.bg};white-space:nowrap;}
.vdc-table tbody tr:hover td{background:#f5faf7;}
.vdc-num{text-align:right;font-variant-numeric:tabular-nums;}
.vdc-pill{display:inline-block;padding:1px 9px;border-radius:999px;font-size:11.5px;font-weight:600;}
.vdc-card{background:#fff;border:1px solid ${colors.border};border-radius:12px;box-shadow:0 1px 2px rgba(20,32,43,.04);}
.vdc-skel{height:12px;border-radius:6px;background:linear-gradient(90deg,#eef1f5 25%,#f7f9fb 50%,#eef1f5 75%);background-size:200% 100%;animation:vdc-shimmer 1.2s infinite;}
@keyframes vdc-shimmer{0%{background-position:200% 0}100%{background-position:-200% 0}}
`;

export const GlobalStyle: React.FC = () => <style>{css}</style>;

export const page: React.CSSProperties = { padding: '20px 24px 28px', color: colors.text, fontFamily: 'inherit', fontSize: 13, height: '100%', overflow: 'auto', boxSizing: 'border-box', background: colors.bg };
export const card: React.CSSProperties = { background: colors.card, border: `1px solid ${colors.border}`, borderRadius: 12, padding: 16, boxShadow: '0 1px 2px rgba(20,32,43,.04)' };
export const h2: React.CSSProperties = { margin: '0 0 2px', fontSize: 14, fontWeight: 600, color: colors.ink };

/** Page frame: title block with actions on the right, then the content. */
export const Page: React.FC<{ title: string; subtitle?: string; actions?: React.ReactNode; children: React.ReactNode }> = ({ title, subtitle, actions, children }) => (
    <div className='vdc-root' style={page}>
        <GlobalStyle />
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 12, flexWrap: 'wrap', marginBottom: 16 }}>
            <div>
                <h1 style={{ margin: 0, fontSize: 22, fontWeight: 600, color: colors.ink, letterSpacing: '-0.01em' }}>{title}</h1>
                {subtitle ? <div style={{ color: colors.muted, marginTop: 2 }}>{subtitle}</div> : null}
            </div>
            {actions ? <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>{actions}</div> : null}
        </div>
        {children}
    </div>
);

/** A card with an optional heading. */
export const Panel: React.FC<{ title?: string; hint?: string; children: React.ReactNode; style?: React.CSSProperties }> = ({ title, hint, children, style }) => (
    <div className='vdc-card' style={{ padding: 16, ...style }}>
        {title ? (
            <div style={{ marginBottom: 12 }}>
                <h3 style={h2}>{title}</h3>
                {hint ? <div style={{ color: colors.muted, fontSize: 12 }}>{hint}</div> : null}
            </div>
        ) : null}
        {children}
    </div>
);

export const Button: React.FC<React.ButtonHTMLAttributes<HTMLButtonElement> & { primary?: boolean; quiet?: boolean }> = ({ primary, quiet, className, type, ...rest }) => (
    <button type={type ?? 'button'} {...rest} className={`vdc-btn${primary ? ' vdc-btn-primary' : ''}${quiet ? ' vdc-btn-quiet' : ''}${className ? ` ${className}` : ''}`} />
);

export const Banner: React.FC<{ kind: 'error' | 'info' | 'warn'; children: React.ReactNode }> = ({ kind, children }) => {
    const c = kind === 'error' ? colors.error : kind === 'warn' ? colors.warn : colors.accentDark;
    const bg = kind === 'error' ? '#fdf1ef' : kind === 'warn' ? '#fdf6e8' : colors.accentSoft;
    return (
        <div role={kind === 'error' ? 'alert' : 'status'} style={{ borderLeft: `4px solid ${c}`, color: colors.text, background: bg, borderRadius: 8, padding: '10px 14px', margin: '0 0 12px' }}>
            {children}
        </div>
    );
};

/** Coloured badge for a product family. */
export const FamilyPill: React.FC<{ family: string; title?: string }> = ({ family, title }) => {
    const c = colors.family[family] ?? colors.muted;
    return <span className='vdc-pill' title={title} style={{ background: `${c}1f`, color: c }}>{family}</span>;
};

export const Tile: React.FC<{ label: string; value: string; sub?: string; trend?: 'up' | 'down' | null }> = ({ label, value, sub, trend }) => (
    <div className='vdc-card' style={{ flex: '1 1 180px', minWidth: 170, padding: '14px 16px', position: 'relative', overflow: 'hidden' }}>
        <div style={{ position: 'absolute', left: 0, top: 0, bottom: 0, width: 4, background: colors.accent }} />
        <div className='vdc-label'>{label}</div>
        <div style={{ fontSize: 28, fontWeight: 600, color: colors.ink, margin: '4px 0 2px', fontVariantNumeric: 'tabular-nums', letterSpacing: '-0.02em' }}>{value}</div>
        {sub ? <div style={{ fontSize: 12, color: trend === 'up' ? colors.accentDark : trend === 'down' ? colors.error : colors.muted, fontWeight: trend ? 600 : 400 }}>{trend === 'up' ? '▲ ' : trend === 'down' ? '▼ ' : ''}{sub}</div> : null}
    </div>
);

export const SkeletonRows: React.FC<{ cols: number; rows?: number }> = ({ cols, rows = 6 }) => (
    <>
        {Array.from({ length: rows }).map((_, r) => (
            <tr key={r}>{Array.from({ length: cols }).map((__, c) => <td key={c}><div className='vdc-skel' style={{ width: c === 0 ? 70 : `${55 + ((r * 7 + c * 13) % 40)}%` }} /></td>)}</tr>
        ))}
    </>
);
