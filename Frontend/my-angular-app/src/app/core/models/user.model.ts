export interface User {
  id: string;
  email: string;
  token: string;
  refreshToken: string;
  roles: string[];
}


export interface UserLocalStorage {
  id: string;
  email: string;
  fullName: string;
  role: string;
  expirationTime: number;
}
