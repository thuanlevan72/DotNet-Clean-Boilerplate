import { FormGroup } from "@angular/forms";

export interface RegisterRequest {
  email: string;
  password: string;
  fullname: string;
}


// Tương đương với AuthResponseDTO ở Backend
export interface RegisterResponse {
  message: string;
}
