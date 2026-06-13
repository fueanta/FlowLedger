import { apiClient } from '../lib/apiClient'
import type { Customer } from '../types'

export async function getCustomers() {
  const response = await apiClient.get<Customer[]>('/customers')
  return response.data
}
