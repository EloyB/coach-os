const { chromium } = require('playwright-core');
const path = require('path'); const fs = require('fs');
const EXE = process.env.HOME + '/Library/Caches/ms-playwright/chromium_headless_shell-1243/chrome-headless-shell-mac-arm64/chrome-headless-shell';
const TONE = process.env.TONE || 'appstore';
const FPS = 30, DUR = 21.2, W = 1920, H = 1080;
const OUT = path.join(__dirname, 'frames_' + TONE);
(async () => {
  fs.mkdirSync(OUT, { recursive: true });
  const total = Math.round(DUR * FPS);
  const b = await chromium.launch({ executablePath: EXE, args: ['--force-color-profile=srgb','--disable-lcd-text'] });
  const p = await b.newPage({ viewport: { width: W, height: H }, deviceScaleFactor: 1 });
  await p.goto('file://' + path.join(__dirname, 'video2.html') + '?tone=' + TONE);
  await p.waitForFunction('window.__ready === true');
  await p.evaluate(() => document.fonts.ready);
  await p.evaluate(() => window.measure && window.measure());
  await p.waitForTimeout(300);
  for (let f = 0; f < total; f++) {
    await p.evaluate((t) => window.SEEK(t), f / FPS);
    await p.screenshot({ path: path.join(OUT, 'f' + String(f).padStart(4,'0') + '.png'), clip: { x:0,y:0,width:W,height:H } });
    if (f % 60 === 0) process.stdout.write(`  ${TONE} ${f}/${total}\r`);
  }
  await b.close(); console.log(`\n${TONE}: ${total} frames`);
})().catch(e => { console.error(e); process.exit(1); });
