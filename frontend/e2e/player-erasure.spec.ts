import { test, expect } from '@playwright/test';

const id = '11111111-1111-4111-8111-111111111111';
async function login(page: import('@playwright/test').Page, role = 'Admin') {
  await page.context().addCookies([{ name: 'has_token', value: '1', domain: 'localhost', path: '/' }, { name: 'user_role', value: role, domain: 'localhost', path: '/' }]);
  await page.addInitScript((role) => {
    localStorage.setItem('token', 'synthetic-browser-token');
    localStorage.setItem('auth_user', JSON.stringify({ role, email: 'admin@example.test', organizationId: 'synthetic-club', emailVerified: true }));
  }, role);
  await page.route('**/api/**', route => route.fulfill({ json: {} }));
}
for (const kind of ['enrollments', 'camp-enrollments']) {
  test(`${kind}: explicit preview, verified confirmation, actual success only`, async ({ page }) => {
    await login(page);
    const requests: Record<string, unknown>[] = [];
    await page.route(`**/privacy/${kind}/${id}/erasure`, route => {
      const body = route.request().postDataJSON(); requests.push(body);
      return route.fulfill({ json: { eligible: true, deleted: body.apply === true, previewHash: 'a'.repeat(64), holds: [], personalRecords: 2 } });
    });
    await page.goto(`/dashboard/privacy?kind=${kind}&id=${id}`);
    await page.getByRole('button', { name: 'Verwijderoverzicht ophalen' }).click();
    await expect(page.getByText('2 persoonlijke records')).toBeVisible();
    expect(requests).toEqual([{ early: false, newGroupLeaderId: null }]);
    const apply = page.getByRole('button', { name: 'Definitief verwijderen', exact: true });
    await expect(apply).toBeDisabled();
    await page.getByLabel('Exacte deelnemersnaam').fill('Synthetic Player');
    await page.getByLabel('Ik heb de bevoegde aanvraag en spelersidentiteit gecontroleerd').check();
    if (kind === 'enrollments') {
      await page.setViewportSize({ width: 1280, height: 1100 });
      await page.locator('main').evaluate(el => { el.scrollTop = 0; });
      await page.screenshot({ path: '../pr-screenshots/issue-261-player-erasure-desktop.png', fullPage: true });
      await page.setViewportSize({ width: 390, height: 1500 });
      await page.screenshot({ path: '../pr-screenshots/issue-261-player-erasure-mobile.png', fullPage: true });
    }
    await apply.click();
    await expect(page.getByRole('status').filter({ hasText: 'Verwijdering voltooid' })).toBeVisible();
    expect(requests[1]).toMatchObject({ apply: true, early: false, previewHash: 'a'.repeat(64), confirmation: 'Synthetic Player', verifiedRequest: true });
  });
}

test('blocked preview and changed scope cannot be applied; failures do not look successful', async ({ page }) => {
  await login(page);
  let blocked = true;
  await page.route(`**/privacy/enrollments/${id}/erasure`, route => route.fulfill({ json: { eligible: !blocked, deleted: false, holds: blocked ? ['financial_review_required'] : [], previewHash: 'a'.repeat(64), personalRecords: 1 } }));
  await page.goto(`/dashboard/privacy?kind=enrollments&id=${id}`);
  await page.getByRole('button', { name: 'Verwijderoverzicht ophalen' }).click();
  await expect(page.getByText('Financiële beoordeling vereist', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Definitief verwijderen', exact: true })).not.toBeVisible();
  blocked = false;
  await page.getByRole('button', { name: 'Verwijderoverzicht ophalen' }).click();
  await page.getByLabel('Vroegtijdig clubverzoek').check();
  await expect(page.getByText('1 persoonlijke records')).not.toBeVisible();
  await page.route(`**/privacy/enrollments/${id}/erasure`, route => route.fulfill({ status: 403, json: {} }));
  await page.getByRole('button', { name: 'Verwijderoverzicht ophalen' }).click();
  await expect(page.getByRole('alert').filter({ hasText: 'Overzicht of verwijdering mislukt' })).toBeVisible();
});

test('trainer cannot use erasure controls', async ({ page }) => {
  await login(page, 'Trainer');
  await page.goto(`/dashboard/privacy?kind=enrollments&id=${id}`);
  await expect(page.getByText('Alleen clubbeheerders kunnen deze verwijderroute gebruiken.')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Verwijderoverzicht ophalen' })).not.toBeVisible();
});
