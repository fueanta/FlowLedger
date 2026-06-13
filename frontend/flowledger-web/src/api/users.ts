import { apiClient } from '../lib/apiClient'
import type { PagedResult, Role, User, UserStatus } from '../types'

export type UserListParams = {
  page?: number
  pageSize?: number
  search?: string
  role?: Role | ''
  status?: UserStatus | ''
}

export async function getUsers(params: UserListParams = {}) {
  const response = await apiClient.get<PagedResult<User>>('/users', {
    params: {
      ...params,
      role: params.role || undefined,
      status: params.status || undefined,
    },
  })
  return response.data
}

export async function updateUserRole(id: string, role: Role) {
  await apiClient.put(`/users/${id}/role`, { role })
}

export async function activateUser(id: string) {
  await apiClient.post(`/users/${id}/activate`)
}

export async function deactivateUser(id: string) {
  await apiClient.post(`/users/${id}/deactivate`)
}
