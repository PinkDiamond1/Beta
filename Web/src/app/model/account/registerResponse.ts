import { LoginResponse } from "./loginResponse";

export class RegisterResponse {
  email: string;
  logged: boolean;
  error: string;
  data: LoginResponse;

  constructor(){
  }
}
