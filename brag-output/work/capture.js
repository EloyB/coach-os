const { chromium } = require('playwright-core');
const path = require('path');
const fs = require('fs');

const EXE = process.env.HOME + '/Library/Caches/ms-playwright/chromium_headless_shell-1243/chrome-headless-shell-mac-arm64/chrome-headless-shell';
const FPS = 30;
const DUR = parseFloat(process.env.DUR || '20');
const W = 1920, H = 1080;
const OUT = path.join(__dirname, 'frames');

(async () => {
  const total = Math.round(DUR * FPS);
  const browser = await chromium.launch({ executablePath: EXE, args: ['--force-color-profile=srgb','--disable-lcd-text'] });
  const page = await browser.newPage({ viewport: { width: W, height: H }, deviceScaleFactor: 1 });
  await page.goto('file://' + path.join(__dirname, 'video.html'));
  await page.waitForFunction('window.__ready === true');
  await page.evaluate(() => document.fonts.ready);
  await page.evaluate(() => window.measure && window.measure());
  await page.waitForTimeout(300);
  for (let f = 0; f < total; f++) {
    const t = f / FPS;
    await page.evaluate((t) => window.SEEK(t), t);
    await page.screenshot({ path: path.join(OUT, 'f' + String(f).padStart(4, '0') + '.png'), clip: { x:0, y:0, width:W, height:H } });
    if (f % 30 === 0) process.stdout.write(`  frame ${f}/${total}\r`);
  }
  await browser.close();
  console.log(`\ndone: ${total} frames`);
})().catch(e => { console.error(e); process.exit(1); });
