import { apiClient } from '../lib/apiClient'
import type { Client, ClientStatus, PagedResult } from '../types'

type ClientDto = {
  id: string
  name: string
  contactPerson: string
  contactEmail: string
  phone: string
  billingAddress: string
  taxIdentifier: string
  status: ClientStatus
  createdAtUtc: string
  updatedAtUtc: string
  archivedAtUtc: string | null
}

export type ClientListParams = {
  page?: number
  pageSize?: number
  search?: string
  status?: ClientStatus | ''
  sortBy?: 'companyName' | 'status' | 'createdAtUtc' | 'updatedAtUtc'
  sortDirection?: 'asc' | 'desc'
}

export type ClientPayload = {
  companyName: string
  contactPerson: string
  email: string
  phone?: string
  address: string
  taxIdentifier?: string
}

export type UpdateClientPayload = ClientPayload & {
  status: Exclude<ClientStatus, 'Archived'>
}

export async function getClients(params: ClientListParams = {}) {
  const response = await apiClient.get<PagedResult<ClientDto>>('/clients', {
    params: {
      ...params,
      status: params.status || undefined,
    },
  })

  return {
    ...response.data,
    items: response.data.items.map(toClient),
  }
}

export async function getActiveClients() {
  const response = await getClients({ pageSize: 100, status: 'Active', sortBy: 'companyName', sortDirection: 'asc' })
  return response.items
}

export async function createClient(payload: ClientPayload) {
  const response = await apiClient.post<{ id: string }>('/clients', payload)
  return response.data.id
}

export async function updateClient(id: string, payload: UpdateClientPayload) {
  await apiClient.put(`/clients/${id}`, payload)
}

export async function archiveClient(id: string) {
  await apiClient.post(`/clients/${id}/archive`)
}

export function toLegacyCustomer(client: Client) {
  return {
    id: client.id,
    name: client.companyName,
    contactEmail: client.email,
    phone: client.phone,
    billingAddress: client.address,
  }
}

function toClient(dto: ClientDto): Client {
  return {
    id: dto.id,
    companyName: dto.name,
    contactPerson: dto.contactPerson,
    email: dto.contactEmail,
    phone: dto.phone,
    address: dto.billingAddress,
    taxIdentifier: dto.taxIdentifier,
    status: dto.status,
    createdAtUtc: dto.createdAtUtc,
    updatedAtUtc: dto.updatedAtUtc,
    archivedAtUtc: dto.archivedAtUtc,
  }
}
