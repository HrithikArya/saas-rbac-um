import axios from 'axios';

// All browser requests go through the Next.js /api proxy (next.config.ts rewrites
// /api/* → NEXT_PUBLIC_API_URL/*). This avoids CORS entirely.
const API_BASE = '/api';

// Used by the token refresh call inside the response interceptor, which runs
// in the browser and must also go through the proxy.
const API_URL = API_BASE;

// ── In-memory state (module-level, survives re-renders) ───────────────────────

let _accessToken: string | null = null;
let _currentOrgId: string | null = null;

export const setAccessToken = (token: string | null) => { _accessToken = token; };
export const getAccessToken = () => _accessToken;
export const setApiOrgId = (orgId: string | null) => { _currentOrgId = orgId; };

// ── Axios instance ────────────────────────────────────────────────────────────

const api = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' },
});

// Request: attach Bearer token and org context
api.interceptors.request.use((config) => {
  if (_accessToken) {
    config.headers.Authorization = `Bearer ${_accessToken}`;
  }
  if (_currentOrgId) {
    config.headers['X-Organization-Id'] = _currentOrgId;
  }
  return config;
});

// Response: on 401 → try silent refresh, then retry original request
let _isRefreshing = false;
let _queue: Array<{ resolve: (token: string) => void; reject: (err: unknown) => void }> = [];

const drainQueue = (err: unknown, token: string | null) => {
  _queue.forEach(({ resolve, reject }) => (err ? reject(err) : resolve(token!)));
  _queue = [];
};

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config;

    if (error.response?.status !== 401 || original._retry) {
      return Promise.reject(error);
    }

    if (_isRefreshing) {
      return new Promise<string>((resolve, reject) => {
        _queue.push({ resolve, reject });
      }).then((token) => {
        original.headers.Authorization = `Bearer ${token}`;
        return api(original);
      });
    }

    original._retry = true;
    _isRefreshing = true;

    const refreshToken = typeof window !== 'undefined'
      ? localStorage.getItem('refreshToken')
      : null;

    if (!refreshToken) {
      _isRefreshing = false;
      if (typeof window !== 'undefined') window.location.href = '/login';
      return Promise.reject(error);
    }

    try {
      const { data } = await axios.post(`${API_URL}/auth/refresh`, { refreshToken });
      setAccessToken(data.accessToken);
      localStorage.setItem('refreshToken', data.refreshToken);
      drainQueue(null, data.accessToken);
      original.headers.Authorization = `Bearer ${data.accessToken}`;
      return api(original);
    } catch (refreshErr) {
      drainQueue(refreshErr, null);
      setAccessToken(null);
      localStorage.removeItem('refreshToken');
      if (typeof window !== 'undefined') window.location.href = '/login';
      return Promise.reject(refreshErr);
    } finally {
      _isRefreshing = false;
    }
  }
);

export default api;
