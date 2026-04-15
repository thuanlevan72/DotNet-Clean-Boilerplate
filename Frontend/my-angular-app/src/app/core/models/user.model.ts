export interface User {
  id: string;
  email: string;
  token: string;
  refreshToken: string;
  roles: string[];
}
