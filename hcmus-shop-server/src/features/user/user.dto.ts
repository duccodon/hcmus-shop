export interface UserFilterInput {
  search?: string;
  role?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateUserInput {
  username: string;
  fullName: string;
  password: string;
  role: string;
}

export interface UpdateUserInput {
  username: string;
  fullName: string;
  password?: string | null;
  role: string;
}
