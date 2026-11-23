import React, { useCallback, useEffect, useState } from 'react';
import { Banner, Button, colors, Page, Panel } from 'views/components/ui';
import { TenantMapping } from 'views/components/TenantMapping';
import { useUsageApi } from 'core/api';
import type { KeyStatus } from 'core/api';

export const SettingsPage: React.FC = () => {
    const api = useUsageApi();
    const [status, setStatus] = useState<KeyStatus | null>(null);
    const [key, setKey] = useState('');
    const [busy, setBusy] = useState(false);
    const [msg, setMsg] = useState<{ kind: 'error' | 'info'; text: string } | null>(null);

    const load = useCallback(async () => {
        const res = await api.settings();
        if (res.error) setMsg({ kind: 'error', text: res.error.message }); else setStatus(res.data);
    }, [api]);

    useEffect(() => { void load(); }, [load]);

    const save = async () => {
        setBusy(true); setMsg(null);
        const res = await api.saveKey(key.trim());
        setBusy(false);
        if (res.error) { setMsg({ kind: 'error', text: res.error.message }); return; }
        setKey('');
        setMsg({ kind: 'info', text: 'API key verified and saved.' });
        await load();
    };

    const remove = async () => {
        setBusy(true); setMsg(null);
        const res = await api.removeKey();
        setBusy(false);
        if (res.error) { setMsg({ kind: 'error', text: res.error.message }); return; }
        setMsg({ kind: 'info', text: 'Saved API key removed.' });
        await load();
    };

    const sourceText = !status ? '' : status.source === 'user' ? 'Using the API key saved here.' : 'No API key saved yet. Create a read-only key in VSPC and paste it below.';

    return (
        <Page title='Settings' subtitle='Connect the plugin to the VSPC REST API'>
            <Panel title='VSPC API key' hint='Read access to Pulse usage is enough' style={{ maxWidth: 680 }}>
                <p style={{ color: colors.muted, marginTop: 0 }}>
                    The plugin reads VDC usage from the VSPC REST API. Create an API key in VSPC (Configuration, API keys) with access to Pulse usage and paste it below.
                    The key is stored encrypted on the VSPC server and is never shown again.
                </p>
                {status ? (
                    <div style={{ display: 'flex', alignItems: 'center', gap: 10, margin: '0 0 14px', padding: '10px 12px', background: colors.soft, border: `1px solid ${colors.border}`, borderRadius: 8 }}>
                        <span aria-hidden style={{ width: 10, height: 10, borderRadius: '50%', background: status.hasKey ? colors.accent : colors.warn }} />
                        <div>
                            <div style={{ fontWeight: 600, color: colors.ink }}>{status.hasKey ? 'Connected' : 'Not connected'}</div>
                            <div style={{ color: colors.muted, fontSize: 12 }}>{sourceText}{status.hasKey ? ` Key ending ${status.hint.slice(-4)}.` : ''}{status.savedAt ? ` Saved ${new Date(status.savedAt).toLocaleString()}.` : ''}</div>
                        </div>
                    </div>
                ) : null}
                <label className='vdc-label' htmlFor='vdc-key'>New API key</label>
                <input id='vdc-key' className='vdc-input' type='password' autoComplete='off' value={key} onChange={e => setKey(e.target.value)} style={{ margin: '6px 0 14px' }} />
                <div style={{ display: 'flex', gap: 8 }}>
                    <Button primary disabled={busy || key.trim().length < 16} onClick={() => void save()}>{busy ? 'Working...' : 'Verify and save'}</Button>
                    <Button disabled={busy || status?.source !== 'user'} onClick={() => void remove()}>Remove saved key</Button>
                </div>
                {msg ? <div style={{ marginTop: 14 }}><Banner kind={msg.kind}>{msg.text}</Banner></div> : null}
            </Panel>
            {status?.hasKey ? <TenantMapping /> : null}
        </Page>
    );
};
