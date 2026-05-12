import bcrypt from "bcrypt";
import { CreateUserInput, UpdateUserInput, UserFilterInput } from "./user.dto";
import { userRepository } from "./user.repository";

export class UserService {
  async getAll(filter: UserFilterInput) {
    return userRepository.findAll(filter);
  }

  async getById(userId: string) {
    return userRepository.findById(userId);
  }

  async create(input: CreateUserInput) {
    await this.ensureValidInput(input.username, input.fullName, input.role, input.password);
    const existing = await userRepository.findByUsername(input.username.trim());
    if (existing) {
      throw new Error("Username already exists");
    }

    const passwordHash = await bcrypt.hash(input.password.trim(), 10);
    return userRepository.create(
      {
        ...input,
        username: input.username.trim(),
        fullName: input.fullName.trim(),
        role: this.normalizeRole(input.role),
        password: input.password.trim(),
      },
      passwordHash
    );
  }

  async update(userId: string, input: UpdateUserInput) {
    await this.ensureValidInput(input.username, input.fullName, input.role, input.password ?? undefined, false);
    const existing = await userRepository.findById(userId);
    if (!existing) {
      throw new Error("User not found");
    }

    const duplicate = await userRepository.findByUsername(input.username.trim());
    if (duplicate && duplicate.userId !== userId) {
      throw new Error("Username already exists");
    }

    const passwordHash = input.password?.trim()
      ? await bcrypt.hash(input.password.trim(), 10)
      : null;

    return userRepository.update(
      userId,
      {
        ...input,
        username: input.username.trim(),
        fullName: input.fullName.trim(),
        role: this.normalizeRole(input.role),
      },
      passwordHash
    );
  }

  async delete(userId: string) {
    const existing = await userRepository.findById(userId);
    if (!existing) {
      throw new Error("User not found");
    }

    if (existing.role.localeCompare("Admin", undefined, { sensitivity: "accent" }) === 0) {
      throw new Error("Admin users cannot be deleted from this screen");
    }

    return userRepository.delete(userId);
  }

  private async ensureValidInput(
    username: string,
    fullName: string,
    role: string,
    password?: string,
    requirePassword = true
  ) {
    if (!username?.trim()) {
      throw new Error("Username is required");
    }

    if (!fullName?.trim()) {
      throw new Error("Full name is required");
    }

    const normalizedRole = this.normalizeRole(role);
    if (normalizedRole !== "Sale" && normalizedRole !== "Admin") {
      throw new Error("Role must be Admin or Sale");
    }

    if (requirePassword && !password?.trim()) {
      throw new Error("Password is required");
    }

    if (password?.trim() && password.trim().length < 6) {
      throw new Error("Password must be at least 6 characters");
    }

    await Promise.resolve();
  }

  private normalizeRole(role: string) {
    const normalizedRole = role?.trim() ?? "";
    if (normalizedRole.localeCompare("admin", undefined, { sensitivity: "accent" }) === 0) {
      return "Admin";
    }

    if (
      normalizedRole.localeCompare("sale", undefined, { sensitivity: "accent" }) === 0 ||
      normalizedRole.localeCompare("sales", undefined, { sensitivity: "accent" }) === 0
    ) {
      return "Sale";
    }

    return "Sale";
  }
}

export const userService = new UserService();
