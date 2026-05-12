import { prisma } from "../../prisma";
import { Prisma } from "@prisma/client";
import { CreateUserInput, UpdateUserInput, UserFilterInput } from "./user.dto";

export class UserRepository {
  async findAll(filter: UserFilterInput) {
    const page = Math.max(filter.page ?? 1, 1);
    const pageSize = Math.max(filter.pageSize ?? 10, 1);
    const where: Prisma.UserWhereInput = {
      ...(filter.role && { role: filter.role }),
      ...(filter.search && {
        OR: [
          { username: { contains: filter.search, mode: "insensitive" } },
          { fullName: { contains: filter.search, mode: "insensitive" } },
        ],
      }),
    };

    const [items, totalCount] = await Promise.all([
      prisma.user.findMany({
        where,
        orderBy: { createdAt: "desc" },
        skip: (page - 1) * pageSize,
        take: pageSize,
      }),
      prisma.user.count({ where }),
    ]);

    return { items, totalCount, page, pageSize };
  }

  async findById(userId: string) {
    return prisma.user.findUnique({ where: { userId } });
  }

  async findByUsername(username: string) {
    return prisma.user.findUnique({ where: { username } });
  }

  async create(input: CreateUserInput, passwordHash: string) {
    return prisma.user.create({
      data: {
        username: input.username,
        fullName: input.fullName,
        passwordHash,
        role: input.role,
      },
    });
  }

  async update(userId: string, input: UpdateUserInput, passwordHash?: string | null) {
    return prisma.user.update({
      where: { userId },
      data: {
        username: input.username,
        fullName: input.fullName,
        role: input.role,
        ...(passwordHash ? { passwordHash } : {}),
      },
    });
  }

  async delete(userId: string) {
    await prisma.user.delete({ where: { userId } });
    return true;
  }
}

export const userRepository = new UserRepository();
