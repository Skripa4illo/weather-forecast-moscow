/**
 * @typedef {Object} Location
 * @property {string} name
 * @property {string} region
 * @property {string} country
 * @property {number} latitude
 * @property {number} longitude
 * @property {string} timeZoneId
 * @property {string} localTime
 */

/**
 * @typedef {Object} CurrentWeather
 * @property {number} tempC
 * @property {number} feelsLikeC
 * @property {string} conditionText
 * @property {string} conditionIconUrl
 * @property {number} isDay
 * @property {number} windKph
 * @property {number} humidity
 * @property {number} pressureMb
 * @property {number} precipMm
 * @property {string} lastUpdatedLocal
 */

/**
 * @typedef {Object} HourlyForecast
 * @property {number} timeEpoch
 * @property {string} timeLocal
 * @property {string} calendarDate
 * @property {number} tempC
 * @property {string} conditionText
 * @property {string} conditionIconUrl
 * @property {number} isDay
 * @property {number} chanceOfRain
 */

/**
 * @typedef {Object} DailyForecast
 * @property {string} date
 * @property {number} dateEpoch
 * @property {number} maxTempC
 * @property {number} minTempC
 * @property {string} conditionText
 * @property {string} conditionIconUrl
 * @property {number} chanceOfRain
 * @property {number} totalPrecipMm
 * @property {number} maxWindKph
 * @property {number} uv
 */

/**
 * @typedef {Object} WeatherAggregate
 * @property {Location} location
 * @property {CurrentWeather} current
 * @property {HourlyForecast[]} hourlyForecast
 * @property {DailyForecast[]} dailyForecast
 * @property {number} retrievedAtUtcUnix
 * @property {boolean} fromCache
 */

export {}
