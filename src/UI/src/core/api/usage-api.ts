/** Typed access to the plugin service. Handled errors come back as HTTP 200 + { data, error }. */

export interface ApiError { code: string; message: string }
export interface ApiEnvelope<T> { data: T | null; error: ApiError | null }

export interface Filters {
    from: string;
    to: string;
    tenant: string[];
    family: string[];
    cloud: string[];
    regionType: string[];
    location: string[];
    sku: string[];
    mapping: '' | 'mapped' | 'unmapped';
}

export const emptyFilters = (): Filters => ({
    from: '', to: '', tenant: [], family: [], cloud: [], regionType: [], location: [], sku: [], mapping: '',
});

export interface UsageRow {
    month: string; tenantUid: string; tenantName: string; tenantMapped: boolean;
    skuCode: string; skuName: string; family: string; families: string[]; edition: string; skuLabel: string; cloud: string; regionType: string;
    unitType: string; usedUnits: number; totalUnits: number; ppu: number; points: number; minCommitQty: number; location: string; companyName: string; mapSource: 'pulse' | 'manual' | 'name' | 'none';
}

export interface Options {
    loadedAt: string; pulseLastUpdate: string | null; months: string[];
    tenants: { uid: string; name: string; mapped: boolean }[];
    families: string[]; clouds: string[]; regionTypes: string[]; locations: string[];
    skus: { name: string; label: string; families: string[]; inData: boolean }[];
}

export interface RowsResult { total: number; totalPoints: number; loadedAt: string; rows: UsageRow[] }

export interface Dashboard {
    totalPoints: number; latestMonth: string | null; latestPoints: number; previousMonth: string | null; previousPoints: number | null;
    tenantCount: number; skuCount: number; months: string[];
    byMonth: { month: string; points: number }[];
    tenantTotals: { tenantUid: string; tenantName: string; points: number }[];
    tenantTrend: { tenantUid: string; tenantName: string; values: number[] }[];
    familyTotals: { family: string; points: number }[];
    skuTotals: { skuCode: string; skuName: string; family: string; points: number }[];
    tenantProducts: { tenantUid: string; tenantName: string; items: { skuName: string; points: number }[] }[];
}

export interface KeyStatus { hasKey: boolean; source: 'user' | 'plugin' | 'none'; hint: string; savedAt: string | null; savedBy: string | null }
export interface FilePayload { fileName: string; contentType: string; base64: string }

export const toQuery = (filters: Filters, extra: Record<string, string | number> = {}): string => {
    const q = new URLSearchParams();
    if (filters.from) q.set('from', filters.from);
    if (filters.to) q.set('to', filters.to);
    if (filters.mapping) q.set('mapping', filters.mapping);
    (['tenant', 'family', 'cloud', 'regionType', 'location', 'sku'] as const).forEach(key => {
        filters[key].forEach(v => q.append(key, v));
    });
    Object.entries(extra).forEach(([k, v]) => q.set(k, String(v)));
    const text = q.toString();
    return text ? `?${text}` : '';
};

/** Turns the transport's response (or failure) into { data, error } so pages never have to catch. */
export const unwrap = async <T>(call: Promise<unknown>): Promise<ApiEnvelope<T>> => {
    try {
        const raw = (await call) as any;
        const body = raw && typeof raw === 'object' && 'data' in raw && 'error' in raw ? raw : raw?.data;
        if (body && typeof body === 'object' && ('data' in body || 'error' in body)) {
            return { data: body.data ?? null, error: body.error ?? null };
        }
        return { data: null, error: { code: 'bad_response', message: 'The plugin service returned an unexpected response.' } };
    } catch (e) {
        return { data: null, error: { code: 'unreachable', message: 'The plugin service could not be reached. Check that the plugin is running and try again.' } };
    }
};

export const saveFile = (file: FilePayload): void => {
    const bytes = Uint8Array.from(atob(file.base64), c => c.charCodeAt(0));
    const url = URL.createObjectURL(new Blob([bytes], { type: file.contentType }));
    const a = document.createElement('a');
    a.href = url;
    a.download = file.fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 5000);
};

// filters are kept for the browser tab so the report and the dashboard show the same slice
let shared: Filters = (() => {
    try {
        const saved = sessionStorage.getItem('vdcUsageFilters');
        return saved ? { ...emptyFilters(), ...JSON.parse(saved) } : emptyFilters();
    } catch { return emptyFilters(); }
})();

export const getSharedFilters = (): Filters => shared;
export const setSharedFilters = (f: Filters): void => {
    shared = f;
    try { sessionStorage.setItem('vdcUsageFilters', JSON.stringify(f)); } catch { /* storage may be unavailable */ }
};

export const fmt = (n: number | null | undefined, digits = 1): string =>
    n == null ? '-' : n.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: digits });

const MONTH_NAMES = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

/** "2026-10" becomes "Oct 2026" (or just "Oct" when withYear is false). */
export const monthLabel = (month: string, withYear = true): string => {
    const [y, m] = month.split('-');
    const name = MONTH_NAMES[Number(m) - 1] ?? month;
    return withYear ? `${name} ${y}` : name;
};

/** Short axis labels: just "Jan, Feb, Mar" when all months are in one year, otherwise "Jan 26". */
export const axisLabels = (months: string[]): string[] => {
    const oneYear = new Set(months.map(m => m.slice(0, 4))).size <= 1;
    return months.map(m => (oneYear ? monthLabel(m, false) : `${monthLabel(m, false)} ${m.slice(2, 4)}`));
};

/** Month range for a quick choice, from the months that exist in the data. */
export const presetRange = (months: string[], preset: 'all' | '3' | '6' | '12' | 'ytd'): { from: string; to: string } => {
    if (months.length === 0 || preset === 'all') return { from: '', to: '' };
    const last = months[months.length - 1];
    if (preset === 'ytd') return { from: months.find(m => m.startsWith(last.slice(0, 4))) ?? '', to: '' };
    const n = Number(preset);
    return { from: months[Math.max(0, months.length - n)], to: '' };
};

export interface MappingData {
    companies: { uid: string; name: string }[];
    tenants: { tenantUid: string; tenantName: string; companyUid: string; companyName: string; source: 'pulse' | 'manual' | 'name' | 'none' }[];
}

/** "5" with unit "1TB" reads "5 TB"; "17" with unit "User" reads "17 users". */
export const formatUnits = (used: number, unitType: string): string => {
    const unit = (unitType || '').trim();
    const n = (v: number) => fmt(v, 2);
    const tb = /^(\d+(?:\.\d+)?)\s*TB$/i.exec(unit);
    if (tb) return `${n(used * Number(tb[1]))} TB`;
    if (/^user$/i.test(unit)) return `${n(used)} ${Math.abs(used - 1) < 1e-9 ? 'user' : 'users'}`;
    return unit ? `${n(used)} ${unit}` : n(used);
};
