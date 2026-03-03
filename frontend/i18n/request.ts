import { getRequestConfig } from 'next-intl/server';

export default getRequestConfig(async () => ({
  locale: 'nl',
  messages: (await import('../messages/nl.json')).default
}));
