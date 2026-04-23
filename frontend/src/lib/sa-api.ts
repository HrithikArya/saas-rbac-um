import axios from 'axios';

const saApi = axios.create({ baseURL: '/api', headers: { 'Content-Type': 'application/json' } });

saApi.interceptors.request.use((config) => {
  const token = typeof window !== 'undefined' ? localStorage.getItem('saToken') : null;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

saApi.interceptors.response.use(
  (res) => res,
  async (error) => {
    if (error.response?.status === 401) {
      if (typeof window !== 'undefined') window.location.href = '/superadmin/login';
    }
    return Promise.reject(error);
  }
);

export default saApi;
