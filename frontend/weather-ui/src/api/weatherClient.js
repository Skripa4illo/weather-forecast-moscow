import axios from 'axios'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? ''

export const weatherApi = axios.create({
  baseURL,
  timeout: 30_000,
  headers: { Accept: 'application/json' },
})

/**
 * @returns {Promise<import('../types/weather').WeatherAggregate>}
 */
export async function fetchWeather() {
  const { data } = await weatherApi.get('/api/weather')
  return data
}
