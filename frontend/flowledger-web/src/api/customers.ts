import { getActiveClients, toLegacyCustomer } from './clients'

export async function getCustomers() {
  const clients = await getActiveClients()
  return clients.map(toLegacyCustomer)
}
