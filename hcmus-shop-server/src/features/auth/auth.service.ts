import bcrypt from "bcrypt";
import { prisma } from "../../prisma";
import { generateToken } from "../../common/jwt";

export class AuthService {
  async login(username: string, password: string) {
    console.log("[AuthService] login attempt", { username });

    const user = await prisma.user.findUnique({ where: { username } });
    if (!user) {
      console.log("[AuthService] login failed: user not found", { username });
      throw new Error("Invalid username or password");
    }

    const valid = await bcrypt.compare(password, user.passwordHash);
    if (!valid) {
      console.log("[AuthService] login failed: invalid password", {
        username,
        role: user.role,
      });
      throw new Error("Invalid username or password");
    }

    const token = generateToken({
      userId: user.userId,
      username: user.username,
      role: user.role,
    });

    console.log("[AuthService] login success", {
      userId: user.userId,
      username: user.username,
      role: user.role,
    });

    return { token, user };
  }

  async getMe(userId: string) {
    const user = await prisma.user.findUnique({ where: { userId } });

    console.log("[AuthService] getMe", {
      userId,
      username: user?.username ?? null,
      role: user?.role ?? null,
    });

    return user;
  }
}

export const authService = new AuthService();
