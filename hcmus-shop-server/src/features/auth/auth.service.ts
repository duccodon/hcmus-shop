import bcrypt from "bcrypt";
import { prisma } from "../../prisma";
import { generateToken } from "../../common/jwt";

export class AuthService {
  async login(username: string, password: string) {
    const user = await prisma.user.findUnique({ where: { username } });
    if (!user) {
      throw new Error("Invalid username or password");
    }

    const valid = await bcrypt.compare(password, user.passwordHash);
    if (!valid) {
      throw new Error("Invalid username or password");
    }

    const token = generateToken({
      userId: user.userId,
      username: user.username,
      role: user.role,
    });

    return { token, user };
  }

  async getMe(userId: string) {
    return prisma.user.findUnique({ where: { userId } });
  }
}

export const authService = new AuthService();
