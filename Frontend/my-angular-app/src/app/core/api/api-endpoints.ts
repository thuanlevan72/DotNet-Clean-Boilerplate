export const API_ENDPOINTS = {
  AUTH: {
    LOGIN: '/auth/login',
    REGISTER: '/auth/register'
  },
  PRODUCT: {
    GET_ALL: '/products',
    GET_BY_ID: (id: string) => `/products/${id}`
  }
};
