const { chromium } = require('@playwright/test');

const API_URL = 'http://localhost:5142/api';
const TEST_USER = {
  token: 'fake-jwt-token-for-testing',
  user: {
    userId: '11111111-1111-1111-1111-111111111111',
    email: 'coach@test.be',
    firstName: 'Jan',
    lastName: 'Janssen',
    organizationId: '22222222-2222-2222-2222-222222222222',
    role: 'Admin',
  },
};
const TEST_TRAINERS = [
  { id: '33333333-3333-3333-3333-333333333333', firstName: 'Jan', lastName: 'Janssen', email: 'coach@test.be', isActive: true, invitePending: false, lessonSeriesCount: 0, currentWeekHoursBooked: 0, weeklyCapacityHours: 16, notes: null, createdAt: '2026-01-01T00:00:00Z' },
  { id: '44444444-4444-4444-4444-444444444444', firstName: 'Piet', lastName: 'Pieters', email: 'piet@test.be', isActive: true, invitePending: false, lessonSeriesCount: 0, currentWeekHoursBooked: 0, weeklyCapacityHours: 16, notes: null, createdAt: '2026-01-01T00:00:00Z' },
];
const TEST_CLUBS = [{ id: '55555555-5555-5555-5555-555555555555', name: 'TC De Aces', address: 'Sportlaan 1, Antwerpen', organizationId: TEST_USER.user.organizationId }];
const TEST_SERIES = [];

async function mockApi(page, method, path, response, status = 200) {
  await page.route(`**${path}*`, (route) => {
    if (route.request().method() === method.toUpperCase() && route.request().url().includes(`${API_URL}${path}`)) {
      return route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(response) });
    }
    return route.continue();
  });
}

async function selectDate(page, pickerIndex, accessibleName) {
  await page.getByRole('button', { name: 'Kies een datum' }).nth(pickerIndex).click();
  const popover = page.locator('[data-radix-popper-content-wrapper]').last();
  await popover.locator('select').nth(0).selectOption('3');
  await popover.locator('select').nth(1).selectOption('2026');
  await popover.getByRole('button', { name: accessibleName }).click();
}

async function addSlotOnDay(page, dayIndex) {
  const gridBody = page.locator('[style*="60px repeat(7, 1fr)"]').last();
  const dayColumn = gridBody.locator('> div').nth(dayIndex + 1);
  await dayColumn.click({ position: { x: 50, y: 312 } });
  await page.locator('div[data-slot-id]').first().waitFor({ state: 'visible' });
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });

  await mockApi(page, 'GET', '/dashboard', { activeSeriesCount: 0, lessonsThisWeekCount: 0, totalEnrollmentCount: 0, activeTrainerCount: 2, tennisClubCount: 1, upcomingLessons: [] });
  await mockApi(page, 'GET', '/lessonseries', TEST_SERIES);
  await mockApi(page, 'GET', '/trainers', TEST_TRAINERS);
  await mockApi(page, 'GET', '/tennisclubs', TEST_CLUBS);
  await mockApi(page, 'GET', '/trainer-availabilities', []);

  await page.goto('http://localhost:5317/login');
  await page.evaluate(({ token, user }) => {
    localStorage.setItem('token', token);
    localStorage.setItem('auth_user', JSON.stringify(user));
    document.cookie = 'has_token=1; path=/; SameSite=Lax';
    document.cookie = 'user_role=Admin; path=/; SameSite=Lax';
  }, TEST_USER);

  await page.goto('http://localhost:5317/dashboard/lessons/new');
  await page.getByPlaceholder('Voorjaarslessen 2026').fill('Zomerlessen 2026');
  await page.locator('input[name="price"]').fill('150');
  await page.locator('input[name="maxRegistrations"]').fill('12');
  await page.locator('button').filter({ hasText: 'Kies een tennisclub' }).click();
  await page.getByRole('option', { name: 'TC De Aces' }).click();
  await selectDate(page, 0, 'dinsdag 21 april 2026');
  await selectDate(page, 0, 'donderdag 30 april 2026');
  await selectDate(page, 0, 'dinsdag 14 april 2026');
  await page.getByRole('button', { name: 'Volgende' }).click();

  await page.getByPlaceholder('Baan 1').fill('Baan 1');
  await addSlotOnDay(page, 1);
  await page.locator('div[data-slot-id]').first().hover();
  await page.getByRole('button', { name: 'Parallel veld op dit moment toevoegen' }).click();
  await page.locator('div[data-slot-id]').nth(1).click();
  await page.getByLabel('Naam baan').fill('Baan 2');
  await page.getByRole('button', { name: 'Slot opslaan' }).click();
  await page.getByText('Parallel veld 1 van 2').waitFor({ state: 'visible' });
  await page.screenshot({ path: '../pr-screenshots/issue-179-parallel-fields.png', fullPage: true });
  await browser.close();
})();
