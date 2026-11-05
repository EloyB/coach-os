// Local real-UI regression runner. Start website on WEBSITE_UI_BASE_URL first.
// PLAYWRIGHT_MODULE can point to an existing local Playwright installation.
// The form test uses a temporary /ui-test route rendering the real WaitlistForm.
const { chromium } = await import(process.env.PLAYWRIGHT_MODULE || 'playwright');
import assert from 'node:assert/strict';
import { mkdir } from 'node:fs/promises';
const browser = await chromium.launch({ headless: true });
const base = process.env.WEBSITE_UI_BASE_URL || 'http://127.0.0.1:5364';
const evidence = process.env.UI_EVIDENCE_DIR;
if (evidence) await mkdir(evidence, { recursive: true });
const token = 'a'.repeat(64); // Synthetic token, never a live credential.
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
const observed = [];
page.on('request', request => observed.push(request.url()));
await page.route('**/*', route => new URL(route.request().url()).origin === base ? route.continue() : route.abort());
async function screenshot(name) {
  if (evidence) await page.screenshot({ path: `${evidence}/${name}.png` });
}
async function submit(response) {
  await page.route('**/api/waitlist', route => route.fulfill({ json: response }));
  await page.goto(`${base}/ui-test`);
  await page.getByLabel('E-mailadres').fill('local@example.test');
  await page.getByRole('button', { name: 'Zelfstandige coach', exact: true }).click();
  await page.getByRole('button', { name: 'Demo aanvragen', exact: true }).click();
  await page.waitForURL('**/bedankt?type=waitlist*');
  await page.getByRole('heading', { name: 'Bedankt, we plannen je demo in.' }).waitFor();
}
try {
  await submit({ ok: true, unsubscribeUrl: `/uitschrijven#${token}` });
  const link = page.getByRole('link', { name: 'Uitschrijflink bewaren' });
  await link.waitFor();
  assert.equal(await link.getAttribute('href'), `/uitschrijven#${token}`);
  assert.equal(new URL(page.url()).search, '?type=waitlist');
  await screenshot('ui-waitlist-link');
  console.log('PASS new signup retains personal fragment-only link');

  for (const response of [{ ok: true }, { ok: true, unsubscribeUrl: 'https://evil.example/#'+token }]) {
    await submit(response);
    assert.equal(await page.getByRole('link', { name: 'Uitschrijflink bewaren' }).count(), 0);
  }
  console.log('PASS duplicate/honeypot and untrusted URLs expose no link');

  let posts = 0;
  let fail = true;
  await page.route('**/api/waitlist/unsubscribe', async route => {
    assert.equal(route.request().method(), 'POST');
    assert.deepEqual(route.request().postDataJSON(), { token });
    posts++;
    await route.fulfill({ status: fail ? 503 : 200, json: fail ? {error:'unavailable'} : {ok:true} });
  });
  await page.goto(`${base}/uitschrijven#${token}`);
  const button = page.getByRole('button', { name: 'Ja, schrijf mij uit', exact: true });
  await button.waitFor();
  assert.equal(posts, 0);
  await screenshot('ui-unsubscribe-confirm');
  await button.click();
  await page.getByRole('alert').waitFor();
  assert.equal(posts, 1);
  assert.equal(new URL(page.url()).hash, '#'+token);
  fail = false;
  await button.click();
  await page.getByRole('status').filter({ hasText: 'Je bent uitgeschreven' }).waitFor();
  assert.equal(posts, 2);
  assert.equal(new URL(page.url()).hash, '');
  await screenshot('ui-unsubscribe-success');
  console.log('PASS explicit POST only, error feedback, retry and success clears fragment');

  for (const suffix of ['', '#invalid', '?token='+token]) {
    await page.goto(`${base}/uitschrijven${suffix}`);
    await page.getByText('Deze uitschrijflink ontbreekt of is ongeldig.', {exact:false}).waitFor();
    assert.equal(await page.getByRole('button', { name: 'Ja, schrijf mij uit', exact:true }).count(), 0);
    assert.equal(posts, 2);
  }
  console.log('PASS missing, malformed and query tokens cannot submit');

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`${base}/uitschrijven#${token}`);
  await button.waitFor();
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
  await screenshot('ui-unsubscribe-mobile');
  // Deliberate query-invalid navigation above is excluded; real fragment flows never send tokens.
  assert.equal(observed.some(url => url.includes(token) && !url.includes('?token=')), false);
  console.log('PASS mobile layout and fragment token absent from requests');
} finally {
  await browser.close();
}
