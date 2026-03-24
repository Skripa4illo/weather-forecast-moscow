import { useCallback, useEffect, useMemo, useState } from 'react'
import { fetchWeather } from './api/weatherClient'
import './App.css'

const REFRESH_MS = 10 * 60 * 1000

function formatError(err) {
  const data = err?.response?.data
  if (data && typeof data === 'object') {
    if (typeof data.detail === 'string') return data.detail
    if (typeof data.title === 'string') return data.title
  }
  if (err?.message) return err.message
  return 'Something went wrong while loading weather.'
}

function groupHourly(items) {
  const map = new Map()
  for (const h of items) {
    const key = h.calendarDate || h.timeLocal?.slice(0, 10) || '—'
    if (!map.has(key)) map.set(key, [])
    map.get(key).push(h)
  }
  return [...map.entries()]
}

export default function App() {
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [dark, setDark] = useState(() => {
    try {
      return localStorage.getItem('theme') !== 'light'
    } catch {
      return true
    }
  })

  useEffect(() => {
    document.documentElement.dataset.theme = dark ? 'dark' : 'light'
    try {
      localStorage.setItem('theme', dark ? 'dark' : 'light')
    } catch {
      /* ignore */
    }
  }, [dark])

  const load = useCallback(async () => {
    setError(null)
    setLoading(true)
    try {
      const json = await fetchWeather()
      setData(json)
    } catch (e) {
      setError(formatError(e))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    const id = setInterval(() => {
      void load()
    }, REFRESH_MS)
    return () => clearInterval(id)
  }, [load])

  const hourlyGroups = useMemo(() => groupHourly(data?.hourlyForecast ?? []), [data])

  return (
    <div className="app">
      <header className="topbar">
        <div>
          <h1 className="title">Moscow weather</h1>
          <p className="subtitle">Fixed location · backend aggregates WeatherAPI.com</p>
        </div>
        <div className="topbar-actions">
          <button
            type="button"
            className="btn ghost"
            onClick={() => setDark((d) => !d)}
            aria-pressed={dark}
          >
            {dark ? 'Light mode' : 'Dark mode'}
          </button>
        </div>
      </header>

      {loading && !data && (
        <div className="panel skeleton-wrap" aria-busy="true">
          <div className="skeleton hero" />
          <div className="skeleton-row">
            <div className="skeleton sm" />
            <div className="skeleton sm" />
            <div className="skeleton sm" />
          </div>
          <div className="skeleton-row">
            <div className="skeleton card" />
            <div className="skeleton card" />
            <div className="skeleton card" />
          </div>
        </div>
      )}

      {error && !data && (
        <div className="panel error-panel" role="alert">
          <h2 className="section-title">Could not load weather</h2>
          <p className="error-text">{error}</p>
          <button type="button" className="btn primary" onClick={() => void load()}>
            Retry
          </button>
        </div>
      )}

      {error && data && (
        <div className="panel error-banner" role="alert">
          <p className="error-text">{error}</p>
          <button type="button" className="btn primary" onClick={() => void load()}>
            Retry
          </button>
        </div>
      )}

      {data && (
        <>
          <section className="panel current">
            <div className="current-main">
              {data.current.conditionIconUrl ? (
                <img
                  className="current-icon"
                  src={data.current.conditionIconUrl}
                  alt=""
                  width={112}
                  height={112}
                />
              ) : (
                <div className="current-icon placeholder" aria-hidden />
              )}
              <div>
                <p className="location-line">
                  {[data.location.name, data.location.country].filter(Boolean).join(', ')}
                </p>
                <p className="local-time">Local: {data.location.localTime}</p>
                <p className="temp-main">{Math.round(data.current.tempC)}°C</p>
                <p className="condition-text">{data.current.conditionText}</p>
                <p className="feels">Feels like {Math.round(data.current.feelsLikeC)}°C</p>
              </div>
            </div>
            <dl className="meta-grid">
              <div>
                <dt>Wind</dt>
                <dd>{Math.round(data.current.windKph)} km/h</dd>
              </div>
              <div>
                <dt>Humidity</dt>
                <dd>{data.current.humidity}%</dd>
              </div>
              <div>
                <dt>Pressure</dt>
                <dd>{Math.round(data.current.pressureMb)} mb</dd>
              </div>
              <div>
                <dt>Precip</dt>
                <dd>{data.current.precipMm} mm</dd>
              </div>
              <div>
                <dt>Updated</dt>
                <dd>{data.current.lastUpdatedLocal}</dd>
              </div>
              <div>
                <dt>Source</dt>
                <dd>{data.fromCache ? 'Cache (≤10 min)' : 'Live'}</dd>
              </div>
            </dl>
          </section>

          <section className="panel">
            <div className="section-head">
              <h2 className="section-title">Hourly forecast</h2>
              <p className="section-hint">Rest of today and all of tomorrow</p>
            </div>
            {hourlyGroups.length === 0 ? (
              <p className="muted">No hourly rows returned.</p>
            ) : (
              hourlyGroups.map(([date, rows]) => (
                <div key={date} className="hourly-block">
                  <h3 className="hourly-date">{date}</h3>
                  <div className="hourly-scroll">
                    {rows.map((h) => (
                      <article key={h.timeEpoch} className="hour-card">
                        <p className="hour-time">{h.timeLocal?.slice(11, 16) ?? '—'}</p>
                        {h.conditionIconUrl ? (
                          <img src={h.conditionIconUrl} alt="" width={48} height={48} />
                        ) : null}
                        <p className="hour-temp">{Math.round(h.tempC)}°</p>
                        <p className="hour-rain">{Math.round(h.chanceOfRain)}% rain</p>
                      </article>
                    ))}
                  </div>
                </div>
              ))
            )}
          </section>

          <section className="panel">
            <div className="section-head">
              <h2 className="section-title">3-day outlook</h2>
            </div>
            <div className="daily-grid">
              {data.dailyForecast.map((d) => (
                <article key={d.dateEpoch + d.date} className="daily-card">
                  <p className="daily-date">{d.date}</p>
                  {d.conditionIconUrl ? (
                    <img src={d.conditionIconUrl} alt="" width={56} height={56} />
                  ) : null}
                  <p className="daily-highlow">
                    {Math.round(d.maxTempC)}° / {Math.round(d.minTempC)}°
                  </p>
                  <p className="daily-desc">{d.conditionText}</p>
                  <p className="daily-meta">
                    Rain {Math.round(d.chanceOfRain)}% · Wind {Math.round(d.maxWindKph)} km/h · UV{' '}
                    {d.uv?.toFixed?.(1) ?? d.uv}
                  </p>
                </article>
              ))}
            </div>
          </section>

          {loading && data && (
            <p className="muted refresh-note" aria-live="polite">
              Refreshing…
            </p>
          )}
        </>
      )}

      <footer className="footer muted">
        Auto-refresh every 10 minutes · API: <code>/api/weather</code>
      </footer>
    </div>
  )
}
