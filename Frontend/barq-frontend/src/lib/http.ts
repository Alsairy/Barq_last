import axios from "axios";

axios.defaults.withCredentials = true;

axios.interceptors.request.use((config) => {
  const unsafeMethods = ["post", "put", "patch", "delete"];
  const isUnsafeMethod = unsafeMethods.includes((config.method || "get").toLowerCase());
  
  if (isUnsafeMethod) {
    const cookieMatch = document.cookie.match(/(?:^|;\s*)XSRF-TOKEN=([^;]+)/);
    if (cookieMatch) {
      config.headers = config.headers || {};
      (config.headers as any)["X-XSRF-TOKEN"] = decodeURIComponent(cookieMatch[1]);
    }
  }
  
  return config;
});

axios.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default axios;
